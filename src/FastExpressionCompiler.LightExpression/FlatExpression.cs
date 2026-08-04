namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FastExpressionCompiler.LightExpression.ImTools;
using SysCatchBlock = System.Linq.Expressions.CatchBlock;
using SysElementInit = System.Linq.Expressions.ElementInit;
using SysExpr = System.Linq.Expressions.Expression;
using SysLabelTarget = System.Linq.Expressions.LabelTarget;
using SysMemberBinding = System.Linq.Expressions.MemberBinding;
using SysParameterExpression = System.Linq.Expressions.ParameterExpression;
using SysSwitchCase = System.Linq.Expressions.SwitchCase;
using ChildIdxs = LightExpression.ImTools.SmallList<ushort, LightExpression.ImTools.Stack8<ushort>, LightExpression.ImTools.NoArrayPool<ushort>>;

// todo: @wip using Idx = ushort

/// <summary>Kind if the node payload.</summary>
public enum ExprNodeKind : byte
{
    /// <summary>Represents a regular expression node.</summary>
    Expression = 0,
    /// <summary>Represents a switch case sub-node.</summary>
    SwitchCase,
    /// <summary>Represents a switch cases sub-node.</summary>
    SwitchCases,
    /// <summary>Represents a catch block payload.</summary>
    CatchBlock,
    /// <summary>Represents a label target payload.</summary>
    LabelTarget,
    /// <summary>Represents a member-assignment binding payload.</summary>
    MemberAssignment,
    /// <summary>Represents a nested member-binding payload.</summary>
    MemberMemberBinding,
    /// <summary>Represents a list-binding payload.</summary>
    MemberListBinding,
    /// <summary>Represents an element initializer payload.</summary>
    ElementInit,
    /// <summary>Represents an internal object-reference metadata node.</summary>
    ObjectReference,
    /// <summary>Expressions in Block as a separate sub-node Block child node, it has the same ExpressionType.Block but this kind.</summary>
    BlockExprs,
    /// <summary>Represents an internal pair of UInt16 values.</summary>
    UInt16Pair,
}

