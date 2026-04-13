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
using FastExpressionCompiler.ImTools;

using SysExpr = System.Linq.Expressions.Expression;
using SysParam = System.Linq.Expressions.ParameterExpression;

/// <summary>1-based index into <see cref="ExpressionTree.Nodes"/>. <c>default</c> == nil.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct Idx : IEquatable<Idx>
{
    /// <summary>Raw 1-based index; 0 means nil.</summary>
    public int It;

    /// <summary>True when this index is nil (unset).</summary>
    public bool IsNil => It == 0;
    /// <summary>The nil sentinel value.</summary>
    public static Idx Nil => default;

    /// <summary>Creates a 1-based index from the given value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Idx Of(int oneBasedIndex) => new Idx { It = oneBasedIndex };

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
/// Fat node in <see cref="ExpressionTree.Nodes"/>. Intrusive linked-list tree encoding:
/// <list type="table">
///   <item><term>Constant</term>
///     <description>
///       ExtraIdx.It == 0 (nil): value is in Info (boxed, or null for null constant).<br/>
///       ExtraIdx.It &gt; 0: value is ClosureConstants[ExtraIdx.It - 1] (1-based).<br/>
///       ExtraIdx.It == -1: int32-fitting value (bool/byte/int/float/…) stored inline in ChildIdx.It bits — no boxing.
///     </description>
///   </item>
///   <item><term>Parameter</term>  <description>Info = name (string or null).</description></item>
///   <item><term>Unary</term>      <description>Info = MethodInfo (nullable), ChildIdx = operand.</description></item>
///   <item><term>Binary</term>     <description>Info = MethodInfo (nullable), ChildIdx = left, ExtraIdx = right.</description></item>
///   <item><term>New</term>        <description>Info = ConstructorInfo, ChildIdx = first arg (chained via NextIdx).</description></item>
///   <item><term>Call</term>       <description>Info = MethodInfo, ChildIdx = instance-or-first-static-arg, ExtraIdx = first arg for instance calls.</description></item>
///   <item><term>Lambda</term>     <description>Info = Idx[] of params, ChildIdx = body. Params stored in Info rather than NextIdx chain because the same parameter node may already participate as a New/Call argument.</description></item>
///   <item><term>Block</term>      <description>ChildIdx = first expr, ExtraIdx = first variable (both chained via NextIdx).</description></item>
///   <item><term>Conditional</term><description>ChildIdx = test, ExtraIdx = ifTrue; ifFalse = ifTrue.NextIdx.</description></item>
/// </list>
/// <para>
/// Layout: 32 bytes on 64-bit (refs first eliminates 4-byte padding after NodeType).<br/>
/// vs LightExpression heap objects (16-byte header + fields):<br/>
///   Constant/Parameter: ~40 bytes heap | Binary/Unary: ~48–56 bytes heap
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ExpressionNode  // 32 bytes: Type(8)+Info(8)+NodeType(4)+NextIdx(4)+ChildIdx(4)+ExtraIdx(4)
{
    // Reference fields placed first to avoid 4-byte padding that would appear after NodeType.
    /// <summary>Result type of this node.</summary>
    public Type Type;
    /// <summary>Method/constructor for Call/New/Unary/Binary; parameter name for Parameter; closure key for Constant; parameter <see cref="Idx"/> array for Lambda.</summary>
    public object Info;
    /// <summary>Expression kind (mirrors <see cref="System.Linq.Expressions.ExpressionType"/>).</summary>
    public ExpressionType NodeType;
    /// <summary>Next sibling in an intrusive linked list (arguments, block expressions, etc.).</summary>
    public Idx NextIdx;
    /// <summary>First child node, or for Constant with ExtraIdx.It==-1: raw int32 value bits.</summary>
    public Idx ChildIdx;
    /// <summary>
    /// Second child node; for Constant: 0=value in Info, positive=ClosureConstants index (1-based), -1=inline bits in ChildIdx.It.
    /// </summary>
    public Idx ExtraIdx;
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
        object info = null,
        Idx childIdx = default,
        Idx extraIdx = default)
    {
        ref var n = ref Nodes.AddDefaultAndGetRef();
        n.NodeType = nodeType;
        n.Type = type;
        n.Info = info;
        n.ChildIdx = childIdx;
        n.ExtraIdx = extraIdx;
        n.NextIdx = Idx.Nil;
        return Idx.Of(Nodes.Count); // Count already incremented by AddDefaultAndGetRef
    }

    // Types whose value fits in 32 bits — stored inline in ChildIdx.It to avoid boxing.
    private static bool FitsInInt32(Type t) =>
        t == typeof(int)   || t == typeof(uint)   || t == typeof(bool)  || t == typeof(float) ||
        t == typeof(byte)  || t == typeof(sbyte)  || t == typeof(short) || t == typeof(ushort) ||
        t == typeof(char);

    // Encode an inline value as its int32 bit pattern (only call when FitsInInt32 is true).
    private static int ToInt32Bits(object value, Type t)
    {
        if (t == typeof(int))    return (int)value;
        if (t == typeof(uint))   return (int)(uint)value;   // reinterpret bits
        if (t == typeof(bool))   return (bool)value ? 1 : 0;
        if (t == typeof(float))  return FloatIntBits.FloatToInt((float)value);
        if (t == typeof(byte))   return (byte)value;
        if (t == typeof(sbyte))  return (sbyte)value;
        if (t == typeof(short))  return (short)value;
        if (t == typeof(ushort)) return (ushort)value;
        if (t == typeof(char))   return (char)value;
        return 0; // unreachable
    }

    // Decode int32 bit pattern back to a boxed value (only call when FitsInInt32 is true).
    internal static object FromInt32Bits(int bits, Type t)
    {
        if (t == typeof(int))    return bits;
        if (t == typeof(uint))   return (uint)bits;
        if (t == typeof(bool))   return bits != 0;
        if (t == typeof(float))  return FloatIntBits.IntToFloat(bits);
        if (t == typeof(byte))   return (byte)bits;
        if (t == typeof(sbyte))  return (sbyte)bits;
        if (t == typeof(short))  return (short)bits;
        if (t == typeof(ushort)) return (ushort)bits;
        if (t == typeof(char))   return (char)bits;
        return null; // unreachable
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

    // Types not fitting in int32 but still safe to keep inline in Info (no special closure treatment needed).
    private static bool IsInfoInline(Type t) =>
        t == typeof(string) || t == typeof(long)    || t == typeof(double) ||
        t == typeof(decimal)|| t == typeof(DateTime)|| t == typeof(Guid);

    /// <summary>Adds a Constant node. Small value types (int, bool, float, etc.) are stored inline without boxing.</summary>
    public Idx Constant(object value, bool putIntoClosure = false)
    {
        if (value == null)
            return AddNode(ExpressionType.Constant, typeof(object));

        var type = value.GetType();
        if (!putIntoClosure)
        {
            if (FitsInInt32(type))
                // ExtraIdx.It == -1 is the "inline bits" sentinel; ChildIdx.It holds the value.
                return AddNode(ExpressionType.Constant, type,
                    childIdx: new Idx { It = ToInt32Bits(value, type) },
                    extraIdx: new Idx { It = -1 });
            if (IsInfoInline(type))
                return AddNode(ExpressionType.Constant, type, info: value);
        }

        var ci = ClosureConstants.Count;
        ClosureConstants.Add(value);
        // ExtraIdx.It > 0 (1-based) identifies the closure constant slot.
        return AddNode(ExpressionType.Constant, type, extraIdx: new Idx { It = ci + 1 });
    }

    /// <summary>Typed overload of <see cref="Constant(object,bool)"/>.</summary>
    public Idx Constant<T>(T value, bool putIntoClosure = false) =>
        Constant((object)value, putIntoClosure);

    /// <summary>Adds a Parameter node with the given type and optional name.</summary>
    public Idx Parameter(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    /// <summary>Alias for <see cref="Parameter"/> — adds a block-local variable node.</summary>
    public Idx Variable(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, info: name);

    /// <summary>Adds a Default(type) node.</summary>
    public Idx Default(Type type) =>
        AddNode(ExpressionType.Default, type);

    /// <summary>Adds a unary expression node.</summary>
    public Idx Unary(ExpressionType nodeType, Idx operand, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: operand);

    /// <summary>Adds a Convert node.</summary>
    public Idx Convert(Idx operand, Type toType) =>
        Unary(ExpressionType.Convert, operand, toType);

    /// <summary>Adds a Not node.</summary>
    public Idx Not(Idx operand) =>
        Unary(ExpressionType.Not, operand, typeof(bool));

    /// <summary>Adds a Negate node, inferring the type from <paramref name="operand"/>.</summary>
    public Idx Negate(Idx operand) =>
        Unary(ExpressionType.Negate, operand, NodeAt(operand).Type);
    /// <summary>Adds a Negate node with an explicit result type.</summary>
    public Idx Negate(Idx operand, Type type) =>
        Unary(ExpressionType.Negate, operand, type);

    /// <summary>Adds a binary expression node with an explicit result type.</summary>
    public Idx Binary(ExpressionType nodeType, Idx left, Idx right, Type type, MethodInfo method = null) =>
        AddNode(nodeType, type, info: method, childIdx: left, extraIdx: right);
    /// <summary>Adds a binary expression node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Binary(ExpressionType nodeType, Idx left, Idx right, MethodInfo method = null) =>
        Binary(nodeType, left, right, NodeAt(left).Type, method);

    /// <summary>Adds an Add node with an explicit result type.</summary>
    public Idx Add(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Add, left, right, type);
    /// <summary>Adds an Add node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Add(Idx left, Idx right) =>
        Binary(ExpressionType.Add, left, right, NodeAt(left).Type);

    /// <summary>Adds a Subtract node with an explicit result type.</summary>
    public Idx Subtract(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Subtract, left, right, type);
    /// <summary>Adds a Subtract node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Subtract(Idx left, Idx right) =>
        Binary(ExpressionType.Subtract, left, right, NodeAt(left).Type);

    /// <summary>Adds a Multiply node with an explicit result type.</summary>
    public Idx Multiply(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Multiply, left, right, type);
    /// <summary>Adds a Multiply node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Multiply(Idx left, Idx right) =>
        Binary(ExpressionType.Multiply, left, right, NodeAt(left).Type);

    /// <summary>Adds a Divide node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Divide(Idx left, Idx right) =>
        Binary(ExpressionType.Divide, left, right, NodeAt(left).Type);
    /// <summary>Adds a Divide node with an explicit result type.</summary>
    public Idx Divide(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Divide, left, right, type);

    /// <summary>Adds a Modulo node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx Modulo(Idx left, Idx right) =>
        Binary(ExpressionType.Modulo, left, right, NodeAt(left).Type);
    /// <summary>Adds a Modulo node with an explicit result type.</summary>
    public Idx Modulo(Idx left, Idx right, Type type) =>
        Binary(ExpressionType.Modulo, left, right, type);

    /// <summary>Adds an Equal node (returns bool).</summary>
    public Idx Equal(Idx left, Idx right) =>
        Binary(ExpressionType.Equal, left, right, typeof(bool));
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
    /// <summary>Adds an AndAlso (short-circuit &amp;&amp;) node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx AndAlso(Idx left, Idx right) =>
        Binary(ExpressionType.AndAlso, left, right, NodeAt(left).Type);
    /// <summary>Adds an OrElse (short-circuit ||) node, inferring the result type from <paramref name="left"/>.</summary>
    public Idx OrElse(Idx left, Idx right) =>
        Binary(ExpressionType.OrElse, left, right, NodeAt(left).Type);

    /// <summary>Adds an Assign node with an explicit result type.</summary>
    public Idx Assign(Idx target, Idx value, Type type) =>
        Binary(ExpressionType.Assign, target, value, type);
    /// <summary>Adds an Assign node, inferring the result type from <paramref name="target"/>.</summary>
    public Idx Assign(Idx target, Idx value) =>
        Binary(ExpressionType.Assign, target, value, NodeAt(target).Type);

    /// <summary>Adds a New node calling the given constructor with the provided arguments.</summary>
    public Idx New(ConstructorInfo ctor, params Idx[] args)
    {
        var firstArgIdx = LinkList(args);
        return AddNode(ExpressionType.New, ctor.DeclaringType, info: ctor, childIdx: firstArgIdx);
    }

    /// <summary>Adds a Call node. Pass <see cref="Idx.Nil"/> for <paramref name="instance"/> for static calls.</summary>
    public Idx Call(MethodInfo method, Idx instance, params Idx[] args)
    {
        var returnType = method.ReturnType == typeof(void) ? typeof(void) : method.ReturnType;
        var firstArgIdx = LinkList(args);
        return instance.IsNil
            ? AddNode(ExpressionType.Call, returnType, info: method, childIdx: firstArgIdx)
            : AddNode(ExpressionType.Call, returnType, info: method, childIdx: instance, extraIdx: firstArgIdx);
    }

    // Parameters stored in Info as Idx[] rather than chained via NextIdx, because the same
    // parameter node may already have its NextIdx used as part of a New/Call argument chain.
    /// <summary>Adds a Lambda node. Sets <see cref="RootIdx"/> when <paramref name="isRoot"/> is true.</summary>
    public Idx Lambda(Type delegateType, Idx body, Idx[] parameters = null, bool isRoot = true)
    {
        var lambdaIdx = AddNode(ExpressionType.Lambda, delegateType, info: parameters, childIdx: body);
        if (isRoot)
            RootIdx = lambdaIdx;
        return lambdaIdx;
    }

    /// <summary>Adds a Conditional (ternary) node.</summary>
    public Idx Conditional(Idx test, Idx ifTrue, Idx ifFalse, Type type)
    {
        NodeAt(ifTrue).NextIdx = ifFalse; // ifFalse hangs off ifTrue.NextIdx
        return AddNode(ExpressionType.Conditional, type, childIdx: test, extraIdx: ifTrue);
    }

    /// <summary>Adds a Block node containing the given expressions and optional block-local variables.</summary>
    public Idx Block(Type type, Idx[] exprs, Idx[] variables = null)
    {
        var firstExprIdx = LinkList(exprs);
        var firstVarIdx = variables == null || variables.Length == 0 ? Idx.Nil : LinkList(variables);
        return AddNode(ExpressionType.Block, type, childIdx: firstExprIdx, extraIdx: firstVarIdx);
    }

    /// <summary>Chains the given indices via <see cref="ExpressionNode.NextIdx"/> and returns the first index.</summary>
    public Idx LinkList(Idx[] indices)
    {
        if (indices == null || indices.Length == 0)
            return Idx.Nil;
        for (var i = 0; i < indices.Length - 1; i++)
            NodeAt(indices[i]).NextIdx = indices[i + 1];
        NodeAt(indices[indices.Length - 1]).NextIdx = Idx.Nil; // reset in case node was previously linked
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
            cur = NodeAt(cur).NextIdx;
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
                if (node.ExtraIdx.It > 0)
                    value = ClosureConstants.GetSurePresentRef(node.ExtraIdx.It - 1);
                else if (node.ExtraIdx.It == -1)
                    value = FromInt32Bits(node.ChildIdx.It, node.Type);
                else
                    value = node.Info;
                return SysExpr.Constant(value, node.Type);
            }

            case ExpressionType.Parameter:
            {
                ref var p = ref paramMap.Map.AddOrGetValueRef(nodeIdx.It, out var found);
                if (!found)
                    p = SysExpr.Parameter(node.Type, node.Info as string);
                return p;
            }

            case ExpressionType.Default:
                return SysExpr.Default(node.Type);

            case ExpressionType.Lambda:
            {
                var paramIdxs = node.Info as Idx[];
                var paramExprs = new List<SysParam>();
                if (paramIdxs != null)
                    foreach (var pIdx in paramIdxs)
                        paramExprs.Add((SysParam)ToSystemExpression(pIdx, ref paramMap));
                var body = ToSystemExpression(node.ChildIdx, ref paramMap);
                return SysExpr.Lambda(node.Type, body, paramExprs);
            }

            case ExpressionType.New:
                return SysExpr.New((ConstructorInfo)node.Info, SiblingList(node.ChildIdx, ref paramMap));

            case ExpressionType.Call:
            {
                var method = (MethodInfo)node.Info;
                return method.IsStatic
                    ? SysExpr.Call(method, SiblingList(node.ChildIdx, ref paramMap))
                    : SysExpr.Call(ToSystemExpression(node.ChildIdx, ref paramMap), method, SiblingList(node.ExtraIdx, ref paramMap));
            }

            case ExpressionType.Add:
            case ExpressionType.AddChecked:
            case ExpressionType.Subtract:
            case ExpressionType.SubtractChecked:
            case ExpressionType.Multiply:
            case ExpressionType.MultiplyChecked:
            case ExpressionType.Divide:
            case ExpressionType.Modulo:
            case ExpressionType.And:
            case ExpressionType.AndAlso:
            case ExpressionType.Or:
            case ExpressionType.OrElse:
            case ExpressionType.ExclusiveOr:
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.Assign:
            case ExpressionType.LeftShift:
            case ExpressionType.RightShift:
            case ExpressionType.Power:
            case ExpressionType.Coalesce:
                return SysExpr.MakeBinary(node.NodeType,
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    ToSystemExpression(node.ExtraIdx, ref paramMap),
                    false, node.Info as MethodInfo);

            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
            case ExpressionType.Not:
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
            case ExpressionType.ArrayLength:
            case ExpressionType.Quote:
            case ExpressionType.TypeAs:
            case ExpressionType.Throw:
            case ExpressionType.Unbox:
            case ExpressionType.Increment:
            case ExpressionType.Decrement:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostDecrementAssign:
                return SysExpr.MakeUnary(node.NodeType,
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    node.Type, node.Info as MethodInfo);

            case ExpressionType.Conditional:
                return SysExpr.Condition(
                    ToSystemExpression(node.ChildIdx, ref paramMap),
                    ToSystemExpression(node.ExtraIdx, ref paramMap),
                    ToSystemExpression(NodeAt(node.ExtraIdx).NextIdx, ref paramMap),
                    node.Type);

            case ExpressionType.Block:
            {
                var exprs = SiblingList(node.ChildIdx, ref paramMap);
                if (node.ExtraIdx.IsNil)
                    return SysExpr.Block(node.Type, exprs);
                var vars = new List<SysParam>();
                var vCur = node.ExtraIdx;
                while (!vCur.IsNil)
                {
                    vars.Add((SysParam)ToSystemExpression(vCur, ref paramMap));
                    vCur = NodeAt(vCur).NextIdx;
                }
                return SysExpr.Block(node.Type, vars, exprs);
            }

            default:
                throw new NotSupportedException(
                    $"FlatExpression → System.Linq.Expressions: NodeType {node.NodeType} is not yet mapped.");
        }
    }

    private List<SysExpr> SiblingList(Idx head, ref SmallMap16<int, SysParam, IntEq> paramMap)
    {
        var list = new List<SysExpr>();
        var cur = head;
        while (!cur.IsNil)
        {
            list.Add(ToSystemExpression(cur, ref paramMap));
            cur = NodeAt(cur).NextIdx;
        }
        return list;
    }

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
            if (na.NodeType != nb.NodeType) return false;
            if (na.Type != nb.Type) return false;
            if (!InfoEqual(na.Info, nb.Info)) return false;
            if (na.NextIdx.It != nb.NextIdx.It) return false;
            if (na.ChildIdx.It != nb.ChildIdx.It) return false;
            if (na.ExtraIdx.It != nb.ExtraIdx.It) return false;
        }
        for (var i = 0; i < a.ClosureConstants.Count; i++)
            if (!Equals(a.ClosureConstants.GetSurePresentRef(i),
                        b.ClosureConstants.GetSurePresentRef(i)))
                return false;
        return true;
    }

    private static bool InfoEqual(object infoA, object infoB)
    {
        // Lambda Info is Idx[] — Equals() on arrays checks reference equality, not contents.
        if (infoA is Idx[] ia && infoB is Idx[] ib)
        {
            if (ia.Length != ib.Length) return false;
            for (var k = 0; k < ia.Length; k++)
                if (ia[k].It != ib[k].It) return false;
            return true;
        }
        return Equals(infoA, infoB);
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
                ? (n.ExtraIdx.It > 0   ? $"closure[{n.ExtraIdx.It - 1}]" :
                   n.ExtraIdx.It == -1 ? $"inline:{FromInt32Bits(n.ChildIdx.It, n.Type)}" :
                   $"info:{n.Info}")
                : null;
            sb.AppendLine(
                $"  [{i + 1}] {n.NodeType,-22} type={n.Type?.Name,-14} " +
                $"{(constStr != null ? $"val={constStr,-28}" : $"info={InfoStr(n.Info),-28}")} " +
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

    private static string InfoStr(object info) =>
        info == null ? "—" :
        info is MethodBase mb ? mb.Name :
        info is Idx[] idxArr ? $"params[{string.Join(",", Enumerable.Select(idxArr, x => x.It))}]" :
        info.ToString();
}
