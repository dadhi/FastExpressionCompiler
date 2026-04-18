/*
The MIT License (MIT)

Copyright (c) 2016-2026 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

// POC for issue #512: data-oriented flat expression tree.
// Intrusive linked-list tree: ChildIdx (first child) + NextIdx (next sibling), 1-based into Nodes.
// 0/default == nil.  ExpressionTree keeps ≤16 nodes on the stack via Stack16<ExpressionNode>.

#nullable disable

namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
#else
using FastExpressionCompiler.ImTools;
#endif

using SysExpr = System.Linq.Expressions.Expression;
using SysParam = System.Linq.Expressions.ParameterExpression;

/// <summary>1-based index into <see cref="ExpressionTree.Nodes"/>. <c>default</c> == nil. Backed by a 2-byte <c>short</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Idx : IEquatable<Idx>
{
    /// <summary>Raw 1-based index; 0 means nil.</summary>
    public short It;

    /// <summary>True when this index is nil (unset).</summary>
    public bool IsNil => It == 0;
    /// <summary>The nil sentinel value.</summary>
    public static Idx Nil => default;

    /// <summary>Creates a 1-based index from the given value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Idx Of(int oneBasedIndex)
    {
        Debug.Assert(oneBasedIndex >= 0 && oneBasedIndex <= short.MaxValue, "Index out of short range");
        return new Idx { It = (short)oneBasedIndex };
    }

    /// <inheritdoc/>
    public bool Equals(Idx other) => It == other.It;
    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is Idx other && Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode() => It;
    /// <inheritdoc/>
    public override string ToString() => IsNil ? "nil" : It.ToString();
}

/// <summary>
/// Compact 32-byte node. Two reference fields first, then two shorts, then the packed 64-bit data word.
/// Layout: Type(8) + Obj(8) + _typeFlags(2) + NextIdx(2) + pad(4) + _data(8) = 32 bytes.
/// <list type="table">
///   <item><term>Constant inline</term>
///     <description>IsInplaceConst = true; <see cref="Data"/> holds raw bits (up to 8 bytes: bool/int/long/float/double/DateTime/…).</description></item>
///   <item><term>Constant closure</term>
///     <description>IsInplaceConst = false; ChildIdx = 1-based slot in <see cref="ExpressionTree.ClosureConstants"/>.</description></item>
///   <item><term>Constant in Obj</term>
///     <description>IsInplaceConst = false; ChildIdx = 0; value in <see cref="Obj"/> (null, string, decimal, Guid, …).</description></item>
///   <item><term>Parameter</term>  <description>Obj = name (string or null).</description></item>
///   <item><term>Unary</term>      <description>Obj = MethodInfo (nullable); ChildIdx = operand; ExtraIdx = nil.</description></item>
///   <item><term>Binary</term>     <description>Obj = MethodInfo (nullable); ChildIdx = left; ExtraIdx = right.</description></item>
///   <item><term>New</term>        <description>Obj = ConstructorInfo; ChildIdx = first arg (chained via NextIdx).</description></item>
///   <item><term>Call</term>       <description>Obj = MethodInfo; ChildIdx = instance-or-first-static-arg; ExtraIdx = first arg for instance calls.</description></item>
///   <item><term>Lambda</term>     <description>Obj = Idx[] of params; ChildIdx = body. Params stored in Obj rather than NextIdx chain because a parameter node may already have its NextIdx occupied as a Call/New argument.</description></item>
///   <item><term>Block</term>      <description>ChildIdx = first expr; ExtraIdx = first variable (both chained via NextIdx).</description></item>
///   <item><term>Conditional</term><description>ChildIdx = test; ExtraIdx = ifTrue; ifFalse = NodeAt(ifTrue).NextIdx.</description></item>
/// </list>
/// <para>vs LightExpression heap objects (16-byte GC header + fields): Constant/Parameter ~40 bytes | Binary/Unary ~48–56 bytes.</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ExpressionNode  // 32 bytes: Type(8)+Obj(8)+_typeFlags(2)+NextIdx(2)+pad(4)+_data(8)
{
    /// <summary>Result type of this node.</summary>
    public Type Type;
    /// <summary>Object payload: method/ctor for Call/New/Unary/Binary; param name for Parameter; type for TypeIs/TypeEqual; Idx[] for Lambda params; constant value for non-inline/non-closure Constant nodes.</summary>
    public object Obj;
    // NodeType in bits 0–14; bit 15 = IsInplaceConst flag.
    internal short _typeFlags;
    /// <summary>Raw next-sibling index (0 = nil).</summary>
    public short NextIdx;
    // Packed 16-bit indexes (or full 8-byte constant bits when IsInplaceConst):
    // bits  0–15: ChildIdx  (first child / 1-based closure slot for Constant)
    // bits 16–31: ChildCount (reserved, future use)
    // bits 32–47: ExtraIdx  (second child; nil for Unary and non-control nodes)
    // bits 48–63: ExtraCount (reserved, future use)
    internal long _data;

    /// <summary>Expression kind.</summary>
    public ExpressionType NodeType => (ExpressionType)(_typeFlags & 0x7FFF);
    /// <summary>True when this Constant node's value is stored inline in <see cref="Data"/>.</summary>
    public bool IsInplaceConst => (_typeFlags & 0x8000) != 0;
    /// <summary>First child, or 1-based closure slot for non-inline Constant nodes.</summary>
    public Idx ChildIdx => Idx.Of((int)(ushort)(_data & 0xFFFF));
    /// <summary>Second child (nil for unary nodes and for New/Call/Invoke args — only used for control nodes).</summary>
    public Idx ExtraIdx => Idx.Of((int)(ushort)((_data >> 32) & 0xFFFF));
    /// <summary>Raw 8-byte constant bits when <see cref="IsInplaceConst"/> is true.</summary>
    public long Data => _data;
}

/// <summary>
/// Flat expression tree backed by a single flat Nodes array. Hold as a local or heap field —
/// do not pass by value (mutable struct; copy silently forks state).
/// </summary>
public struct ExpressionTree
{
    // First 16 nodes are on the stack; further nodes spill to a heap array.
    /// <summary>Flat node storage. First 16 nodes are stack-resident; further nodes spill to a heap array.</summary>
    public SmallList<ExpressionNode, Stack16<ExpressionNode>, NoArrayPool<ExpressionNode>> Nodes;
    // First 4 closure constants on stack.
    /// <summary>Closure-captured constants. First 4 are stack-resident.</summary>
    public SmallList<object, Stack4<object>, NoArrayPool<object>> ClosureConstants;
    /// <summary>Index of the root expression node (typically a Lambda).</summary>
    public Idx RootIdx;

    /// <summary>Total number of nodes in this tree.</summary>
    public int NodeCount => Nodes.Count;

    /// <summary>Returns a reference to the node at the given index.</summary>
    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref ExpressionNode NodeAt(Idx idx)
    {
        Debug.Assert(!idx.IsNil, "Cannot dereference a nil Idx");
        return ref Nodes.GetSurePresentRef(idx.It - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Idx AddNode(
        ExpressionType nodeType,
        Type type,
        object obj = null,
        Idx childIdx = default,
        Idx extraIdx = default)
    {
        ref var n = ref Nodes.AddDefaultAndGetRef();
        n._typeFlags = (short)nodeType;
        n.Type = type;
        n.Obj = obj;
        n._data = ((long)(ushort)extraIdx.It << 32) | (ushort)childIdx.It;
        n.NextIdx = 0;
        return Idx.Of(Nodes.Count); // Count already incremented by AddDefaultAndGetRef
    }

    // Types whose value fits in 64 bits — stored inline in _data to avoid boxing.
    private static bool FitsInInt64(Type t)
    {
        switch (Type.GetTypeCode(t))
        {
            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Single:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Double:
            case TypeCode.DateTime:
                return true;
            default:
                return false;
        }
    }

    // Encode an inline value as its int64 bit pattern (only call when FitsInInt64 is true).
    private static long ToInt64Bits(object value, Type t)
    {
        switch (Type.GetTypeCode(t))
        {
            case TypeCode.Int64:    return (long)value;
            case TypeCode.UInt64:   return (long)(ulong)value;
            case TypeCode.Double:   return BitConverter.DoubleToInt64Bits((double)value);
            case TypeCode.DateTime: return ((DateTime)value).ToBinary();
            case TypeCode.Int32:    return (int)value;
            case TypeCode.UInt32:   return (uint)value;
            case TypeCode.Boolean:  return (bool)value ? 1 : 0;
            case TypeCode.Single:   return FloatIntBits.FloatToInt((float)value);
            case TypeCode.Byte:     return (byte)value;
            case TypeCode.SByte:    return (sbyte)value;
            case TypeCode.Int16:    return (short)value;
            case TypeCode.UInt16:   return (ushort)value;
            case TypeCode.Char:     return (char)value;
            default:                return 0; // unreachable
        }
    }

    // Decode int64 bit pattern back to a boxed value (only call when FitsInInt64 is true).
    internal static object FromInt64Bits(long bits, Type t)
    {
        switch (Type.GetTypeCode(t))
        {
            case TypeCode.Int64:    return bits;
            case TypeCode.UInt64:   return (ulong)bits;
            case TypeCode.Double:   return BitConverter.Int64BitsToDouble(bits);
            case TypeCode.DateTime: return DateTime.FromBinary(bits);
            case TypeCode.Int32:    return (int)bits;
            case TypeCode.UInt32:   return (uint)bits;
            case TypeCode.Boolean:  return bits != 0;
            case TypeCode.Single:   return FloatIntBits.IntToFloat((int)bits);
            case TypeCode.Byte:     return (byte)bits;
            case TypeCode.SByte:    return (sbyte)bits;
            case TypeCode.Int16:    return (short)bits;
            case TypeCode.UInt16:   return (ushort)bits;
            case TypeCode.Char:     return (char)bits;
            default:                return null; // unreachable
        }
    }

    // Explicit-layout union to reinterpret float/int bits without Unsafe or BitConverter (portable across all TFMs).
    [StructLayout(LayoutKind.Explicit)]
    private struct FloatIntBits
    {
        [FieldOffset(0)] public float F;
        [FieldOffset(0)] public int I;
        public static int FloatToInt(float f) => new FloatIntBits { F = f }.I;
        public static float IntToFloat(int i) => new FloatIntBits { I = i }.F;
    }

    /// <summary>Adds a Constant node. Value types up to 8 bytes (int, bool, long, double, DateTime, etc.) are stored inline without boxing.</summary>
    public Idx Constant(object value, bool putIntoClosure = false)
    {
        if (value == null)
            return AddNode(ExpressionType.Constant, typeof(object));

        var type = value.GetType();
        if (!putIntoClosure)
        {
            if (FitsInInt64(type))
            {
                // IsInplaceConst (bit 15 of _typeFlags) = true; _data holds raw constant bits.
                ref var n = ref Nodes.AddDefaultAndGetRef();
                n._typeFlags = unchecked((short)((int)ExpressionType.Constant | 0x8000));
                n.Type = type;
                n.Obj = null;
                n._data = ToInt64Bits(value, type);
                n.NextIdx = 0;
                return Idx.Of(Nodes.Count);
            }
            // String, decimal, Guid, and other reference/large types go directly in Obj.
            return AddNode(ExpressionType.Constant, type, obj: value);
        }

        var ci = ClosureConstants.Count;
        ClosureConstants.Add(value);
        // ChildIdx (bits 0–15 of _data) = 1-based closure slot.
        return AddNode(ExpressionType.Constant, type, childIdx: Idx.Of(ci + 1));
    }

    /// <summary>Typed overload of <see cref="Constant(object,bool)"/>.</summary>
    public Idx Constant<T>(T value, bool putIntoClosure = false) =>
        Constant((object)value, putIntoClosure);

    /// <summary>Adds a Parameter node with the given type and optional name.</summary>
    public Idx Parameter(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, obj: name);

    /// <summary>Alias for <see cref="Parameter"/> — adds a block-local variable node.</summary>
    public Idx Variable(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, obj: name);

    /// <summary>Adds a Default(type) node.</summary>
    public Idx Default(Type type) =>
        AddNode(ExpressionType.Default, type);

    /// <summary>Adds a unary expression node.</summary>
    public Idx Unary(ExpressionType nodeType, Idx operand, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, obj: method, childIdx: operand);

    /// <summary>Adds a Convert node.</summary>
    public Idx Convert(Idx operand, Type toType) =>
        Unary(ExpressionType.Convert, operand, toType);

    /// <summary>Adds a Not node.</summary>
    public Idx Not(Idx operand) =>
        Unary(ExpressionType.Not, operand, typeof(bool));

    /// <summary>Adds a Negate node.</summary>
    public Idx Negate(Idx operand, Type type) =>
        Unary(ExpressionType.Negate, operand, type);

    /// <summary>Adds a binary expression node.</summary>
    public Idx Binary(ExpressionType nodeType, Idx left, Idx right, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, obj: method, childIdx: left, extraIdx: right);

    /// <summary>Adds an Add node.</summary>
    public Idx Add(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Add, left, right, type);

    /// <summary>Adds a Subtract node.</summary>
    public Idx Subtract(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Subtract, left, right, type);

    /// <summary>Adds a Multiply node.</summary>
    public Idx Multiply(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Multiply, left, right, type);

    /// <summary>Adds an Equal node (returns bool).</summary>
    public Idx Equal(Idx left, Idx right) =>
        Binary(ExpressionType.Equal, left, right, typeof(bool));

    /// <summary>Adds an Assign node.</summary>
    public Idx Assign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.Assign, target, value, type);

    /// <summary>Adds a New node calling the given constructor with the provided arguments.</summary>
    public Idx New(ConstructorInfo ctor, params Idx[] args) =>
        AddNode(ExpressionType.New, ctor.DeclaringType, obj: ctor, childIdx: LinkList(args));

    /// <summary>Adds a Call node. Pass <see cref="Idx.Nil"/> for <paramref name="instance"/> for static calls.</summary>
    public Idx Call(MethodInfo method, Idx instance, params Idx[] args)
    {
        var returnType = method.ReturnType == typeof(void) ? typeof(void) : method.ReturnType;
        var firstArgIdx = LinkList(args);
        return instance.IsNil
            ? AddNode(ExpressionType.Call, returnType, obj: method, childIdx: firstArgIdx)
            : AddNode(ExpressionType.Call, returnType, obj: method, childIdx: instance, extraIdx: firstArgIdx);
    }

    // Parameters stored in Obj as Idx[] rather than chained via NextIdx, because the same
    // parameter node may already have its NextIdx occupied as part of a New/Call argument chain.
    /// <summary>Adds a Lambda node. Sets <see cref="RootIdx"/> when <paramref name="isRoot"/> is true.</summary>
    public Idx Lambda(Type delegateType, Idx body, Idx[] parameters = null, bool isRoot = true)
    {
        var lambdaIdx = AddNode(ExpressionType.Lambda, delegateType, obj: parameters, childIdx: body);
        if (isRoot)
            RootIdx = lambdaIdx;
        return lambdaIdx;
    }

    /// <summary>Adds a Conditional (ternary) node.</summary>
    public Idx Conditional(Idx test, Idx ifTrue, Idx ifFalse, Type type)
    {
        NodeAt(ifTrue).NextIdx = ifFalse.It; // ifFalse hangs off ifTrue.NextIdx
        return AddNode(ExpressionType.Conditional, type, childIdx: test, extraIdx: ifTrue);
    }

    /// <summary>Adds a Block node containing the given expressions and optional block-local variables.</summary>
    public Idx Block(Type type, Idx[] exprs, Idx[] variables = null)
    {
        var firstExprIdx = LinkList(exprs);
        var firstVarIdx = variables == null || variables.Length == 0 ? Idx.Nil : LinkList(variables);
        return AddNode(ExpressionType.Block, type, childIdx: firstExprIdx, extraIdx: firstVarIdx);
    }

    // ── Additional convenience shorthands for binary ops ───────────────────────────────────────

    /// <summary>Adds a NotEqual node (returns bool).</summary>
    public Idx NotEqual(Idx left, Idx right) =>
        Binary(ExpressionType.NotEqual, left, right, typeof(bool));

    /// <summary>Adds a LessThan node (returns bool).</summary>
    public Idx LessThan(Idx left, Idx right) =>
        Binary(ExpressionType.LessThan, left, right, typeof(bool));

    /// <summary>Adds a LessThanOrEqual node (returns bool).</summary>
    public Idx LessThanOrEqual(Idx left, Idx right) =>
        Binary(ExpressionType.LessThanOrEqual, left, right, typeof(bool));

    /// <summary>Adds a GreaterThan node (returns bool).</summary>
    public Idx GreaterThan(Idx left, Idx right) =>
        Binary(ExpressionType.GreaterThan, left, right, typeof(bool));

    /// <summary>Adds a GreaterThanOrEqual node (returns bool).</summary>
    public Idx GreaterThanOrEqual(Idx left, Idx right) =>
        Binary(ExpressionType.GreaterThanOrEqual, left, right, typeof(bool));

    /// <summary>Adds an AndAlso (short-circuit &amp;&amp;) node.</summary>
    public Idx AndAlso(Idx left, Idx right) =>
        Binary(ExpressionType.AndAlso, left, right, typeof(bool));

    /// <summary>Adds an OrElse (short-circuit ||) node.</summary>
    public Idx OrElse(Idx left, Idx right) =>
        Binary(ExpressionType.OrElse, left, right, typeof(bool));

    /// <summary>Adds a Coalesce (??) node.</summary>
    public Idx Coalesce(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Coalesce, left, right, type);

    /// <summary>Adds an ArrayIndex node. The <paramref name="array"/> node must have an array type.</summary>
    public Idx ArrayIndex(Idx array, Idx index) =>
        Binary(ExpressionType.ArrayIndex, array, index, NodeAt(array).Type.GetElementType()
            ?? throw new ArgumentException("Array node type must be an array type.", nameof(array)));

    // ── Compound-assignment convenience shorthands ──────────────────────────────────────────────

    /// <summary>Adds an AddAssign node.</summary>
    public Idx AddAssign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.AddAssign, target, value, type);

    /// <summary>Adds a SubtractAssign node.</summary>
    public Idx SubtractAssign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.SubtractAssign, target, value, type);

    /// <summary>Adds a MultiplyAssign node.</summary>
    public Idx MultiplyAssign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.MultiplyAssign, target, value, type);

    // ── MemberAccess ────────────────────────────────────────────────────────────────────────────
    // Obj = MemberInfo; ChildIdx = instance (nil for static).

    /// <summary>Adds a MemberAccess node for a field or property. Pass <see cref="Idx.Nil"/> for static members.</summary>
    public Idx MemberAccess(Idx instance, MemberInfo member)
    {
        Type memberType;
        if (member is PropertyInfo pi)
            memberType = pi.PropertyType;
        else if (member is FieldInfo fi)
            memberType = fi.FieldType;
        else
            throw new ArgumentException($"MemberAccess requires a FieldInfo or PropertyInfo, got {member.GetType().Name}.", nameof(member));
        return AddNode(ExpressionType.MemberAccess, memberType, obj: member, childIdx: instance);
    }

    /// <summary>Adds a MemberAccess node for a field.</summary>
    public Idx Field(Idx instance, FieldInfo field) => MemberAccess(instance, field);

    /// <summary>Adds a MemberAccess node for a property.</summary>
    public Idx Property(Idx instance, PropertyInfo property) => MemberAccess(instance, property);

    // ── Invoke ──────────────────────────────────────────────────────────────────────────────────
    // ChildIdx = delegate expression; ExtraIdx = first argument (chained via NextIdx).

    /// <summary>Adds an Invoke node (delegate invocation).</summary>
    public Idx Invoke(Idx delegateExpr, Type returnType, params Idx[] args) =>
        AddNode(ExpressionType.Invoke, returnType, childIdx: delegateExpr, extraIdx: LinkList(args));

    // ── TypeIs / TypeEqual ──────────────────────────────────────────────────────────────────────
    // Obj = Type to test against; ChildIdx = expression.

    /// <summary>Adds a TypeIs node (returns bool; true when expr is a subtype of <paramref name="type"/>).</summary>
    public Idx TypeIs(Idx expr, Type type) =>
        AddNode(ExpressionType.TypeIs, typeof(bool), obj: type, childIdx: expr);

    /// <summary>Adds a TypeEqual node (returns bool; true when expr's exact runtime type equals <paramref name="type"/>).</summary>
    public Idx TypeEqual(Idx expr, Type type) =>
        AddNode(ExpressionType.TypeEqual, typeof(bool), obj: type, childIdx: expr);

    // ── NewArrayInit / NewArrayBounds ───────────────────────────────────────────────────────────
    // Type = array type; ChildIdx = first element/bound (chained via NextIdx).

    /// <summary>Adds a NewArrayInit node (creates and initializes a 1D array).</summary>
    public Idx NewArrayInit(Type elementType, params Idx[] elements) =>
        AddNode(ExpressionType.NewArrayInit, elementType.MakeArrayType(), childIdx: LinkList(elements));

    /// <summary>Adds a NewArrayBounds node (creates an array given dimension bounds).</summary>
    public Idx NewArrayBounds(Type elementType, params Idx[] bounds) =>
        AddNode(ExpressionType.NewArrayBounds, elementType.MakeArrayType(), childIdx: LinkList(bounds));

    /// <summary>Chains the given indices via <see cref="ExpressionNode.NextIdx"/> and returns the first index.</summary>
    public Idx LinkList(Idx[] indices)
    {
        if (indices == null || indices.Length == 0)
            return Idx.Nil;
        for (var i = 0; i < indices.Length - 1; i++)
            NodeAt(indices[i]).NextIdx = indices[i + 1].It;
        NodeAt(indices[indices.Length - 1]).NextIdx = 0; // reset in case node was previously linked
        return indices[0];
    }

    // Allocates an enumerator — suitable for tests and diagnostics; avoid in hot paths.
    /// <summary>Enumerates the sibling chain starting at <paramref name="head"/>. Allocates an enumerator — avoid in hot paths.</summary>
    public IEnumerable<Idx> Siblings(Idx head)
    {
        var cur = head;
        while (!cur.IsNil)
        {
            yield return cur;
            cur = Idx.Of(NodeAt(cur).NextIdx);
        }
    }

    // Builds body after registering params so they are found in paramMap when encountered in the body.
    /// <summary>Converts this flat tree to a <see cref="System.Linq.Expressions.Expression"/> rooted at <see cref="RootIdx"/>.</summary>
    public SysExpr ToSystemExpression()
    {
        var paramMap = default(SmallMap16<int, SysParam, IntEq>);
        return ToSystemExpression(RootIdx, ref paramMap);
    }

    private SysExpr ToSystemExpression(Idx nodeIdx, ref SmallMap16<int, SysParam, IntEq> paramMap)
    {
        if (nodeIdx.IsNil)
            throw new InvalidOperationException("Cannot convert nil Idx to System.Linq.Expressions");

        ref var node = ref NodeAt(nodeIdx);

        switch (node.NodeType)
        {
            case ExpressionType.Constant:
            {
                object value;
                if (node.IsInplaceConst)
                    value = FromInt64Bits(node.Data, node.Type);
                else if (node.ChildIdx.It > 0)
                    value = ClosureConstants.GetSurePresentRef(node.ChildIdx.It - 1);
                else
                    value = node.Obj;
                return SysExpr.Constant(value, node.Type);
            }

            case ExpressionType.Parameter:
            {
                ref var p = ref paramMap.Map.AddOrGetValueRef(nodeIdx.It, out var found);
                if (!found)
                    p = SysExpr.Parameter(node.Type, node.Obj as string);
                return p;
            }

            case ExpressionType.Default:
                return SysExpr.Default(node.Type);

            case ExpressionType.Lambda:
            {
                var paramIdxs = node.Obj as Idx[];
                var paramExprs = new List<SysParam>();
                if (paramIdxs != null)
                    foreach (var pIdx in paramIdxs)
                        paramExprs.Add((SysParam)ToSystemExpression(pIdx, ref paramMap));
                var body = ToSystemExpression(node.ChildIdx, ref paramMap);
                return SysExpr.Lambda(node.Type, body, paramExprs);
            }

            case ExpressionType.New:
                return SysExpr.New((ConstructorInfo)node.Obj, SiblingListSE(node.ChildIdx, ref paramMap));

            case ExpressionType.NewArrayInit:
                return SysExpr.NewArrayInit(node.Type.GetElementType(), SiblingListSE(node.ChildIdx, ref paramMap));

            case ExpressionType.NewArrayBounds:
                return SysExpr.NewArrayBounds(node.Type.GetElementType(), SiblingListSE(node.ChildIdx, ref paramMap));

            case ExpressionType.Call:
            {
                var method = (MethodInfo)node.Obj;
                return method.IsStatic
                    ? SysExpr.Call(method, SiblingListSE(node.ChildIdx, ref paramMap))
                    : SysExpr.Call(ToSystemExpression(node.ChildIdx, ref paramMap), method, SiblingListSE(node.ExtraIdx, ref paramMap));
            }

            case ExpressionType.Invoke:
                return SysExpr.Invoke(ToSystemExpression(node.ChildIdx, ref paramMap), SiblingListSE(node.ExtraIdx, ref paramMap));

            case ExpressionType.MemberAccess:
            {
                var member = (MemberInfo)node.Obj;
                return SysExpr.MakeMemberAccess(node.ChildIdx.IsNil ? null : ToSystemExpression(node.ChildIdx, ref paramMap), member);
            }

            case ExpressionType.TypeIs:
                return SysExpr.TypeIs(ToSystemExpression(node.ChildIdx, ref paramMap), (Type)node.Obj);

            case ExpressionType.TypeEqual:
                return SysExpr.TypeEqual(ToSystemExpression(node.ChildIdx, ref paramMap), (Type)node.Obj);

            case ExpressionType.Conditional:
                return SysExpr.Condition(
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    ToSystemExpression(node.ExtraIdx, ref paramMap),
                    ToSystemExpression(Idx.Of(NodeAt(node.ExtraIdx).NextIdx), ref paramMap),
                    node.Type);

            case ExpressionType.Block:
            {
                var exprs = SiblingListSE(node.ChildIdx, ref paramMap);
                if (node.ExtraIdx.IsNil)
                    return SysExpr.Block(node.Type, exprs);
                var vars = new List<SysParam>();
                var vCur = node.ExtraIdx;
                while (!vCur.IsNil)
                {
                    vars.Add((SysParam)ToSystemExpression(vCur, ref paramMap));
                    vCur = Idx.Of(NodeAt(vCur).NextIdx);
                }
                return SysExpr.Block(node.Type, vars, exprs);
            }

            default:
                // All Binary and Unary node types: use ExtraIdx presence to distinguish.
                if (!node.ExtraIdx.IsNil)
                    return SysExpr.MakeBinary(node.NodeType,
                        ToSystemExpression(node.ChildIdx, ref paramMap),
                        ToSystemExpression(node.ExtraIdx, ref paramMap),
                        false, node.Obj as MethodInfo);
                return SysExpr.MakeUnary(node.NodeType,
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    node.Type, node.Obj as MethodInfo);
        }
    }

    private List<SysExpr> SiblingListSE(Idx head, ref SmallMap16<int, SysParam, IntEq> paramMap)
    {
        var list = new List<SysExpr>();
        var cur = head;
        while (!cur.IsNil)
        {
            list.Add(ToSystemExpression(cur, ref paramMap));
            cur = Idx.Of(NodeAt(cur).NextIdx);
        }
        return list;
    }

#if LIGHT_EXPRESSION
    /// <summary>Converts this flat tree to a <see cref="FastExpressionCompiler.LightExpression.Expression"/> rooted at <see cref="RootIdx"/>.</summary>
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("FastExpressionCompiler is not supported in trimming scenarios.")]
    public FastExpressionCompiler.LightExpression.Expression ToLightExpression()
    {
        var paramMap = default(SmallMap16<int, FastExpressionCompiler.LightExpression.ParameterExpression, IntEq>);
        return ToLightExpression(RootIdx, ref paramMap);
    }

    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("FastExpressionCompiler is not supported in trimming scenarios.")]
    private FastExpressionCompiler.LightExpression.Expression ToLightExpression(
        Idx nodeIdx, ref SmallMap16<int, FastExpressionCompiler.LightExpression.ParameterExpression, IntEq> paramMap)
    {
        if (nodeIdx.IsNil)
            throw new InvalidOperationException("Cannot convert nil Idx to LightExpression");

        ref var node = ref NodeAt(nodeIdx);

        switch (node.NodeType)
        {
            case ExpressionType.Constant:
            {
                object value;
                if (node.IsInplaceConst)
                    value = FromInt64Bits(node.Data, node.Type);
                else if (node.ChildIdx.It > 0)
                    value = ClosureConstants.GetSurePresentRef(node.ChildIdx.It - 1);
                else
                    value = node.Obj;
                return FastExpressionCompiler.LightExpression.Expression.Constant(value, node.Type);
            }

            case ExpressionType.Parameter:
            {
                ref var p = ref paramMap.Map.AddOrGetValueRef(nodeIdx.It, out var found);
                if (!found)
                    p = FastExpressionCompiler.LightExpression.Expression.Parameter(node.Type, node.Obj as string);
                return p;
            }

            case ExpressionType.Default:
                return FastExpressionCompiler.LightExpression.Expression.Default(node.Type);

            case ExpressionType.Lambda:
            {
                var paramIdxs = node.Obj as Idx[];
                var paramExprs = new List<FastExpressionCompiler.LightExpression.ParameterExpression>();
                if (paramIdxs != null)
                    foreach (var pIdx in paramIdxs)
                        paramExprs.Add((FastExpressionCompiler.LightExpression.ParameterExpression)ToLightExpression(pIdx, ref paramMap));
                var body = ToLightExpression(node.ChildIdx, ref paramMap);
                return FastExpressionCompiler.LightExpression.Expression.Lambda(node.Type, body, paramExprs);
            }

            case ExpressionType.New:
                return FastExpressionCompiler.LightExpression.Expression.New(
                    (ConstructorInfo)node.Obj, SiblingListLE(node.ChildIdx, ref paramMap));

            case ExpressionType.NewArrayInit:
                return FastExpressionCompiler.LightExpression.Expression.NewArrayInit(
                    node.Type.GetElementType(), SiblingListLE(node.ChildIdx, ref paramMap));

            case ExpressionType.NewArrayBounds:
                return FastExpressionCompiler.LightExpression.Expression.NewArrayBounds(
                    node.Type.GetElementType(), SiblingListLE(node.ChildIdx, ref paramMap));

            case ExpressionType.Call:
            {
                var method = (MethodInfo)node.Obj;
                return method.IsStatic
                    ? FastExpressionCompiler.LightExpression.Expression.Call(method, SiblingListLE(node.ChildIdx, ref paramMap))
                    : FastExpressionCompiler.LightExpression.Expression.Call(ToLightExpression(node.ChildIdx, ref paramMap), method, SiblingListLE(node.ExtraIdx, ref paramMap));
            }

            case ExpressionType.Invoke:
                return FastExpressionCompiler.LightExpression.Expression.Invoke(
                    ToLightExpression(node.ChildIdx, ref paramMap), SiblingListLE(node.ExtraIdx, ref paramMap));

            case ExpressionType.MemberAccess:
            {
                var member = (MemberInfo)node.Obj;
                var instance = node.ChildIdx.IsNil ? null : ToLightExpression(node.ChildIdx, ref paramMap);
                if (member is FieldInfo fi)
                    return FastExpressionCompiler.LightExpression.Expression.Field(instance, fi);
                return FastExpressionCompiler.LightExpression.Expression.Property(instance, (PropertyInfo)member);
            }

            case ExpressionType.TypeIs:
                return FastExpressionCompiler.LightExpression.Expression.TypeIs(
                    ToLightExpression(node.ChildIdx, ref paramMap), (Type)node.Obj);

            case ExpressionType.TypeEqual:
                return FastExpressionCompiler.LightExpression.Expression.TypeEqual(
                    ToLightExpression(node.ChildIdx, ref paramMap), (Type)node.Obj);

            case ExpressionType.Conditional:
                return FastExpressionCompiler.LightExpression.Expression.Condition(
                    ToLightExpression(node.ChildIdx, ref paramMap),
                    ToLightExpression(node.ExtraIdx, ref paramMap),
                    ToLightExpression(Idx.Of(NodeAt(node.ExtraIdx).NextIdx), ref paramMap),
                    node.Type);

            case ExpressionType.Block:
            {
                var exprs = SiblingListLE(node.ChildIdx, ref paramMap);
                if (node.ExtraIdx.IsNil)
                    return FastExpressionCompiler.LightExpression.Expression.Block(node.Type, exprs);
                var vars = new List<FastExpressionCompiler.LightExpression.ParameterExpression>();
                var vCur = node.ExtraIdx;
                while (!vCur.IsNil)
                {
                    vars.Add((FastExpressionCompiler.LightExpression.ParameterExpression)ToLightExpression(vCur, ref paramMap));
                    vCur = Idx.Of(NodeAt(vCur).NextIdx);
                }
                return FastExpressionCompiler.LightExpression.Expression.Block(node.Type, vars, exprs);
            }

            default:
                // All Binary and Unary node types: use ExtraIdx presence to distinguish.
                if (!node.ExtraIdx.IsNil)
                    return FastExpressionCompiler.LightExpression.Expression.MakeBinary(node.NodeType,
                        ToLightExpression(node.ChildIdx, ref paramMap),
                        ToLightExpression(node.ExtraIdx, ref paramMap),
                        false, node.Obj as MethodInfo);
                return FastExpressionCompiler.LightExpression.Expression.MakeUnary(node.NodeType,
                    ToLightExpression(node.ChildIdx, ref paramMap),
                    node.Type, node.Obj as MethodInfo);
        }
    }

    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("FastExpressionCompiler is not supported in trimming scenarios.")]
    private List<FastExpressionCompiler.LightExpression.Expression> SiblingListLE(
        Idx head, ref SmallMap16<int, FastExpressionCompiler.LightExpression.ParameterExpression, IntEq> paramMap)
    {
        var list = new List<FastExpressionCompiler.LightExpression.Expression>();
        var cur = head;
        while (!cur.IsNil)
        {
            list.Add(ToLightExpression(cur, ref paramMap));
            cur = Idx.Of(NodeAt(cur).NextIdx);
        }
        return list;
    }
#endif

    // O(n) structural equality — no traversal, single pass over the flat arrays.
    /// <summary>O(n) structural equality check. Compares both trees node-by-node in a single pass — no recursive traversal.</summary>
    public static bool StructurallyEqual(ref ExpressionTree a, ref ExpressionTree b)
    {
        if (a.NodeCount != b.NodeCount) return false;
        if (a.ClosureConstants.Count != b.ClosureConstants.Count) return false;
        for (var i = 0; i < a.NodeCount; i++)
        {
            ref var na = ref a.Nodes.GetSurePresentRef(i);
            ref var nb = ref b.Nodes.GetSurePresentRef(i);
            if (na._typeFlags != nb._typeFlags) return false;
            if (na.Type != nb.Type) return false;
            if (!ObjEqual(na.Obj, nb.Obj)) return false;
            if (na.NextIdx != nb.NextIdx) return false;
            if (na._data != nb._data) return false;
        }
        for (var i = 0; i < a.ClosureConstants.Count; i++)
            if (!Equals(a.ClosureConstants.GetSurePresentRef(i),
                        b.ClosureConstants.GetSurePresentRef(i)))
                return false;
        return true;
    }

    private static bool ObjEqual(object objA, object objB)
    {
        // Lambda Obj is Idx[] — Equals() on arrays checks reference equality, not contents.
        if (objA is Idx[] ia && objB is Idx[] ib)
        {
            if (ia.Length != ib.Length) return false;
            for (var k = 0; k < ia.Length; k++)
                if (ia[k].It != ib[k].It) return false;
            return true;
        }
        return Equals(objA, objB);
    }

    /// <summary>Returns a human-readable dump of all nodes and closure constants for diagnostics.</summary>
    public string Dump()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ExpressionTree  NodeCount={NodeCount}  ClosureConstants={ClosureConstants.Count}  RootIdx={RootIdx}");
        for (var i = 0; i < NodeCount; i++)
        {
            ref var n = ref Nodes.GetSurePresentRef(i);
            var constStr = n.NodeType == ExpressionType.Constant
                ? (n.IsInplaceConst        ? $"inline:{FromInt64Bits(n.Data, n.Type)}" :
                   n.ChildIdx.It > 0       ? $"closure[{n.ChildIdx.It - 1}]" :
                   $"obj:{n.Obj}")
                : null;
            sb.AppendLine(
                $"  [{i + 1}] {n.NodeType,-22} type={n.Type?.Name,-14} " +
                $"{(constStr != null ? $"val={constStr,-28}" : $"obj={ObjStr(n.Obj),-28}")} " +
                $"child={n.ChildIdx}  extra={n.ExtraIdx}  next={n.NextIdx}");
        }
        if (ClosureConstants.Count > 0)
        {
            sb.AppendLine("  Closure constants:");
            for (var i = 0; i < ClosureConstants.Count; i++)
                sb.AppendLine($"    [{i}] = {ClosureConstants.GetSurePresentRef(i)}");
        }
        return sb.ToString();
    }

    private static string ObjStr(object obj) =>
        obj == null ? "—" :
        obj is MethodBase mb ? mb.Name :
        obj is Idx[] idxArr ? $"params[{string.Join(",", Enumerable.Select(idxArr, x => x.It))}]" :
        obj.ToString();
}