/// <summary>Stores one flat expression node and its child-link metadata in 24 bytes on 64-bit runtimes.</summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct ExprNode
{
    // _meta layout: bits [31:24]=NodeType | [23:20]=Flags | [19:16]=Kind | [15:0]=NextIdx
    private const int MetaTagShift = 16;
    // _data layout: bits [31:16]=ChildCount | [15:0]=ChildIdx  (or full uint for inline constants)
    private const int ChildCountShift = 16;
    private const uint ChildCountMask = 0xFFFF0000u;
    private const uint FirstChildIdxMask = 0xFFFFu;
    private const int FlagsShift = 4;

    /// <summary>Sentinel placed in <see cref="Obj"/> to indicate the node holds a small primitive constant in <see cref="InlineValue"/>.</summary>
    internal static readonly object InlineValueMarker = new();

    /// <summary>Gets or sets the runtime type of the represented node.</summary>
    [FieldOffset(0)]
    public Type Type;

    /// <summary>Gets or sets the runtime payload associated with the node.</summary>
    [FieldOffset(8)]
    public object Obj;

    /// <summary>ChildCount(16b) | ChildIdx(16b) or raw 32-bit inline constant value.</summary>
    [FieldOffset(16)]
    private uint _child;

    /// <summary>Index of the next sibling node if any.</summary>
    [FieldOffset(20)]
    public ushort NextIdx;

    [FieldOffset(22)]
    private byte _nodeType;

    /// <summary>4bits:Flags|4bits:Kind</summary>
    [FieldOffset(23)]
    public byte FlagsAndKind;

    /// <summary>Gets the expression kind encoded for this node.</summary>
    public ExpressionType NodeType => (ExpressionType)_nodeType;

    /// <summary>Gets the payload classification for this node.</summary>
    public ExprNodeKind Kind => (ExprNodeKind)(FlagsAndKind & 0b1111);

    internal byte Flags => (byte)(FlagsAndKind >> 4);

    /// <summary>Gets the number of direct children linked from this node.</summary>
    public ushort ChildCount => (ushort)(_child >> ChildCountShift);

    /// <summary>Gets the first child idx or an auxiliary payload idx.</summary>
    public ushort FirstChildIdx => (ushort)(_child & FirstChildIdxMask);

    public void SetChildrenInfo(ushort childCount, ushort firstChildIdx) => _child = ((uint)childCount << ChildCountShift) | firstChildIdx;

    /// <summary>Gets the raw 32-bit value for inline primitive constants. Only valid when <see cref="Obj"/> == <see cref="InlineValueMarker"/>.</summary>
    internal uint InlineValue => _child;

    internal ExprNode(ExpressionType nodeType, Type type, object obj = null)
    {
        Type = type;
        Obj = obj;
        _nodeType = (byte)nodeType;
    }

    internal ExprNode(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind,
        ushort childIdx = 0, ushort childCount = 0, ushort nextIdx = 0)
    {
        Type = type;
        Obj = obj;
        _child = ((uint)childCount << ChildCountShift) | childIdx;
        NextIdx = nextIdx;
        _nodeType = (byte)nodeType;
        FlagsAndKind = (byte)((flags << 4) | ((byte)kind & 0b1111));
    }

    /// <summary>Constructs an inline primitive constant node, <see cref="Obj"/> is set to <see cref="InlineValueMarker"/>.</summary>
    internal ExprNode(Type type, uint inlineValue)
    {
        Type = type;
        Obj = InlineValueMarker;
        _nodeType = (byte)ExpressionType.Constant;
        _child = inlineValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Is(ExprNodeKind kind) => Kind == kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool RequiresInlineConstantStorage(Type type, object obj, ExpressionType nodeType) =>
        nodeType == ExpressionType.Constant && obj != null && !ReferenceEquals(obj, InlineValueMarker) &&
        (type.IsEnum
            ? IsSmallPrimitive(Type.GetTypeCode(Enum.GetUnderlyingType(type)))
            : type.IsPrimitive && IsSmallPrimitive(Type.GetTypeCode(type)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSmallPrimitive(TypeCode tc) =>
        tc == TypeCode.Boolean || tc == TypeCode.Byte || tc == TypeCode.SByte ||
        tc == TypeCode.Char || tc == TypeCode.Int16 || tc == TypeCode.UInt16 ||
        tc == TypeCode.Int32 || tc == TypeCode.UInt32 || tc == TypeCode.Single;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasFlag(byte flag) => (Flags & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasSameShapeExceptChildIdx(ref ExprNode other) =>
        Type == other.Type && NodeType == other.NodeType && FlagsAndKind == other.FlagsAndKind &&
        (_child & ChildCountMask) == (other._child & ChildCountMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasSameShape(ref ExprNode other) =>
        Type == other.Type && NodeType == other.NodeType && FlagsAndKind == other.FlagsAndKind &&
        _child == other._child;
}

/// <summary>Maps a lambda node to a captured outer parameter or variable.
/// Uses the same 16-bit storage range as the flat node links and identities.</summary>
[StructLayout(LayoutKind.Explicit, Size = 6)]
public struct LambdaClosureParameterUsage
{
    /// <summary>The lambda node idx in the flat-expression node array.</summary>
    [FieldOffset(0)]
    public ushort LambdaIdx;

    /// <summary>The parameter-usage expression node idx in the flat-expression node array.</summary>
    [FieldOffset(2)]
    public ushort ParameterIdx;

    /// <summary>The shared parameter/variable identity stored in <see cref="ExprNode.ChildIdx"/>.</summary>
    [FieldOffset(4)]
    public ushort ParameterId;

    /// <summary>Creates the lambda capture mapping.</summary>
    public LambdaClosureParameterUsage(ushort lambdaIdx, ushort parameterIdx, ushort parameterId)
    {
        LambdaIdx = lambdaIdx;
        ParameterIdx = parameterIdx;
        ParameterId = parameterId;
    }
}

/// <summary>Stores an expression tree as flat nodes plus separate closure constants.</summary>
public struct ExprTree : IEquatable<ExprTree>
{
    private static readonly object ClosureConstantMarker = new();
    private const byte ParameterByRefFlag = 1;
    private const byte BinaryLiftedToNullFlag = 1;
    private const byte LoopHasBreakFlag = 1;
    private const byte LoopHasContinueFlag = 2;
    private const byte CatchHasVariableFlag = 1;
    private const byte CatchHasFilterFlag = 2;
    private const byte TryFaultFlag = 1;

    /// <summary>Gets or sets the root node idx.</summary>
    public int RootIdx;

    /// <summary>Gets or sets the flat node storage.</summary>
    public SmallList<ExprNode, Stack16<ExprNode>, NoArrayPool<ExprNode>> Nodes;

    /// <summary>Gets or sets closure constants that are referenced from constant nodes.</summary>
    public SmallList<object, Stack16<object>, NoArrayPool<object>> ClosureConstants;

    /// <summary>Gets or sets all lambda node idxs added during construction.
    /// The root lambda idx is stored in <see cref="RootIdx"/>; other entries are nested lambdas.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> LambdaNodes;

    /// <summary>Gets or sets all block node idxs that carry explicit variable declarations.
    /// A tracked block uses <c>children.Count == 2</c>: one child list for variables and one for expressions.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> BlocksWithVariables;

    /// <summary>Gets or sets all <see cref="ExpressionType.Goto"/> node idxs,
    /// including <c>return</c>, <c>break</c>, and <c>continue</c>.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> GotoNodes;

    /// <summary>Gets or sets all <see cref="ExpressionType.Label"/> expression node idxs.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> LabelNodes;

    /// <summary>Gets or sets all <see cref="ExpressionType.Try"/> node idxs for try/catch, try/finally, try/fault, and combined forms.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> TryCatchNodes;

    /// <summary>Gets or sets captured outer parameter and variable usages for lambdas.
    /// The stored idxs use the same 16-bit range as <see cref="ExprNode.ChildIdx"/> and <see cref="ExprNode.NextIdx"/>.</summary>
    public SmallList<LambdaClosureParameterUsage, Stack16<LambdaClosureParameterUsage>, NoArrayPool<LambdaClosureParameterUsage>> LambdaClosureParameterUsages;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort AddNode(ExpressionType nodType, Type type, object obj = null, byte flags = 0, ExprNodeKind kind = default)
    {
        var node = new ExprNode(nodType, type, obj, flags, kind);
        return (ushort)Nodes.Add(in node);
    }

    /// <summary>Adds a parameter node and returns its idx.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Parameter(Type type, string name = null) =>
        AddNode(ExpressionType.Parameter, type, name, type.IsByRef ? ParameterByRefFlag : (byte)0);

    /// <summary>Adds a typed parameter node and returns its idx.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ParameterOf<T>(string name = null) => Parameter(typeof(T), name);

    /// <summary>Adds a variable node and returns its idx.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Variable(Type type, string name = null) => Parameter(type, name);

    /// <summary>Adds a default-value node and returns its idx.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Default(Type type) => AddNode(ExpressionType.Default, type);

    /// <summary>Adds a constant node with an explicit constant type.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Constant(object value, Type type) => AddNode(ExpressionType.Constant, type, value);

    /// <summary>Adds a constant node using the runtime type of the supplied value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Constant(object value) => Constant(value, value?.GetType() ?? typeof(object));

    /// <summary>Adds a null constant node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ConstantNull(Type type = null) => AddNode(ExpressionType.Constant, type ?? typeof(object));

    /// <summary>Adds an <see cref="int"/> constant node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ConstantInt(int value) => (ushort)Nodes.Add(new(typeof(int), unchecked((uint)value)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort New(ConstructorInfo ctor) => AddNode(ExpressionType.New, ctor.DeclaringType, ctor);

    /// <summary>Adds a parameterless <c>new</c> node for the specified type.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    public ushort New(Type type)
    {
        if (type.IsValueType)
            return AddNode(ExpressionType.New, type);

        foreach (var ctor in type.GetConstructors())
            if (ctor.GetParameters().Length == 0)
                return New(ctor);

        throw new ArgumentException($"The type {type} is missing the default constructor");
    }

    [UnscopedRef]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref ExprNode CloneIfHasNext(ref ushort idx)
    {
        ref var childRef = ref Nodes.GetSurePresentRef(idx);
        if (idx != 0 && childRef.NextIdx != 0)
            idx = (ushort)Nodes.AddCopy(childRef);
        return ref childRef;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort WithOneChild(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, ushort ch0)
    {
        var owner = new ExprNode(nodeType, type, obj, flags, kind);
        ref var child = ref CloneIfHasNext(ref ch0);
        owner.SetChildrenInfo(ch0 != 0 ? (ushort)1 : (ushort)0, ch0);
        return child.NextIdx = (ushort)Nodes.Add(in owner); // do not forget to set last arg.NextIdx to its parent index to navigate upwards
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort WithTwoChildren(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, ushort ch0, ushort ch1)
    {
        var owner = new ExprNode(nodeType, type, obj, flags, kind);
        ushort childCount = 0;
        ref var child = ref CloneIfHasNext(ref ch0);
        childCount += (ushort)(ch0 * 1);

        if (ch1 != 0)
        {
            ref var nextRef = ref CloneIfHasNext(ref ch1);
            child.NextIdx = ch1;
            child = ref nextRef;

            if (ch0 == 0) ch0 = ch1;
            ++childCount;
        }

        owner.SetChildrenInfo(childCount, ch0);
        return child.NextIdx = (ushort)Nodes.Add(in owner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort WithThreeChildren(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, ushort ch0, ushort ch1, ushort ch2)
    {
        var owner = new ExprNode(nodeType, type, obj, flags, kind);
        ushort childCount = 0;
        ref var child = ref CloneIfHasNext(ref ch0);
        childCount += (ushort)(ch0 * 1);

        if (ch1 != 0)
        {
            ref var nextRef = ref CloneIfHasNext(ref ch1);
            child.NextIdx = ch1;
            child = ref nextRef;

            if (ch0 == 0) ch0 = ch1;
            ++childCount;
        }

        if (ch2 != 0)
        {
            ref var nextRef = ref CloneIfHasNext(ref ch2);
            child.NextIdx = ch2;
            child = ref nextRef;

            if (ch0 == 0) ch0 = ch2;
            ++childCount;
        }

        owner.SetChildrenInfo(childCount, ch0);
        return child.NextIdx = (ushort)Nodes.Add(in owner);
    }

    private ushort WithTwoOrMoreChildren(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, ushort ch0, ushort ch1, ushort[] more)
    {
        var owner = new ExprNode(nodeType, type, obj, flags, kind);
        ushort childCount = 0;
        ref var childRef = ref CloneIfHasNext(ref ch0);
        childCount += (ushort)(ch0 * 1);

        if (ch1 != 0)
        {
            ref var nextRef = ref CloneIfHasNext(ref ch1);
            childRef.NextIdx = ch1;
            childRef = ref nextRef;

            if (ch0 == 0) ch0 = ch1;
            ++childCount;
        }

        ushort ch = 0;
        if (more != null)
        {
            for (ushort i = 0; i < more.Length; ++i)
            {
                ch = more.GetSurePresentRef(i);
                if (ch == 0) continue;
                ref var nextRef = ref CloneIfHasNext(ref ch);
                childRef.NextIdx = ch;
                childRef = ref nextRef;

                if (ch0 == 0) ch0 = ch;
                ++childCount;
            }
        }

        owner.SetChildrenInfo(childCount, ch0); // adding 0 and 0 is ubiquitous
        return childRef.NextIdx = (ushort)Nodes.Add(in owner);
    }

    private ushort WithOneOrMoreChildren(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, ushort ch0, ushort[] more)
    {
        var owner = new ExprNode(nodeType, type, obj, flags, kind);

        ushort childCount = 0;
        ref var childRef = ref CloneIfHasNext(ref ch0);
        childCount += (ushort)(ch0 * 1);

        ushort ch = 0;
        if (more != null)
        {
            for (ushort i = 0; i < more.Length; ++i)
            {
                ch = more.GetSurePresentRef(i);
                if (ch == 0) continue;
                ref var nextRef = ref CloneIfHasNext(ref ch);
                childRef.NextIdx = ch;
                childRef = ref nextRef;

                if (ch0 == 0) ch0 = ch;
                ++childCount;
            }
        }

        owner.SetChildrenInfo(childCount, ch0); // adding 0 and 0 is ubiquitous
        return childRef.NextIdx = (ushort)Nodes.Add(in owner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort WithChildren(ExpressionType nodeType, Type type, object obj, byte flags, ExprNodeKind kind, params ushort[] children) =>
        WithOneOrMoreChildren(nodeType, type, obj, flags, kind, 0, children);

    /// <summary>Adds a constructor call node.</summary>
    public ushort New(ConstructorInfo ctor, params ushort[] args) =>
        WithChildren(ExpressionType.New, ctor.DeclaringType, ctor, default, default, args);

    /// <summary>Adds an array initialization node.</summary>
    public ushort NewArrayInit(Type elementType, params ushort[] expressions) =>
        WithChildren(ExpressionType.NewArrayInit, elementType.MakeArrayType(), null, default, default, expressions);

    /// <summary>Adds an array-bounds node.</summary>
    public ushort NewArrayBounds(Type elementType, params ushort[] bounds) =>
        WithChildren(ExpressionType.NewArrayBounds, elementType.MakeArrayType(), null, default, default, bounds);

    /// <summary>Adds an invocation node.</summary>
    public ushort Invoke(ushort expr, params ushort[] args) =>
        WithOneOrMoreChildren(ExpressionType.Invoke, Nodes[expr].Type, null, default, default, expr, args);

    /// <summary>Adds a static-call node.</summary>
    public ushort Call(MethodInfo method, params ushort[] args) =>
        WithChildren(ExpressionType.Call, method.ReturnType, method, default, default, args);

    /// <summary>Adds an instance-call node.</summary>
    public ushort Call(ushort instance, MethodInfo method, params ushort[] args) =>
        WithOneOrMoreChildren(ExpressionType.Call, method.ReturnType, method, default, default, instance, args);

    /// <summary>Adds a field or property access node.</summary>
    public ushort MakeMemberAccess(MemberInfo member) =>
        AddNode(ExpressionType.MemberAccess, GetMemberType(member), member);

    public ushort MakeMemberAccess(ushort instance, MemberInfo member) =>
        WithOneChild(ExpressionType.MemberAccess, GetMemberType(member), member, default, default, instance);

    /// <summary>Adds a field-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Field(FieldInfo field) => MakeMemberAccess(field);

    /// <summary>Adds a field-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Field(ushort instance, FieldInfo field) => MakeMemberAccess(instance, field);

    /// <summary>Adds a static property-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Property(PropertyInfo prop) => MakeMemberAccess(prop);

    /// <summary>Adds a property-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Property(ushort instance, PropertyInfo prop) => MakeMemberAccess(instance, prop);

    /// <summary>Adds an indexed property-access node.</summary>
    public ushort Property(ushort instance, PropertyInfo prop, params ushort[] args) =>
        args == null || args.Length == 0
            ? Property(instance, prop)
            : WithOneOrMoreChildren(ExpressionType.Index, prop.PropertyType, prop, default, default, instance, args);

    /// <summary>Adds a binary node of the specified kind.</summary>
    public ushort MakeBinary(ExpressionType nodeType, ushort left, ushort right, bool isLiftedToNull = false,
        MethodInfo method = null, ushort conversion = 0, Type type = null)
        => WithThreeChildren(
            nodeType, type ?? GetBinaryResultType(nodeType, Nodes[left].Type, method), method, isLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, default,
            left, right, conversion);

    /// <summary>Adds a one-dimensional array index node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ArrayIndex(ushort array, ushort idx) => MakeBinary(ExpressionType.ArrayIndex, array, idx);

    /// <summary>Adds an array access node.</summary>
    public ushort ArrayAccess(ushort array, params ushort[] idxs) =>
        idxs != null && idxs.Length == 1
            ? ArrayIndex(array, idxs[0])
            : WithOneOrMoreChildren(ExpressionType.Index, GetArrayElementType(Nodes[array].Type, idxs?.Length ?? 0), null, default, default, array, idxs);

    /// <summary>Adds a conversion node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Convert(ushort operand, Type type, MethodInfo method = null) =>
        WithOneChild(ExpressionType.Convert, type, method, default, default, operand);

    /// <summary>Adds a type-as node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort TypeAs(ushort operand, Type type) =>
        WithOneChild(ExpressionType.TypeAs, type, null, default, default, operand);

    /// <summary>Adds a numeric negation node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Negate(ushort operand, MethodInfo method = null) =>
        MakeUnary(ExpressionType.Negate, operand, method: method);

    /// <summary>Adds a logical or bitwise not node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Not(ushort operand, MethodInfo method = null) =>
        MakeUnary(ExpressionType.Not, operand, method: method);

    /// <summary>Adds a unary node of the specified kind.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort MakeUnary(ExpressionType nodeType, ushort operand, Type type = null, MethodInfo method = null) =>
        WithOneChild(nodeType, type ?? GetUnaryResultType(nodeType, Nodes[operand].Type, method), method, default, default, operand);

    /// <summary>Adds an assignment node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Assign(ushort left, ushort right) => MakeBinary(ExpressionType.Assign, left, right);

    /// <summary>Adds an addition node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Add(ushort left, ushort right, MethodInfo method = null) => MakeBinary(ExpressionType.Add, left, right, method: method);

    /// <summary>Adds an equality node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Equal(ushort left, ushort right, MethodInfo method = null) => MakeBinary(ExpressionType.Equal, left, right, method: method);

    /// <summary>Adds a conditional node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Condition(ushort test, ushort ifTrue, ushort ifFalse, Type type = null) =>
        WithThreeChildren(ExpressionType.Conditional, type ?? Nodes[ifTrue].Type, null, default, default, test, ifTrue, ifFalse);

    /// <summary>BlockNode.ChildIdx -> ExprsNode; ExprsNode.NextIdx -> VarsNode|BlockNode</summary>
    public ushort Block(Type type, ushort[] vars, params ushort[] exprs)
    {
        if (exprs == null || exprs.Length == 0)
            throw new ArgumentException("Block should contain at least one expression.", nameof(exprs));

        var exprsSubNode = WithChildren(ExpressionType.Block, null, null, default, ExprNodeKind.BlockExprs, exprs);

        type ??= Nodes[exprs[^1]].Type;
        return WithOneOrMoreChildren(ExpressionType.Block, type, null, default, default, exprsSubNode, vars);
    }

    /// <summary>Adds a block node without explicit variables.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Block(params ushort[] exprs) =>
        Block(null, null, exprs);

    /// <summary>Adds a lambda node.</summary>
    public ushort Lambda(Type delegateType, ushort bodyIdx, params ushort[] pars) =>
        WithOneOrMoreChildren(ExpressionType.Lambda, delegateType, null, default, default, bodyIdx, pars);

    /// <summary>Adds a typed lambda node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Lambda<TDelegate>(ushort bodyIdx, params ushort[] parameters) where TDelegate : Delegate =>
        Lambda(typeof(TDelegate), bodyIdx, parameters);

    /// <summary>Adds a member-assignment binding node.</summary>
    public ushort Bind(MemberInfo member, ushort expr) =>
        WithOneChild(default, GetMemberType(member), member, default, ExprNodeKind.MemberAssignment, expr);

    /// <summary>Adds a nested member-binding node.</summary>
    public ushort MemberBind(MemberInfo member, params ushort[] bindings) =>
        WithChildren(default, GetMemberType(member), member, default, ExprNodeKind.MemberMemberBinding, bindings);

    /// <summary>Adds an element-initializer node.</summary>
    public ushort ElementInit(MethodInfo addMethod, params ushort[] args) =>
        WithChildren(default, addMethod.DeclaringType, addMethod, default, ExprNodeKind.ElementInit, args);

    /// <summary>Adds a list-binding node.</summary>
    public ushort ListBind(MemberInfo member, params ushort[] initializers) =>
        WithChildren(default, GetMemberType(member), member, default, ExprNodeKind.MemberListBinding, initializers);

    /// <summary>Adds a member-init node.</summary>
    public ushort MemberInit(ushort expr, params ushort[] bindings) =>
        WithOneOrMoreChildren(ExpressionType.MemberInit, Nodes[expr].Type, null, default, default, expr, bindings);

    /// <summary>Adds a list-init node.</summary>
    public ushort ListInit(ushort @new, params ushort[] initializers) =>
        WithOneOrMoreChildren(ExpressionType.ListInit, Nodes[@new].Type, null, default, default, @new, initializers);

    /// <summary>Adds a label-target node.</summary>
    public ushort Label(Type type = null, string name = null) =>
        WithOneChild(default, type ?? typeof(void), name, default, ExprNodeKind.LabelTarget, 0);

    /// <summary>Adds a label-expression node.</summary>
    public ushort Label(ushort target, ushort defaultValue = 0) =>
        WithOneChild(ExpressionType.Label, Nodes[target].Type, null, default, default, target);

    /// <summary>Adds a goto-family node.</summary>
    public ushort MakeGoto(GotoExpressionKind gotoKind, ushort target, ushort value = 0, Type type = null)
    {
        var resultType = type ?? (value != 0 ? Nodes[value].Type : typeof(void));
        return WithTwoChildren(ExpressionType.Goto, resultType, null, (byte)gotoKind, default, target, value);
    }

    /// <summary>Adds a goto node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Goto(ushort target, ushort value = 0, Type type = null) => MakeGoto(GotoExpressionKind.Goto, target, value, type);

    /// <summary>Adds a return node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort Return(ushort target, ushort value) => MakeGoto(GotoExpressionKind.Return, target, value, Nodes[value].Type);

    /// <summary>Adds a loop node.</summary>
    public ushort Loop(ushort body, ushort @break = 0, ushort @continue = 0) =>
        WithThreeChildren(ExpressionType.Loop, typeof(void), null, default, default, body, @break, @continue);

    /// <summary>Adds a switch-case node.</summary>
    public ushort SwitchCase(ushort body, params ushort[] testValues) =>
        WithOneOrMoreChildren(ExpressionType.Switch, null, null, default, ExprNodeKind.SwitchCase, body, testValues);

    /// <summary>Adds a switch node.</summary>
    public ushort Switch(Type type, ushort switchValue, ushort defaultBody, MethodInfo comparison, params ushort[] cases)
    {
        var casesIdx = cases == null || cases.Length == 0
            ? (ushort)0
            : WithChildren(ExpressionType.Switch, type, null, default, ExprNodeKind.SwitchCases, cases);
        return WithThreeChildren(ExpressionType.Switch, type, comparison, default, default, casesIdx, switchValue, defaultBody);
    }

    /// <summary>Adds a switch node without an explicit default case or comparer.</summary>
    public ushort Switch(ushort switchValue, params ushort[] cases) =>
        WithChildren(ExpressionType.Switch, Nodes[switchValue].Type, null, default, default, cases);

    /// <summary>Adds a catch block with an exception variable.</summary>
    public ushort Catch(ushort variable, ushort body) =>
        WithTwoChildren(default, Nodes[variable].Type, null, default, ExprNodeKind.CatchBlock, body, variable);

    /// <summary>Adds a catch block without an exception variable.</summary>
    public ushort Catch(Type test, ushort body) =>
        WithOneChild(default, test, null, default, ExprNodeKind.CatchBlock, body);

    /// <summary>Adds a catch block with optional exception variable and filter.</summary>
    public ushort MakeCatchBlock(Type test, ushort variable, ushort body, ushort filter = 0) =>
        WithThreeChildren(default, test, null, default, ExprNodeKind.CatchBlock, body, variable, filter);

    /// <summary>Adds a try/catch node.</summary>
    public ushort TryCatch(ushort body, params ushort[] handlers) =>
        WithOneOrMoreChildren(ExpressionType.Try, Nodes[body].Type, null, default, default, body, handlers);

    /// <summary>Adds a try/finally node.</summary>
    public ushort TryFinally(ushort body, ushort @finally) =>
        WithTwoChildren(ExpressionType.Try, Nodes[body].Type, null, default, default, body, @finally);

    /// <summary>Adds a try/fault node.</summary>
    public ushort TryFault(ushort body, ushort fault) =>
        WithTwoChildren(ExpressionType.Try, Nodes[body].Type, null, TryFaultFlag, default, body, fault);

    /// <summary>Adds a try node with optional finally block and catch handlers.</summary>
    public ushort TryCatchFinally(ushort body, ushort @finally, params ushort[] handlers) =>
        WithTwoOrMoreChildren(ExpressionType.Try, Nodes[body].Type, null, default, default, body, @finally, handlers);

    /// <summary>Adds a type-test node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort TypeIs(ushort expr, Type type) =>
        WithOneChild(ExpressionType.TypeIs, typeof(bool), null, default, default, expr);

    /// <summary>Adds a type-equality test node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort TypeEqual(ushort expr, Type type) =>
        WithOneChild(ExpressionType.TypeEqual, typeof(bool), type, default, default, expr);

    /// <summary>Adds a dynamic-expression node.</summary>
    public ushort Dynamic(Type delegateType, CallSiteBinder binder, params ushort[] args) =>
        WithChildren(ExpressionType.Dynamic, typeof(object), binder, default, default, args);

    /// <summary>Adds a runtime-variables node.</summary>
    public ushort RuntimeVariables(params ushort[] vars) =>
        WithChildren(ExpressionType.RuntimeVariables, typeof(IRuntimeVariables), null, default, default, vars);

    /// <summary>Adds a debug-info node.</summary>
    public ushort DebugInfo(string fileName, ushort startLine, ushort startColumn, ushort endLine, ushort endColumn) =>
        WithChildren(ExpressionType.DebugInfo, typeof(void), fileName, default, default, startLine, startColumn, endLine, endColumn);

    private int AddSwitchCase(SysSwitchCase switchCase)
    {
        ChildIdxs children = default;
        for (var i = 0; i < switchCase.TestValues.Count; ++i)
            children.Add(AddExpression(switchCase.TestValues[i]));
        children.Add(AddExpression(switchCase.Body));
        return _tree.AddRawAuxNode(switchCase.Body.Type, null, ExprNodeKind.SwitchCase, children);
    }

    private int AddCatchBlock(SysCatchBlock catchBlock)
    {
        ChildIdxs children = default;
        if (catchBlock.Variable != null)
            children.Add(AddExpression(catchBlock.Variable));
        children.Add(AddExpression(catchBlock.Body));
        if (catchBlock.Filter != null)
            children.Add(AddExpression(catchBlock.Filter));
        return _tree.AddNode(catchBlock.Test, null, ExpressionType.Extension, ExprNodeKind.CatchBlock,
            (byte)((catchBlock.Variable != null ? CatchHasVariableFlag : 0) | (catchBlock.Filter != null ? CatchHasFilterFlag : 0)), in children);
    }

    private int AddLabelTarget(SysLabelTarget target) =>
        _tree.AddRawLeafAuxNode(target.Type, target.Name, ExprNodeKind.LabelTarget, childIdx: GetId(ref _labelIds, target));

    private int AddMemberBinding(SysMemberBinding binding)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                ChildIdxs assignmentChildren = default;
                assignmentChildren.Add(AddExpression(((MemberAssignment)binding).Expression));
                return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberAssignment,
                    assignmentChildren);
            case MemberBindingType.MemberBinding:
                {
                    var memberBinding = (MemberMemberBinding)binding;
                    ChildIdxs children = default;
                    for (var i = 0; i < memberBinding.Bindings.Count; ++i)
                        children.Add(AddMemberBinding(memberBinding.Bindings[i]));
                    return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberMemberBinding, children);
                }
            case MemberBindingType.ListBinding:
                {
                    var listBinding = (MemberListBinding)binding;
                    ChildIdxs children = default;
                    for (var i = 0; i < listBinding.Initializers.Count; ++i)
                        children.Add(AddElementInit(listBinding.Initializers[i]));
                    return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberListBinding, children);
                }
            default:
                throw new NotSupportedException($"Flattening of member binding `{binding.BindingType}` is not supported yet.");
        }
    }

    private int AddElementInit(SysElementInit init)
    {
        ChildIdxs children = default;
        for (var i = 0; i < init.Arguments.Count; ++i)
            children.Add(AddExpression(init.Arguments[i]));
        return _tree.AddRawAuxNode(init.AddMethod.DeclaringType, init.AddMethod, ExprNodeKind.ElementInit, children);
    }

    private static int GetId(ref SmallMap16<object, int, RefEq<object>> ids, object item)
    {
        ref var id = ref ids.Map.AddOrGetValueRef(item, out var found);
        if (!found)
            id = ids.Map.Count;
        return id;
    }

    public ushort FromSysExpr(SysExpr expr)
    {
        switch (expr.NodeType)
        {
            case ExpressionType.Constant:
                return Constant(((ConstantExpression)expr).Value, expr.Type);

            case ExpressionType.Default:
                return Default(expr.Type);

            case ExpressionType.Parameter:
                {
                    var parameter = (SysParameterExpression)expr;
                    return _tree.AddLeafNode(expr.Type, parameter.Name, expr.NodeType,
                        flags: parameter.IsByRef ? ParameterByRefFlag : (byte)0, childIdx: GetId(ref _parameterIds, parameter));
                }
            case ExpressionType.Lambda:
                {
                    // Layout: children[0] = body, children[1..n] = parameter decl nodes.
                    // Body is stored before parameters so that the Reader encounters parameter
                    // refs in the body before their decl nodes (out-of-order decl); identity
                    // is preserved via the shared _parametersById id-map.
                    var lambda = (LambdaExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(lambda.Body));
                    for (var i = 0; i < lambda.Parameters.Count; ++i)
                        children.Add(AddExpression(lambda.Parameters[i]));
                    var lambdaIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, in children);
                    _tree.LambdaNodes.Add(lambdaIdx);
                    _tree.CollectLambdaClosureParameterUsages(lambdaIdx);
                    return lambdaIdx;
                }
            case ExpressionType.Block:
                {
                    // With variables: children[0] is the variable list and children[1] is the expression list.
                    // Without variables: children[0] is the expression list.
                    // children.Count == 2 means the block has explicit variables.
                    var block = (BlockExpression)expr;
                    ChildIdxs children = default;
                    var hasVariables = block.Variables.Count != 0;
                    if (hasVariables)
                    {
                        ChildIdxs variables = default;
                        for (var i = 0; i < block.Variables.Count; ++i)
                            variables.Add(AddExpression(block.Variables[i]));
                        children.Add(_tree.AddChildListNode(in variables));
                    }
                    ChildIdxs expressions = default;
                    for (var i = 0; i < block.Expressions.Count; ++i)
                        expressions.Add(AddExpression(block.Expressions[i]));
                    children.Add(_tree.AddChildListNode(in expressions));
                    var blockIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, in children);
                    if (hasVariables)
                        _tree.BlocksWithVariables.Add(blockIdx);
                    return blockIdx;
                }
            case ExpressionType.MemberAccess:
                {
                    var member = (MemberExpression)expr;
                    ChildIdxs children = default;
                    if (member.Expression != null)
                        children.Add(AddExpression(member.Expression));
                    return _tree.AddRawExpressionNode(expr.Type, member.Member, expr.NodeType,
                        children);
                }
            case ExpressionType.Call:
                {
                    var call = (MethodCallExpression)expr;
                    ChildIdxs children = default;
                    if (call.Object != null)
                        children.Add(AddExpression(call.Object));
                    for (var i = 0; i < call.Arguments.Count; ++i)
                        children.Add(AddExpression(call.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, call.Method, expr.NodeType, children);
                }
            case ExpressionType.New:
                {
                    var @new = (NewExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < @new.Arguments.Count; ++i)
                        children.Add(AddExpression(@new.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, @new.Constructor, expr.NodeType, children);
                }
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                {
                    var array = (NewArrayExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < array.Expressions.Count; ++i)
                        children.Add(AddExpression(array.Expressions[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.Invoke:
                {
                    var invoke = (InvocationExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(invoke.Expression));
                    for (var i = 0; i < invoke.Arguments.Count; ++i)
                        children.Add(AddExpression(invoke.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.Index:
                {
                    var indexExpr = (IndexExpression)expr;
                    ChildIdxs children = default;
                    if (indexExpr.Object != null)
                        children.Add(AddExpression(indexExpr.Object));
                    for (var i = 0; i < indexExpr.Arguments.Count; ++i)
                        children.Add(AddExpression(indexExpr.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, indexExpr.Indexer, expr.NodeType, children);
                }
            case ExpressionType.Conditional:
                {
                    var conditional = (ConditionalExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(conditional.Test));
                    children.Add(AddExpression(conditional.IfTrue));
                    children.Add(AddExpression(conditional.IfFalse));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children[0], children[1], children[2]);
                }
            case ExpressionType.Loop:
                {
                    var loop = (LoopExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(loop.Body));
                    if (loop.BreakLabel != null)
                        children.Add(AddLabelTarget(loop.BreakLabel));
                    if (loop.ContinueLabel != null)
                        children.Add(AddLabelTarget(loop.ContinueLabel));
                    return _tree.AddNode(expr.Type, null, expr.NodeType, ExprNodeKind.Expression,
                        (byte)((loop.BreakLabel != null ? LoopHasBreakFlag : 0) | (loop.ContinueLabel != null ? LoopHasContinueFlag : 0)), in children);
                }
            case ExpressionType.Goto:
                {
                    var @goto = (GotoExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddLabelTarget(@goto.Target));
                    if (@goto.Value != null)
                        children.Add(AddExpression(@goto.Value));
                    var gotoIdx = _tree.AddRawExpressionNode(expr.Type, @goto.Kind, expr.NodeType, children);
                    _tree.GotoNodes.Add(gotoIdx);
                    return gotoIdx;
                }
            case ExpressionType.Label:
                {
                    var label = (LabelExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddLabelTarget(label.Target));
                    if (label.DefaultValue != null)
                        children.Add(AddExpression(label.DefaultValue));
                    var labelIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                    _tree.LabelNodes.Add(labelIdx);
                    return labelIdx;
                }
            case ExpressionType.Switch:
                {
                    var @switch = (SwitchExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(@switch.SwitchValue));
                    if (@switch.DefaultBody != null)
                        children.Add(AddExpression(@switch.DefaultBody));
                    if (@switch.Cases.Count != 0)
                    {
                        ChildIdxs cases = default;
                        for (var i = 0; i < @switch.Cases.Count; ++i)
                            cases.Add(AddSwitchCase(@switch.Cases[i]));
                        children.Add(_tree.AddChildListNode(in cases));
                    }
                    return _tree.AddRawExpressionNode(expr.Type, @switch.Comparison, expr.NodeType, in children);
                }
            case ExpressionType.Try:
                {
                    var @try = (TryExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(@try.Body));
                    var flags = (byte)0;
                    if (@try.Fault != null)
                    {
                        flags = TryFaultFlag;
                        children.Add(AddExpression(@try.Fault));
                    }
                    else if (@try.Finally != null)
                        children.Add(AddExpression(@try.Finally));
                    if (@try.Handlers.Count != 0)
                    {
                        ChildIdxs handlers = default;
                        for (var i = 0; i < @try.Handlers.Count; ++i)
                            handlers.Add(AddCatchBlock(@try.Handlers[i]));
                        children.Add(_tree.AddChildListNode(in handlers));
                    }
                    var tryIdx = _tree.AddNode(expr.Type, null, expr.NodeType, ExprNodeKind.Expression, flags, in children);
                    _tree.TryCatchNodes.Add(tryIdx);
                    return tryIdx;
                }
            case ExpressionType.MemberInit:
                {
                    var memberInit = (MemberInitExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(memberInit.NewExpression));
                    for (var i = 0; i < memberInit.Bindings.Count; ++i)
                        children.Add(AddMemberBinding(memberInit.Bindings[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.ListInit:
                {
                    var listInit = (ListInitExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(listInit.NewExpression));
                    for (var i = 0; i < listInit.Initializers.Count; ++i)
                        children.Add(AddElementInit(listInit.Initializers[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.TypeIs:
            case ExpressionType.TypeEqual:
                {
                    var typeBinary = (TypeBinaryExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(typeBinary.Expression));
                    return _tree.AddRawExpressionNode(expr.Type, typeBinary.TypeOperand, expr.NodeType,
                        children);
                }
            case ExpressionType.Dynamic:
                {
                    var dynamic = (DynamicExpression)expr;
                    ChildIdxs children = default;
                    children.Add(_tree.AddObjectReferenceNode(typeof(Type), dynamic.DelegateType));
                    for (var i = 0; i < dynamic.Arguments.Count; ++i)
                        children.Add(AddExpression(dynamic.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, dynamic.Binder, expr.NodeType, children);
                }
            case ExpressionType.RuntimeVariables:
                {
                    var runtime = (RuntimeVariablesExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < runtime.Variables.Count; ++i)
                        children.Add(AddExpression(runtime.Variables[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.DebugInfo:
                {
                    var debug = (DebugInfoExpression)expr;
                    return _tree.AddFactoryExpressionNode(expr.Type, debug.Document.FileName, expr.NodeType,
                        _tree.CreateDebugInfoChildren(debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn));
                }
            default:
                if (expr is UnaryExpression unary)
                {
                    ChildIdxs children = default;
                    children.Add(AddExpression(unary.Operand));
                    return _tree.AddRawExpressionNode(expr.Type, unary.Method, expr.NodeType,
                        children);
                }

                if (expr is BinaryExpression binary)
                {
                    ChildIdxs children = default;
                    children.Add(AddExpression(binary.Left));
                    children.Add(AddExpression(binary.Right));
                    if (binary.Conversion != null)
                        children.Add(AddExpression(binary.Conversion));
                    return _tree.AddNode(expr.Type, binary.Method, expr.NodeType, ExprNodeKind.Expression,
                        binary.IsLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, in children);
                }

                throw new NotSupportedException($"Flattening of `ExpressionType.{expr.NodeType}` is not supported yet.");
        }
    }

    public ushort FromLightExpr(LightExpression.Expression expr)
    {
        switch (expr.NodeType)
        {
            case ExpressionType.Constant:
                return Constant(((LightExpression.ConstantExpression)expr).Value, expr.Type);

            case ExpressionType.Default:
                return _tree.AddLeafNode(expr.Type, null, expr.NodeType);

            case ExpressionType.Parameter:
                {
                    var parameter = (SysParameterExpression)expr;
                    return _tree.AddLeafNode(expr.Type, parameter.Name, expr.NodeType,
                        flags: parameter.IsByRef ? ParameterByRefFlag : (byte)0, childIdx: GetId(ref _parameterIds, parameter));
                }
            case ExpressionType.Lambda:
                {
                    // Layout: children[0] = body, children[1..n] = parameter decl nodes.
                    // Body is stored before parameters so that the Reader encounters parameter
                    // refs in the body before their decl nodes (out-of-order decl); identity
                    // is preserved via the shared _parametersById id-map.
                    var lambda = (LambdaExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(lambda.Body));
                    for (var i = 0; i < lambda.Parameters.Count; ++i)
                        children.Add(AddExpression(lambda.Parameters[i]));
                    var lambdaIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, in children);
                    _tree.LambdaNodes.Add(lambdaIdx);
                    _tree.CollectLambdaClosureParameterUsages(lambdaIdx);
                    return lambdaIdx;
                }
            case ExpressionType.Block:
                {
                    // With variables: children[0] is the variable list and children[1] is the expression list.
                    // Without variables: children[0] is the expression list.
                    // children.Count == 2 means the block has explicit variables.
                    var block = (BlockExpression)expr;
                    ChildIdxs children = default;
                    var hasVariables = block.Variables.Count != 0;
                    if (hasVariables)
                    {
                        ChildIdxs variables = default;
                        for (var i = 0; i < block.Variables.Count; ++i)
                            variables.Add(AddExpression(block.Variables[i]));
                        children.Add(_tree.AddChildListNode(in variables));
                    }
                    ChildIdxs expressions = default;
                    for (var i = 0; i < block.Expressions.Count; ++i)
                        expressions.Add(AddExpression(block.Expressions[i]));
                    children.Add(_tree.AddChildListNode(in expressions));
                    var blockIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, in children);
                    if (hasVariables)
                        _tree.BlocksWithVariables.Add(blockIdx);
                    return blockIdx;
                }
            case ExpressionType.MemberAccess:
                {
                    var member = (MemberExpression)expr;
                    ChildIdxs children = default;
                    if (member.Expression != null)
                        children.Add(AddExpression(member.Expression));
                    return _tree.AddRawExpressionNode(expr.Type, member.Member, expr.NodeType,
                        children);
                }
            case ExpressionType.Call:
                {
                    var call = (MethodCallExpression)expr;
                    ChildIdxs children = default;
                    if (call.Object != null)
                        children.Add(AddExpression(call.Object));
                    for (var i = 0; i < call.Arguments.Count; ++i)
                        children.Add(AddExpression(call.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, call.Method, expr.NodeType, children);
                }
            case ExpressionType.New:
                {
                    var @new = (NewExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < @new.Arguments.Count; ++i)
                        children.Add(AddExpression(@new.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, @new.Constructor, expr.NodeType, children);
                }
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                {
                    var array = (NewArrayExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < array.Expressions.Count; ++i)
                        children.Add(AddExpression(array.Expressions[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.Invoke:
                {
                    var invoke = (InvocationExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(invoke.Expression));
                    for (var i = 0; i < invoke.Arguments.Count; ++i)
                        children.Add(AddExpression(invoke.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.Index:
                {
                    var indexExpr = (IndexExpression)expr;
                    ChildIdxs children = default;
                    if (indexExpr.Object != null)
                        children.Add(AddExpression(indexExpr.Object));
                    for (var i = 0; i < indexExpr.Arguments.Count; ++i)
                        children.Add(AddExpression(indexExpr.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, indexExpr.Indexer, expr.NodeType, children);
                }
            case ExpressionType.Conditional:
                {
                    var conditional = (ConditionalExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(conditional.Test));
                    children.Add(AddExpression(conditional.IfTrue));
                    children.Add(AddExpression(conditional.IfFalse));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children[0], children[1], children[2]);
                }
            case ExpressionType.Loop:
                {
                    var loop = (LoopExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(loop.Body));
                    if (loop.BreakLabel != null)
                        children.Add(AddLabelTarget(loop.BreakLabel));
                    if (loop.ContinueLabel != null)
                        children.Add(AddLabelTarget(loop.ContinueLabel));
                    return _tree.AddNode(expr.Type, null, expr.NodeType, ExprNodeKind.Expression,
                        (byte)((loop.BreakLabel != null ? LoopHasBreakFlag : 0) | (loop.ContinueLabel != null ? LoopHasContinueFlag : 0)), in children);
                }
            case ExpressionType.Goto:
                {
                    var @goto = (GotoExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddLabelTarget(@goto.Target));
                    if (@goto.Value != null)
                        children.Add(AddExpression(@goto.Value));
                    var gotoIdx = _tree.AddRawExpressionNode(expr.Type, @goto.Kind, expr.NodeType, children);
                    _tree.GotoNodes.Add(gotoIdx);
                    return gotoIdx;
                }
            case ExpressionType.Label:
                {
                    var label = (LabelExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddLabelTarget(label.Target));
                    if (label.DefaultValue != null)
                        children.Add(AddExpression(label.DefaultValue));
                    var labelIdx = _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                    _tree.LabelNodes.Add(labelIdx);
                    return labelIdx;
                }
            case ExpressionType.Switch:
                {
                    var @switch = (SwitchExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(@switch.SwitchValue));
                    if (@switch.DefaultBody != null)
                        children.Add(AddExpression(@switch.DefaultBody));
                    if (@switch.Cases.Count != 0)
                    {
                        ChildIdxs cases = default;
                        for (var i = 0; i < @switch.Cases.Count; ++i)
                            cases.Add(AddSwitchCase(@switch.Cases[i]));
                        children.Add(_tree.AddChildListNode(in cases));
                    }
                    return _tree.AddRawExpressionNode(expr.Type, @switch.Comparison, expr.NodeType, in children);
                }
            case ExpressionType.Try:
                {
                    var @try = (TryExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(@try.Body));
                    var flags = (byte)0;
                    if (@try.Fault != null)
                    {
                        flags = TryFaultFlag;
                        children.Add(AddExpression(@try.Fault));
                    }
                    else if (@try.Finally != null)
                        children.Add(AddExpression(@try.Finally));
                    if (@try.Handlers.Count != 0)
                    {
                        ChildIdxs handlers = default;
                        for (var i = 0; i < @try.Handlers.Count; ++i)
                            handlers.Add(AddCatchBlock(@try.Handlers[i]));
                        children.Add(_tree.AddChildListNode(in handlers));
                    }
                    var tryIdx = _tree.AddNode(expr.Type, null, expr.NodeType, ExprNodeKind.Expression, flags, in children);
                    _tree.TryCatchNodes.Add(tryIdx);
                    return tryIdx;
                }
            case ExpressionType.MemberInit:
                {
                    var memberInit = (MemberInitExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(memberInit.NewExpression));
                    for (var i = 0; i < memberInit.Bindings.Count; ++i)
                        children.Add(AddMemberBinding(memberInit.Bindings[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.ListInit:
                {
                    var listInit = (ListInitExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(listInit.NewExpression));
                    for (var i = 0; i < listInit.Initializers.Count; ++i)
                        children.Add(AddElementInit(listInit.Initializers[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.TypeIs:
            case ExpressionType.TypeEqual:
                {
                    var typeBinary = (TypeBinaryExpression)expr;
                    ChildIdxs children = default;
                    children.Add(AddExpression(typeBinary.Expression));
                    return _tree.AddRawExpressionNode(expr.Type, typeBinary.TypeOperand, expr.NodeType,
                        children);
                }
            case ExpressionType.Dynamic:
                {
                    var dynamic = (DynamicExpression)expr;
                    ChildIdxs children = default;
                    children.Add(_tree.AddObjectReferenceNode(typeof(Type), dynamic.DelegateType));
                    for (var i = 0; i < dynamic.Arguments.Count; ++i)
                        children.Add(AddExpression(dynamic.Arguments[i]));
                    return _tree.AddRawExpressionNode(expr.Type, dynamic.Binder, expr.NodeType, children);
                }
            case ExpressionType.RuntimeVariables:
                {
                    var runtime = (RuntimeVariablesExpression)expr;
                    ChildIdxs children = default;
                    for (var i = 0; i < runtime.Variables.Count; ++i)
                        children.Add(AddExpression(runtime.Variables[i]));
                    return _tree.AddRawExpressionNode(expr.Type, null, expr.NodeType, children);
                }
            case ExpressionType.DebugInfo:
                {
                    var debug = (DebugInfoExpression)expr;
                    return _tree.AddFactoryExpressionNode(expr.Type, debug.Document.FileName, expr.NodeType,
                        _tree.CreateDebugInfoChildren(debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn));
                }
            default:
                if (expr is UnaryExpression unary)
                {
                    ChildIdxs children = default;
                    children.Add(AddExpression(unary.Operand));
                    return _tree.AddRawExpressionNode(expr.Type, unary.Method, expr.NodeType,
                        children);
                }

                if (expr is BinaryExpression binary)
                {
                    ChildIdxs children = default;
                    children.Add(AddExpression(binary.Left));
                    children.Add(AddExpression(binary.Right));
                    if (binary.Conversion != null)
                        children.Add(AddExpression(binary.Conversion));
                    return _tree.AddNode(expr.Type, binary.Method, expr.NodeType, ExprNodeKind.Expression,
                        binary.IsLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, in children);
                }

                throw new NotSupportedException($"Flattening of `ExpressionType.{expr.NodeType}` is not supported yet.");
        }
    }

    /// <summary>Reconstructs the flat tree as a System.Linq expression tree.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077",
        Justification = "Flat expression round-trip stores the runtime type metadata explicitly for reconstruction.")]
    public SysExpr ToExpression() =>
        Nodes.Count != 0
            ? new SysExprBuilder(this).ReadExpression(RootIdx)
            : throw new InvalidOperationException("Flat expression tree is empty.");

    /// <summary>Reconstructs the flat tree as a LightExpression tree.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    public LightExpression ToLightExpression() => FastExpressionCompiler.LightExpression.FromSysExpressionConverter.ToLightExpression(ToExpression());

    /// <summary>Structurally compares two flat expression trees.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ExprTree other) =>
        new StructuralComparer().Eq(ref this, ref other);

    /// <summary>Structurally compares this tree with another object.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object obj) =>
        obj is ExprTree other && Equals(other);

    /// <summary>Computes a content-addressable hash for the flat expression tree.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() =>
        new StructuralComparer().Hash(ref this);

    /// <summary>Determines whether two flat expression trees are structurally equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ExprTree left, ExprTree right) => left.Equals(right);

    /// <summary>Determines whether two flat expression trees are not structurally equal.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ExprTree left, ExprTree right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool In32BitRange(TypeCode tc) =>
        tc == TypeCode.Boolean || tc == TypeCode.Byte || tc == TypeCode.SByte ||
        tc == TypeCode.Char || tc == TypeCode.Int16 || tc == TypeCode.UInt16 ||
        tc == TypeCode.Int32 || tc == TypeCode.UInt32 || tc == TypeCode.Single;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ToInlineValue(object value, TypeCode tc) => tc switch
    {
        TypeCode.Boolean => (bool)value ? 1u : 0u,
        TypeCode.Byte => (byte)value,
        TypeCode.SByte => (uint)(byte)(sbyte)value,
        TypeCode.Char => (char)value,
        TypeCode.Int16 => (uint)(ushort)(short)value,
        TypeCode.UInt16 => (ushort)value,
        TypeCode.Int32 => (uint)(int)value,
        TypeCode.UInt32 => (uint)value,
        TypeCode.Single => FloatBits.ToUInt((float)value),
        _ => FlatExpressionThrow.UnsupportedInlineConstantType<uint>(value, tc)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type GetMemberType(MemberInfo member) => member switch
    {
        FieldInfo field => field.FieldType,
        PropertyInfo property => property.PropertyType,
        _ => typeof(object)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type GetUnaryResultType(ExpressionType nodeType, Type operandType, MethodInfo method) =>
        nodeType switch
        {
            ExpressionType.IsFalse or ExpressionType.IsTrue or ExpressionType.TypeIs or ExpressionType.TypeEqual => typeof(bool),
            _ => method?.ReturnType ?? operandType
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type GetBinaryResultType(ExpressionType nodeType, Type leftType, MethodInfo method) =>
        method != null ? method.ReturnType : nodeType switch
        {
            ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
                or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.AndAlso or ExpressionType.OrElse => typeof(bool),
            ExpressionType.ArrayIndex => leftType.GetElementType(),
            ExpressionType.Assign => leftType,
            _ => leftType
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type GetArrayElementType(Type arrayType, int depth)
    {
        var elementType = arrayType;
        for (var i = 0; i < depth; ++i)
            elementType = elementType.GetElementType();
        return elementType ?? typeof(object);
    }

    private static bool Contains<TStack, TPool>(ref SmallList<ushort, TStack, TPool> ids, ushort value)
        where TStack : struct, IStack<ushort, TStack>
        where TPool : struct, ISmallArrayPool<ushort>
    {
        for (var i = 0; i < ids.Count; ++i)
            if (ids[i] == value)
                return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ToStoredUShortIdx(int idx) => checked((ushort)idx);

    private struct StructuralComparer
    {
        private ChildIdxs _xParameterIds, _yParameterIds;
        private SmallList<ushort, Stack8<ushort>, NoArrayPool<ushort>> _xLabelIds, _yLabelIds;
        private SmallList<TraversalFrame, Stack16<TraversalFrame>, NoArrayPool<TraversalFrame>> _eqFrames;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Eq(ref ExprTree xTree, ref ExprTree yTree)
        {
            if (xTree.Nodes.Count == 0 || yTree.Nodes.Count == 0)
                return xTree.Nodes.Count == yTree.Nodes.Count;

            var xIdx = xTree.RootIdx;
            var yIdx = yTree.RootIdx;
            var remainingSiblings = 0;
            while (true)
            {
                ref var x = ref xTree.Nodes.GetSurePresentRef(xIdx);
                ref var y = ref yTree.Nodes.GetSurePresentRef(yIdx);
                if (x.Kind == ExprNodeKind.UInt16Pair)
                {
                    if (!x.HasSameShape(ref y))
                        return false;
                }
                else if (x.NodeType == ExpressionType.Constant)
                {

                    if (x.Type != y.Type || x.NodeType != y.NodeType || x.FlagsAndKind != y.FlagsAndKind)
                        return false;
                }
                else if (!x.HasSameShapeExceptChildIdx(ref y))
                    return false;

                var descendX = 0;
                var descendY = 0;
                var descendChildCount = 0;
                var restoreXParameterCount = -1;
                var restoreYParameterCount = -1;

                if (x.Kind != ExprNodeKind.UInt16Pair)
                {
                    if (x.Kind == ExprNodeKind.LabelTarget)
                    {
                        if (!EqLabelTarget(ref x, ref y))
                            return false;
                    }
                    else if (x.Kind == ExprNodeKind.CatchBlock)
                    {
                        restoreXParameterCount = _xParameterIds.Count;
                        restoreYParameterCount = _yParameterIds.Count;
                        descendX = x.ChildIdx;
                        descendY = y.ChildIdx;
                        var hasVariable = x.Flags & CatchHasVariableFlag;
                        descendChildCount = x.ChildCount - hasVariable;
                        if (hasVariable != 0)
                        {
                            ref var xv = ref xTree.Nodes.GetSurePresentRef(descendX);
                            ref var yv = ref yTree.Nodes.GetSurePresentRef(descendY);
                            if (!AreEquivalentParameterDeclarations(ref xv, ref yv))
                                return false;
                            _xParameterIds.Add(ToStoredUShortIdx(xv.ChildIdx));
                            _yParameterIds.Add(ToStoredUShortIdx(yv.ChildIdx));
                            descendX = xv.NextIdx;
                            descendY = yv.NextIdx;
                        }
                    }
                    else
                    {
                        switch (x.NodeType)
                        {
                            case ExpressionType.Parameter:
                                if (!EqParameter(ref x, ref y))
                                    return false;
                                break;

                            case ExpressionType.Constant:
                                if (!AreConstantsEqual(ref xTree, ref x, ref yTree, ref y))
                                    return false;
                                break;

                            case ExpressionType.Lambda:
                                if (x.ChildCount == 0)
                                    return false;

                                restoreXParameterCount = _xParameterIds.Count;
                                restoreYParameterCount = _yParameterIds.Count;
                                descendX = x.ChildIdx;
                                descendY = y.ChildIdx;
                                descendChildCount = 1;
                                var xParameterIdx = xTree.Nodes.GetSurePresentRef(descendX).NextIdx;
                                var yParameterIdx = yTree.Nodes.GetSurePresentRef(descendY).NextIdx;
                                for (var i = 1; i < x.ChildCount; ++i)
                                {
                                    ref var xp = ref xTree.Nodes.GetSurePresentRef(xParameterIdx);
                                    ref var yp = ref yTree.Nodes.GetSurePresentRef(yParameterIdx);
                                    if (!AreEquivalentParameterDeclarations(ref xp, ref yp))
                                        return false;
                                    _xParameterIds.Add(ToStoredUShortIdx(xp.ChildIdx));
                                    _yParameterIds.Add(ToStoredUShortIdx(yp.ChildIdx));
                                    xParameterIdx = xp.NextIdx;
                                    yParameterIdx = yp.NextIdx;
                                }
                                break;

                            case ExpressionType.Block:
                                if (x.ChildCount == 0)
                                    return false;

                                restoreXParameterCount = _xParameterIds.Count;
                                restoreYParameterCount = _yParameterIds.Count;
                                descendX = x.ChildIdx;
                                descendY = y.ChildIdx;
                                descendChildCount = 1;
                                if (x.ChildCount == 2)
                                {
                                    ref var xVariables = ref xTree.Nodes.GetSurePresentRef(descendX);
                                    ref var yVariables = ref yTree.Nodes.GetSurePresentRef(descendY);
                                    if (xVariables.Kind != ExprNodeKind.BlockExprs || yVariables.Kind != ExprNodeKind.BlockExprs ||
                                        xVariables.ChildCount != yVariables.ChildCount)
                                        return false;

                                    var xVariableIdx = xVariables.ChildIdx;
                                    var yVariableIdx = yVariables.ChildIdx;
                                    for (var i = 0; i < xVariables.ChildCount; ++i)
                                    {
                                        ref var xv = ref xTree.Nodes.GetSurePresentRef(xVariableIdx);
                                        ref var yv = ref yTree.Nodes.GetSurePresentRef(yVariableIdx);
                                        if (!AreEquivalentParameterDeclarations(ref xv, ref yv))
                                            return false;
                                        _xParameterIds.Add(ToStoredUShortIdx(xv.ChildIdx));
                                        _yParameterIds.Add(ToStoredUShortIdx(yv.ChildIdx));
                                        xVariableIdx = xv.NextIdx;
                                        yVariableIdx = yv.NextIdx;
                                    }

                                    descendX = xVariables.NextIdx;
                                    descendY = yVariables.NextIdx;
                                }
                                break;

                            default:
                                if (!EqObj(ref x, ref y))
                                    return false;
                                if (x.ChildCount != 0)
                                {
                                    descendX = x.ChildIdx;
                                    descendY = y.ChildIdx;
                                    descendChildCount = x.ChildCount;
                                }
                                break;
                        }
                    }
                }

                if (descendChildCount != 0)
                {
                    _eqFrames.Add(new TraversalFrame(x.NextIdx, y.NextIdx, remainingSiblings, restoreXParameterCount, restoreYParameterCount));
                    xIdx = descendX;
                    yIdx = descendY;
                    remainingSiblings = descendChildCount - 1;
                    continue;
                }

                var advanced = false;
                while (true)
                {
                    if (remainingSiblings != 0)
                    {
                        xIdx = x.NextIdx;
                        yIdx = y.NextIdx;
                        remainingSiblings--;
                        advanced = true;
                        break;
                    }

                    if (_eqFrames.Count == 0)
                        return true;

                    var frame = _eqFrames[_eqFrames.Count - 1];
                    _eqFrames.Count -= 1;
                    if (frame.XParameterCount >= 0)
                        _xParameterIds.Count = frame.XParameterCount;
                    if (frame.YParameterCount >= 0)
                        _yParameterIds.Count = frame.YParameterCount;
                    if (frame.RemainingSiblingsAfterNode != 0)
                    {
                        xIdx = frame.XNextIdx;
                        yIdx = frame.YNextIdx;
                        remainingSiblings = frame.RemainingSiblingsAfterNode - 1;
                        advanced = true;
                        break;
                    }
                }
                if (advanced)
                    continue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Hash(ref ExprTree tree) =>
            tree.Nodes.Count == 0 ? 0 : HashNode(ref tree, tree.RootIdx);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Combine(int h1, int h2) =>
            unchecked(h1 ^ (h2 + (int)0x9e3779b9 + (h1 << 6) + (h1 >> 2)));

        private bool EqParameter(ref ExprNode x, ref ExprNode y)
        {
            var xId = ToStoredUShortIdx(x.ChildIdx);
            for (var i = 0; i < _xParameterIds.Count; ++i)
                if (_xParameterIds[i] == xId)
                    return _yParameterIds[i] == ToStoredUShortIdx(y.ChildIdx);

            return x.HasFlag(ParameterByRefFlag) == y.HasFlag(ParameterByRefFlag) &&
                Equals(x.Obj, y.Obj);
        }

        private bool EqLabelTarget(ref ExprNode x, ref ExprNode y)
        {
            var xId = ToStoredUShortIdx(x.ChildIdx);
            for (var i = 0; i < _xLabelIds.Count; ++i)
                if (_xLabelIds[i] == xId)
                    return _yLabelIds[i] == ToStoredUShortIdx(y.ChildIdx);

            _xLabelIds.Add(xId);
            _yLabelIds.Add(ToStoredUShortIdx(y.ChildIdx));
            return Equals(x.Obj, y.Obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreEquivalentParameterDeclarations(ref ExprNode x, ref ExprNode y) =>
            x.NodeType == ExpressionType.Parameter &&
            y.NodeType == ExpressionType.Parameter &&
            x.HasSameShapeExceptChildIdx(ref y);

        private static bool EqObj(ref ExprNode x, ref ExprNode y) =>
            ReferenceEquals(x.Obj, y.Obj) || Equals(x.Obj, y.Obj);

        private int HashNode(ref ExprTree tree, int idx)
        {
            ref var node = ref tree.Nodes.GetSurePresentRef(idx);
            if (node.Kind == ExprNodeKind.LabelTarget)
                return Combine(Combine((int)node.Kind, node.Type?.GetHashCode() ?? 0), node.Obj?.GetHashCode() ?? 0);

            if (node.Kind == ExprNodeKind.CatchBlock)
                return HashCatchBlock(ref tree, idx, ref node);

            if (node.Kind == ExprNodeKind.UInt16Pair)
                return Combine(Combine((int)node.Kind, node.ChildIdx), node.ChildCount);

            var h = Combine(Combine((int)node.Kind, (int)node.NodeType), node.Type?.GetHashCode() ?? 0);
            h = Combine(h, node.Flags);

            switch (node.NodeType)
            {
                case ExpressionType.Parameter:
                    {
                        var id = ToStoredUShortIdx(node.ChildIdx);
                        for (var i = 0; i < _xParameterIds.Count; ++i)
                            if (_xParameterIds[i] == id)
                                return Combine(h, i);
                        return Combine(h, node.Obj?.GetHashCode() ?? 0);
                    }

                case ExpressionType.Constant:
                    return Combine(h, GetConstantHashCode(ref tree, ref node));

                case ExpressionType.Lambda:
                    return HashLambda(ref tree, idx, h);

                case ExpressionType.Block:
                    return HashBlock(ref tree, idx, h);
            }

            h = Combine(h, node.Obj?.GetHashCode() ?? 0);
            var childIdx = node.ChildIdx;
            for (var i = 0; i < node.ChildCount; ++i)
            {
                h = Combine(h, HashNode(ref tree, childIdx));
                childIdx = tree.Nodes.GetSurePresentRef(childIdx).NextIdx;
            }
            return h;
        }

        private int HashLambda(ref ExprTree tree, int idx, int h)
        {
            var scopeCount = _xParameterIds.Count;
            ref var node = ref tree.Nodes.GetSurePresentRef(idx);
            var bodyIdx = node.ChildIdx;
            var parameterIdx = tree.Nodes.GetSurePresentRef(bodyIdx).NextIdx;
            for (var i = 1; i < node.ChildCount; ++i)
            {
                ref var parameter = ref tree.Nodes.GetSurePresentRef(parameterIdx);
                _xParameterIds.Add(ToStoredUShortIdx(parameter.ChildIdx));
                h = Combine(h, Combine(parameter.Type?.GetHashCode() ?? 0, parameter.HasFlag(ParameterByRefFlag) ? 1 : 0));
                parameterIdx = parameter.NextIdx;
            }

            h = Combine(h, HashNode(ref tree, bodyIdx));
            _xParameterIds.Count = scopeCount;
            return h;
        }

        private int HashBlock(ref ExprTree tree, int idx, int h)
        {
            var scopeCount = _xParameterIds.Count;
            ref var node = ref tree.Nodes.GetSurePresentRef(idx);
            var bodyListIdx = node.ChildIdx;
            if (node.ChildCount == 2)
            {
                ref var variables = ref tree.Nodes.GetSurePresentRef(bodyListIdx);
                var variableIdx = variables.ChildIdx;
                for (var i = 0; i < variables.ChildCount; ++i)
                {
                    ref var variable = ref tree.Nodes.GetSurePresentRef(variableIdx);
                    _xParameterIds.Add(ToStoredUShortIdx(variable.ChildIdx));
                    h = Combine(h, Combine(variable.Type?.GetHashCode() ?? 0, variable.HasFlag(ParameterByRefFlag) ? 1 : 0));
                    variableIdx = variable.NextIdx;
                }
                bodyListIdx = variables.NextIdx;
            }

            h = Combine(h, HashNode(ref tree, bodyListIdx));
            _xParameterIds.Count = scopeCount;
            return h;
        }

        private int HashCatchBlock(ref ExprTree tree, int idx, ref ExprNode node)
        {
            var h = Combine(Combine((int)node.Kind, node.Type?.GetHashCode() ?? 0), node.Flags);
            var scopeCount = _xParameterIds.Count;
            var childIdx = 0;
            var catchChildIdx = node.ChildIdx;
            if (node.HasFlag(CatchHasVariableFlag))
            {
                ref var variable = ref tree.Nodes.GetSurePresentRef(catchChildIdx);
                _xParameterIds.Add(ToStoredUShortIdx(variable.ChildIdx));
                h = Combine(h, Combine(variable.Type?.GetHashCode() ?? 0, variable.HasFlag(ParameterByRefFlag) ? 1 : 0));
                catchChildIdx = variable.NextIdx;
                childIdx++;
            }

            h = Combine(h, HashNode(ref tree, catchChildIdx));
            catchChildIdx = tree.Nodes.GetSurePresentRef(catchChildIdx).NextIdx;
            childIdx++;
            if (node.HasFlag(CatchHasFilterFlag))
                h = Combine(h, HashNode(ref tree, catchChildIdx));

            _xParameterIds.Count = scopeCount;
            return h;
        }

        private static int GetConstantHashCode(ref ExprTree tree, ref ExprNode node)
        {
            if (ReferenceEquals(node.Obj, ExprNode.InlineValueMarker))
                return GetInlineConstantHashCode(node.Type, node.InlineValue);

            Debug.Assert(!ExprNode.RequiresInlineConstantStorage(node.Type, node.Obj, node.NodeType));
            return GetStoredConstantValue(ref tree, ref node)?.GetHashCode() ?? 0;
        }

        private static bool AreConstantsEqual(ref ExprTree xTree, ref ExprNode x, ref ExprTree yTree, ref ExprNode y)
        {
            var xInline = ReferenceEquals(x.Obj, ExprNode.InlineValueMarker);
            var yInline = ReferenceEquals(y.Obj, ExprNode.InlineValueMarker);
            Debug.Assert(xInline == yInline);
            if (xInline != yInline)
                return false;

            if (!xInline)
            {
                Debug.Assert(!ExprNode.RequiresInlineConstantStorage(x.Type, x.Obj, x.NodeType));
                var xObj = GetStoredConstantValue(ref xTree, ref x);
                var yObj = GetStoredConstantValue(ref yTree, ref y);
                return xObj?.Equals(yObj) ?? yObj == null;
            }

            if (x.Type.IsEnum)
                return x.InlineValue == y.InlineValue;

            var typeCode = Type.GetTypeCode(x.Type);
            Debug.Assert(In32BitRange(typeCode));
            return typeCode != TypeCode.Single
                ? x.InlineValue == y.InlineValue
                : FloatBits.ToFloat(x.InlineValue).Equals(FloatBits.ToFloat(y.InlineValue));
        }

        private static object GetStoredConstantValue(ref ExprTree tree, ref ExprNode node) =>
            ReferenceEquals(node.Obj, ClosureConstantMarker) ? tree.ClosureConstants[node.ChildIdx] : node.Obj;

        private static int GetInlineConstantHashCode(Type type, uint data)
        {
            if (!type.IsEnum)
            {
                var typeCode = Type.GetTypeCode(type);
                Debug.Assert(In32BitRange(typeCode));
                if (typeCode == TypeCode.Single)
                    return FloatBits.ToFloat(data).GetHashCode();
            }

            return data.GetHashCode();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TraversalFrame
        {
            public int RemainingSiblingsAfterNode;
            public int XParameterCount;
            public int YParameterCount;
            public ushort XNextIdx;
            public ushort YNextIdx;

            public TraversalFrame(int xNextIdx, int yNextIdx, int remainingSiblingsAfterNode, int xParameterCount, int yParameterCount)
            {
                RemainingSiblingsAfterNode = remainingSiblingsAfterNode;
                XParameterCount = xParameterCount;
                YParameterCount = yParameterCount;
                XNextIdx = checked((ushort)xNextIdx);
                YNextIdx = checked((ushort)yNextIdx);
            }
        }
    }

    /// <summary>Reconstructs System.Linq nodes from the flat representation while reusing parameter and label identities.</summary>
    private struct SysExprBuilder
    {
        private readonly ExprTree _tree;
        private SmallMap16<int, SysParameterExpression, IntEq> _parametersById;
        private SmallMap16<int, SysLabelTarget, IntEq> _labelsById;

        public SysExprBuilder(ExprTree tree)
        {
            _tree = tree;
            _parametersById = default;
            _labelsById = default;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        public SysExpr ReadExpression(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            if (node.Kind != ExprNodeKind.Expression)
                throw new InvalidOperationException($"Node at idx {idx} is not an expression node.");

            switch (node.NodeType)
            {
                case ExpressionType.Constant:
                    if (ReferenceEquals(node.Obj, ClosureConstantMarker))
                        return SysExpr.Constant(_tree.ClosureConstants[node.ChildIdx], node.Type);
                    if (ReferenceEquals(node.Obj, ExprNode.InlineValueMarker))
                        return SysExpr.Constant(ReadInlineValue(node.Type, node.InlineValue), node.Type);
                    return SysExpr.Constant(node.Obj, node.Type);
                case ExpressionType.Default:
                    return SysExpr.Default(node.Type);
                case ExpressionType.Parameter:
                    {
                        ref var parameter = ref _parametersById.Map.AddOrGetValueRef(node.ChildIdx, out var found);
                        if (found)
                            return parameter;

                        var parameterType = node.HasFlag(ParameterByRefFlag) && !node.Type.IsByRef ? node.Type.MakeByRefType() : node.Type;
                        return parameter = SysExpr.Parameter(parameterType, (string)node.Obj);
                    }
                case ExpressionType.Lambda:
                    {
                        // Layout: children[0] = body, children[1..n] = parameter decl nodes.
                        // Body is read first; parameter refs inside it are resolved via _parametersById
                        // even before the decl nodes at children[1..n] are visited (out-of-order decl).
                        var children = GetChildren(idx);
                        var body = ReadExpression(children[0]);
                        var parameters = new SysParameterExpression[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            parameters[i - 1] = (SysParameterExpression)ReadExpression(children[i]);
                        return SysExpr.Lambda(node.Type, body, parameters);
                    }
                case ExpressionType.Block:
                    {
                        // With variables: children[0] is the variable list and children[1] is the expression list.
                        // Without variables: children[0] is the expression list.
                        // children.Count == 2 means the block has explicit variables.
                        // Variable decl nodes in children[0] are registered in _parametersById before
                        // the body expressions in children[1] are read, so refs in the body resolve
                        // to the same SysParameterExpression object as the decl (normal order here).
                        var children = GetChildren(idx);
                        var hasVariables = children.Count == 2;
                        var variableIdxs = hasVariables ? GetChildren(children[0]) : default;
                        var expressionIdxs = GetChildren(children[children.Count - 1]);
                        var variables = new SysParameterExpression[variableIdxs.Count];
                        for (var i = 0; i < variables.Length; ++i)
                            variables[i] = (SysParameterExpression)ReadExpression(variableIdxs[i]);
                        var expressions = new SysExpr[expressionIdxs.Count];
                        for (var i = 0; i < expressions.Length; ++i)
                            expressions[i] = ReadExpression(expressionIdxs[i]);
                        return SysExpr.Block(node.Type, variables, expressions);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var children = GetChildren(idx);
                        return SysExpr.MakeMemberAccess(children.Count != 0 ? ReadExpression(children[0]) : null, (MemberInfo)node.Obj);
                    }
                case ExpressionType.Call:
                    {
                        var method = (MethodInfo)node.Obj;
                        var children = GetChildren(idx);
                        var hasInstance = !method.IsStatic;
                        var instance = hasInstance ? ReadExpression(children[0]) : null;
                        var arguments = new SysExpr[children.Count - (hasInstance ? 1 : 0)];
                        for (var i = hasInstance ? 1 : 0; i < children.Count; ++i)
                            arguments[i - (hasInstance ? 1 : 0)] = ReadExpression(children[i]);
                        return SysExpr.Call(instance, method, arguments);
                    }
                case ExpressionType.New:
                    {
                        var children = GetChildren(idx);
                        var arguments = ReadExpressions(children);
                        return node.Obj is ConstructorInfo ctor
                            ? SysExpr.New(ctor, arguments)
                            : CreateValueTypeNewExpression(node.Type);
                    }
                case ExpressionType.NewArrayInit:
                    return SysExpr.NewArrayInit(node.Type.GetElementType(), ReadExpressions(GetChildren(idx)));
                case ExpressionType.NewArrayBounds:
                    return SysExpr.NewArrayBounds(node.Type.GetElementType(), ReadExpressions(GetChildren(idx)));
                case ExpressionType.Invoke:
                    {
                        var children = GetChildren(idx);
                        var arguments = new SysExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children[i]);
                        return SysExpr.Invoke(ReadExpression(children[0]), arguments);
                    }
                case ExpressionType.Index:
                    {
                        var children = GetChildren(idx);
                        var property = (PropertyInfo)node.Obj;
                        var hasInstance = property != null || children.Count > 1;
                        var instance = hasInstance ? ReadExpression(children[0]) : null;
                        var arguments = new SysExpr[children.Count - (hasInstance ? 1 : 0)];
                        for (var i = hasInstance ? 1 : 0; i < children.Count; ++i)
                            arguments[i - (hasInstance ? 1 : 0)] = ReadExpression(children[i]);
                        return property != null
                            ? SysExpr.Property(instance, property, arguments)
                            : SysExpr.ArrayAccess(instance, arguments);
                    }
                case ExpressionType.Conditional:
                    {
                        var children = GetChildren(idx);
                        return SysExpr.Condition(ReadExpression(children[0]), ReadExpression(children[1]), ReadExpression(children[2]), node.Type);
                    }
                case ExpressionType.Loop:
                    {
                        var children = GetChildren(idx);
                        var childIdx = 1;
                        var breakLabel = node.HasFlag(LoopHasBreakFlag) ? ReadLabelTarget(children[childIdx++]) : null;
                        var continueLabel = node.HasFlag(LoopHasContinueFlag) ? ReadLabelTarget(children[childIdx]) : null;
                        return SysExpr.Loop(ReadExpression(children[0]), breakLabel, continueLabel);
                    }
                case ExpressionType.Goto:
                    {
                        var children = GetChildren(idx);
                        var value = children.Count > 1 ? ReadExpression(children[1]) : null;
                        return SysExpr.MakeGoto((GotoExpressionKind)node.Obj, ReadLabelTarget(children[0]), value, node.Type);
                    }
                case ExpressionType.Label:
                    {
                        var children = GetChildren(idx);
                        var defaultValue = children.Count > 1 ? ReadExpression(children[1]) : null;
                        return SysExpr.Label(ReadLabelTarget(children[0]), defaultValue);
                    }
                case ExpressionType.Switch:
                    {
                        var children = GetChildren(idx);
                        var defaultBody = default(SysExpr);
                        ChildIdxs caseIdxs = default;
                        if (children.Count > 1)
                        {
                            ref var lastChild = ref _tree.Nodes[children[children.Count - 1]];
                            if (lastChild.Is(ExprNodeKind.BlockExprs))
                            {
                                caseIdxs = GetChildren(children[children.Count - 1]);
                                if (children.Count == 3)
                                    defaultBody = ReadExpression(children[1]);
                            }
                            else
                                defaultBody = ReadExpression(children[1]);
                        }
                        var cases = new SysSwitchCase[caseIdxs.Count];
                        for (var i = 0; i < cases.Length; ++i)
                            cases[i] = ReadSwitchCase(caseIdxs[i]);
                        return SysExpr.Switch(node.Type, ReadExpression(children[0]), defaultBody, (MethodInfo)node.Obj, cases);
                    }
                case ExpressionType.Try:
                    {
                        var children = GetChildren(idx);
                        if (node.HasFlag(TryFaultFlag))
                            return SysExpr.TryFault(ReadExpression(children[0]), ReadExpression(children[1]));

                        var handlers = default(SysCatchBlock[]);
                        var lastChildIsHandlerList = children.Count > 1 && _tree.Nodes[children[children.Count - 1]].Is(ExprNodeKind.BlockExprs);
                        if (lastChildIsHandlerList)
                        {
                            var handlerIdxs = GetChildren(children[children.Count - 1]);
                            handlers = new SysCatchBlock[handlerIdxs.Count];
                            for (var i = 0; i < handlers.Length; ++i)
                                handlers[i] = ReadCatchBlock(handlerIdxs[i]);
                        }
                        else
                            handlers = Array.Empty<SysCatchBlock>();

                        var @finally = children.Count > 1 && (!lastChildIsHandlerList || children.Count == 3)
                            ? ReadExpression(children[1])
                            : null;
                        return SysExpr.TryCatchFinally(ReadExpression(children[0]), @finally, handlers);
                    }
                case ExpressionType.MemberInit:
                    {
                        var children = GetChildren(idx);
                        var bindings = new SysMemberBinding[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            bindings[i - 1] = ReadMemberBinding(children[i]);
                        return SysExpr.MemberInit((NewExpression)ReadExpression(children[0]), bindings);
                    }
                case ExpressionType.ListInit:
                    {
                        var children = GetChildren(idx);
                        var initializers = new SysElementInit[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            initializers[i - 1] = ReadElementInit(children[i]);
                        return SysExpr.ListInit((NewExpression)ReadExpression(children[0]), initializers);
                    }
                case ExpressionType.TypeIs:
                    return SysExpr.TypeIs(ReadExpression(GetChildren(idx)[0]), (Type)node.Obj);
                case ExpressionType.TypeEqual:
                    return SysExpr.TypeEqual(ReadExpression(GetChildren(idx)[0]), (Type)node.Obj);
                case ExpressionType.Dynamic:
                    {
                        var children = GetChildren(idx);
                        var delegateType = (Type)ReadObjectReference(children[0]);
                        var arguments = new SysExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children[i]);
                        return SysExpr.MakeDynamic(delegateType, (CallSiteBinder)node.Obj, arguments);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var children = GetChildren(idx);
                        var variables = new SysParameterExpression[children.Count];
                        for (var i = 0; i < children.Count; ++i)
                            variables[i] = (SysParameterExpression)ReadExpression(children[i]);
                        return SysExpr.RuntimeVariables(variables);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var children = GetChildren(idx);
                        ReadUInt16Pair(children[0], out var startLine, out var startColumn);
                        ReadUInt16Pair(children[1], out var endLine, out var endColumn);
                        return SysExpr.DebugInfo(SysExpr.SymbolDocument((string)node.Obj), startLine, startColumn, endLine, endColumn);
                    }
                default:
                    if (node.ChildCount == 1)
                    {
                        var method = node.Obj as MethodInfo;
                        return SysExpr.MakeUnary(node.NodeType, ReadExpression(GetChildren(idx)[0]), node.Type, method);
                    }

                    if (node.ChildCount >= 2)
                    {
                        var children = GetChildren(idx);
                        var conversion = children.Count > 2 ? (LambdaExpression)ReadExpression(children[2]) : null;
                        return SysExpr.MakeBinary(node.NodeType, ReadExpression(children[0]), ReadExpression(children[1]),
                            node.HasFlag(BinaryLiftedToNullFlag), (MethodInfo)node.Obj, conversion);
                    }

                    throw new NotSupportedException($"Reconstruction of `ExpressionType.{node.NodeType}` is not supported yet.");
            }
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysSwitchCase ReadSwitchCase(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.SwitchCase));
            var children = GetChildren(idx);
            var testValues = new SysExpr[children.Count - 1];
            for (var i = 0; i < testValues.Length; ++i)
                testValues[i] = ReadExpression(children[i]);
            return SysExpr.SwitchCase(ReadExpression(children[children.Count - 1]), testValues);
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysCatchBlock ReadCatchBlock(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.CatchBlock));
            var children = GetChildren(idx);
            var childIdx = 0;
            var variable = node.HasFlag(CatchHasVariableFlag) ? (SysParameterExpression)ReadExpression(children[childIdx++]) : null;
            var body = ReadExpression(children[childIdx++]);
            var filter = node.HasFlag(CatchHasFilterFlag) ? ReadExpression(children[childIdx]) : null;
            return SysExpr.MakeCatchBlock(node.Type, variable, body, filter);
        }

        private SysLabelTarget ReadLabelTarget(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.LabelTarget));
            ref var label = ref _labelsById.Map.AddOrGetValueRef(node.ChildIdx, out var found);
            if (found)
                return label;

            return label = SysExpr.Label(node.Type, (string)node.Obj);
        }

        private object ReadObjectReference(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.ObjectReference));
            return node.Obj;
        }

        private void ReadUInt16Pair(int idx, out int first, out int second)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.UInt16Pair));
            first = node.ChildIdx;
            second = node.ChildCount;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysMemberBinding ReadMemberBinding(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            var member = (MemberInfo)node.Obj;
            switch (node.Kind)
            {
                case ExprNodeKind.MemberAssignment:
                    return SysExpr.Bind(member, ReadExpression(GetChildren(idx)[0]));
                case ExprNodeKind.MemberMemberBinding:
                    {
                        var childIdxs = GetChildren(idx);
                        var bindings = new SysMemberBinding[childIdxs.Count];
                        for (var i = 0; i < childIdxs.Count; ++i)
                            bindings[i] = ReadMemberBinding(childIdxs[i]);
                        return SysExpr.MemberBind(member, bindings);
                    }
                case ExprNodeKind.MemberListBinding:
                    {
                        var childIdxs = GetChildren(idx);
                        var initializers = new SysElementInit[childIdxs.Count];
                        for (var i = 0; i < childIdxs.Count; ++i)
                            initializers[i] = ReadElementInit(childIdxs[i]);
                        return SysExpr.ListBind(member, initializers);
                    }
                default:
                    throw new InvalidOperationException($"Node at idx {idx} is not a member binding node.");
            }
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysElementInit ReadElementInit(int idx)
        {
            ref var node = ref _tree.Nodes[idx];
            Debug.Assert(node.Is(ExprNodeKind.ElementInit));
            return SysExpr.ElementInit((MethodInfo)node.Obj, ReadExpressions(GetChildren(idx)));
        }

        private ChildIdxs GetChildren(int idx)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            var count = node.ChildCount;
            ChildIdxs children = default;
            var childIdx = node.ChildIdx;
            for (var i = 0; i < count; ++i)
            {
                children.Add(childIdx);
                childIdx = _tree.Nodes.GetSurePresentRef(childIdx).NextIdx;
            }
            return children;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ReadInlineValue(Type type, uint data)
        {
            if (type.IsEnum)
                return Enum.ToObject(type, Type.GetTypeCode(Enum.GetUnderlyingType(type)) switch
                {
                    TypeCode.Byte => (object)(byte)data,
                    TypeCode.SByte => (object)(sbyte)(byte)data,
                    TypeCode.Char => (object)(char)(ushort)data,
                    TypeCode.Int16 => (object)(short)(ushort)data,
                    TypeCode.UInt16 => (object)(ushort)data,
                    TypeCode.Int32 => (object)(int)data,
                    TypeCode.UInt32 => (object)data,
                    var tc => FlatExpressionThrow.UnsupportedInlineConstantType<object>(type, tc)
                });
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => (object)(data != 0),
                TypeCode.Byte => (object)(byte)data,
                TypeCode.SByte => (object)(sbyte)(byte)data,
                TypeCode.Char => (object)(char)(ushort)data,
                TypeCode.Int16 => (object)(short)(ushort)data,
                TypeCode.UInt16 => (object)(ushort)data,
                TypeCode.Int32 => (object)(int)data,
                TypeCode.UInt32 => (object)data,
                TypeCode.Single => (object)FloatBits.ToFloat(data),
                _ => FlatExpressionThrow.UnsupportedInlineConstantType<object>(type)
            };
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysExpr[] ReadExpressions(in ChildIdxs childIdxs)
        {
            var expressions = new SysExpr[childIdxs.Count];
            for (var i = 0; i < expressions.Length; ++i)
                expressions[i] = ReadExpression(childIdxs[i]);
            return expressions;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077",
            Justification = "Flat expression round-trip stores the runtime type metadata explicitly for reconstruction.")]
        private static NewExpression CreateValueTypeNewExpression(Type type) => SysExpr.New(type);
    }
}

/// <summary>Builds the flat representation while preserving parameter and label identity.</summary>
public static class FlatExprBuilder
{
    private SmallMap16<object, int, RefEq<object>> _parameterIds;
    private SmallMap16<object, int, RefEq<object>> _labelIds;
    private ExprTree _tree;

    public ExprTree Build(SysExpr sysExpr)
    {
        _tree.RootIdx = AddExpression(sysExpr);
        return _tree;
    }
}

/// <summary>Union struct for reinterpreting float bits as uint without unsafe code.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct FloatBits
{
    [FieldOffset(0)] private float _floatValue;
    [FieldOffset(0)] private uint _uintValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ToUInt(float value) => new FloatBits { _floatValue = value }._uintValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ToFloat(uint value) => new FloatBits { _uintValue = value }._floatValue;
}

/// <summary>Throw helpers that prevent bare <c>throw</c> from blocking inlining of hot-path callers.</summary>
internal static class FlatExpressionThrow
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T UnsupportedInlineConstantType<T>(Type type) =>
        throw new NotSupportedException($"Cannot reconstruct inline constant of type {type}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T UnsupportedInlineConstantType<T>(Type type, TypeCode tc) =>
        throw new NotSupportedException($"Cannot reconstruct inline constant of type {type} with TypeCode {tc}");

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T UnsupportedInlineConstantType<T>(object value, TypeCode tc) =>
        throw new NotSupportedException($"Cannot convert value '{value}' of TypeCode {tc} to an inline constant");
}

/// <summary>Provides conversions from System and LightExpression trees to <see cref="ExprTree"/>.</summary>
public static class FlatExpressionExtensions
{
    /// <summary>Flattens a System.Linq expression tree.</summary>
    public static ref ExprTree ToFlatExpression(this SysExpr expression, ref ExprTree exprTree)
    {
        exprTree.FromSysExpr(expression);
        return ref exprTree;
    }
}
