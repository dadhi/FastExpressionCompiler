namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FastExpressionCompiler.LightExpression.ImTools;
using ChildList = FastExpressionCompiler.LightExpression.ImTools.SmallList<int, FastExpressionCompiler.LightExpression.ImTools.Stack16<int>, FastExpressionCompiler.LightExpression.ImTools.NoArrayPool<int>>;
using LightExpression = FastExpressionCompiler.LightExpression.Expression;
using SysCatchBlock = System.Linq.Expressions.CatchBlock;
using SysElementInit = System.Linq.Expressions.ElementInit;
using SysExpr = System.Linq.Expressions.Expression;
using SysLabelTarget = System.Linq.Expressions.LabelTarget;
using SysMemberBinding = System.Linq.Expressions.MemberBinding;
using SysParameterExpression = System.Linq.Expressions.ParameterExpression;
using SysSwitchCase = System.Linq.Expressions.SwitchCase;

/// <summary>Classifies the stored flat node payload.</summary>
public enum ExprNodeKind : byte
{
    /// <summary>Represents a regular expression node.</summary>
    Expression,
    /// <summary>Represents a switch case payload.</summary>
    SwitchCase,
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
    /// <summary>Represents an internal child-list metadata node.</summary>
    ChildList,
    /// <summary>Represents an internal pair of UInt16 values.</summary>
    UInt16Pair,
}

/// <summary>Maps a lambda node to a parameter identity used from an outer scope and therefore captured in closure.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly struct LambdaClosureParameterUsage
{
    /// <summary>The lambda node index containing the parameter usage.</summary>
    public readonly short LambdaNodeIndex;

    /// <summary>The parameter identity id (<see cref="ExprNode.ChildIdx"/>) referenced from outer scope.</summary>
    public readonly short ParameterId;

    public LambdaClosureParameterUsage(short lambdaNodeIndex, short parameterId)
    {
        LambdaNodeIndex = lambdaNodeIndex;
        ParameterId = parameterId;
    }
}

/// <summary>Stores one flat expression node plus its intrusive child-link metadata in 24 bytes on 64-bit runtimes.</summary>
/// <remarks>
/// Layout (64-bit): Type(8) | Obj(8) | _meta(4) | _data(4) = 24 bytes.
/// _meta bits: NodeType(8)|Tag(8)|NextIdx(16).
/// _data bits: ChildCount(16)|ChildIdx(16) for regular nodes,
///             or the raw 32-bit value for inline primitive constants (when <see cref="Obj"/> == <see cref="InlineValueMarker"/>).
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct ExprNode
{
    // _meta layout: bits [31:24]=NodeType | [23:20]=Flags | [19:16]=Kind | [15:0]=NextIdx
    private const int MetaNodeTypeShift = 24;
    private const int MetaTagShift = 16;
    private const uint MetaKeepWithoutNext = 0xFFFF0000u;
    // _data layout: bits [31:16]=ChildCount | [15:0]=ChildIdx  (or full uint for inline constants)
    private const int DataCountShift = 16;
    private const uint DataIdxMask = 0xFFFFu;
    private const int FlagsShift = 4;
    private const uint KindMask = 0x0Fu;

    /// <summary>Sentinel placed in <see cref="Obj"/> to indicate the node holds a small primitive constant in <see cref="InlineValue"/>.</summary>
    internal static readonly object InlineValueMarker = new();

    /// <summary>Gets or sets the runtime type of the represented node.</summary>
    [FieldOffset(0)]
    public Type Type;

    /// <summary>Gets or sets the runtime payload associated with the node.</summary>
    [FieldOffset(8)]
    public object Obj;

    /// <summary>NodeType(8b) | Tag=(Flags:4b|Kind:4b)(8b) | NextIdx(16b)</summary>
    [FieldOffset(16)]
    private uint _meta;

    /// <summary>ChildCount(16b) | ChildIdx(16b)  —OR—  raw 32-bit inline constant value.</summary>
    [FieldOffset(20)]
    private uint _data;

    /// <summary>Gets the expression kind encoded for this node.</summary>
    public ExpressionType NodeType => (ExpressionType)(_meta >> MetaNodeTypeShift);

    /// <summary>Gets the payload classification for this node.</summary>
    public ExprNodeKind Kind => (ExprNodeKind)((_meta >> MetaTagShift) & KindMask);

    internal byte Flags => (byte)((_meta >> (MetaTagShift + FlagsShift)) & 0xFu);

    /// <summary>Gets the next sibling node index in the intrusive child chain.</summary>
    public int NextIdx => (int)(_meta & 0xFFFFu);

    /// <summary>Gets the number of direct children linked from this node.</summary>
    public int ChildCount => (int)(_data >> DataCountShift);

    /// <summary>Gets the first child index or an auxiliary payload index.</summary>
    public int ChildIdx => (int)(_data & DataIdxMask);

    /// <summary>Gets the raw 32-bit value for inline primitive constants. Only valid when <see cref="Obj"/> == <see cref="InlineValueMarker"/>.</summary>
    internal uint InlineValue => _data;

    internal ExprNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags = 0, int childIdx = 0, int childCount = 0, int nextIdx = 0)
    {
        Type = type;
        Obj = obj;
        var tag = (byte)((flags << FlagsShift) | (byte)kind);
        _meta = ((uint)(byte)nodeType << MetaNodeTypeShift) | ((uint)tag << MetaTagShift) | (ushort)nextIdx;
        _data = ((uint)(ushort)childCount << DataCountShift) | (ushort)childIdx;
    }

    /// <summary>Constructs an inline primitive constant node; <see cref="Obj"/> is set to <see cref="InlineValueMarker"/>.</summary>
    internal ExprNode(Type type, uint inlineValue)
    {
        Type = type;
        Obj = InlineValueMarker;
        _meta = (uint)(byte)ExpressionType.Constant << MetaNodeTypeShift;
        _data = inlineValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetNextIdx(int nextIdx) =>
        _meta = (_meta & MetaKeepWithoutNext) | (ushort)nextIdx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetChildInfo(int childIdx, int childCount) =>
        _data = ((uint)(ushort)childCount << DataCountShift) | (ushort)childIdx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Is(ExprNodeKind kind) => Kind == kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsExpression() => Kind == ExprNodeKind.Expression;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasFlag(byte flag) => (Flags & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ShouldCloneWhenLinked() =>
        ReferenceEquals(Obj, InlineValueMarker) ||
        Kind == ExprNodeKind.LabelTarget || NodeType == ExpressionType.Parameter ||
        Kind == ExprNodeKind.ObjectReference || ChildCount == 0;
}

/// <summary>Stores an expression tree as a flat node array plus out-of-line closure constants.</summary>
public struct ExprTree
{
    private static readonly object ClosureConstantMarker = new();
    private const byte ParameterByRefFlag = 1;
    private const byte BinaryLiftedToNullFlag = 1;
    private const byte LoopHasBreakFlag = 1;
    private const byte LoopHasContinueFlag = 2;
    private const byte CatchHasVariableFlag = 1;
    private const byte CatchHasFilterFlag = 2;
    private const byte TryFaultFlag = 1;

    /// <summary>Gets or sets the root node index.</summary>
    public int RootIndex;

    /// <summary>Gets or sets the flat node storage.</summary>
    public SmallList<ExprNode, Stack16<ExprNode>, NoArrayPool<ExprNode>> Nodes;

    /// <summary>Gets or sets closure constants that are referenced from constant nodes.</summary>
    public SmallList<object, Stack16<object>, NoArrayPool<object>> ClosureConstants;

    /// <summary>Gets or sets the indices of all lambda nodes added during construction.
    /// The root lambda index is stored in <see cref="RootIndex"/>; all other entries are nested lambdas.
    /// Populated automatically by <see cref="Lambda(Type,int,int[])"/> and <see cref="ExprTree.FromExpression"/>,
    /// enabling callers to discover nested lambdas without a full tree traversal.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> LambdaNodes;

    /// <summary>Gets or sets the indices of all block nodes that carry explicit variable declarations.
    /// These are the block nodes where <c>children.Count == 2</c> (variable list + expression list).
    /// Populated automatically by <see cref="Block(Type,IEnumerable{int},int[])"/> and <see cref="ExprTree.FromExpression"/>,
    /// enabling callers to enumerate block-scoped variables without a full tree traversal.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> BlocksWithVariables;

    /// <summary>Gets or sets the indices of all <see cref="ExpressionType.Goto"/> nodes
    /// (including <c>return</c> and <c>break</c>/<c>continue</c> goto-family nodes).
    /// Populated automatically by <see cref="MakeGoto"/> and <see cref="ExprTree.FromExpression"/>,
    /// enabling callers to link gotos to their label targets without a full tree traversal.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> GotoNodes;

    /// <summary>Gets or sets the indices of all <see cref="ExpressionType.Label"/> expression nodes.
    /// Populated automatically by <see cref="Label(int,int?)"/> and <see cref="ExprTree.FromExpression"/>,
    /// enabling callers to link label expressions to their targets without a full tree traversal.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> LabelNodes;

    /// <summary>Gets or sets the indices of all <see cref="ExpressionType.Try"/> nodes
    /// (try/catch, try/finally, try/fault, and combined forms).
    /// Populated automatically by <see cref="TryCatch"/>, <see cref="TryFinally"/>,
    /// <see cref="TryFault"/>, <see cref="TryCatchFinally"/> and <see cref="ExprTree.FromExpression"/>,
    /// enabling callers to locate all try regions without a full tree traversal.</summary>
    public SmallList<int, Stack16<int>, NoArrayPool<int>> TryCatchNodes;

    /// <summary>Gets or sets mappings of lambda-node index to captured parameter id for nested-lambda closures.
    /// Populated by <see cref="ExprTree.FromExpression"/> while flattening System.Linq lambdas;
    /// each entry means that the lambda body references a parameter that is not declared as that lambda parameter
    /// and not declared as a local block/catch variable in that lambda body scope.</summary>
    public SmallList<LambdaClosureParameterUsage, Stack16<LambdaClosureParameterUsage>, NoArrayPool<LambdaClosureParameterUsage>> LambdaClosureParameterUsages;

    /// <summary>Adds a parameter node and returns its index.</summary>
    public int Parameter(Type type, string name = null)
    {
        var id = Nodes.Count + 1;
        return AddRawLeafExpressionNode(type, name, ExpressionType.Parameter, type.IsByRef ? ParameterByRefFlag : (byte)0, childIdx: id);
    }

    /// <summary>Adds a typed parameter node and returns its index.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ParameterOf<T>(string name = null) => Parameter(typeof(T), name);

    /// <summary>Adds a variable node and returns its index.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Variable(Type type, string name = null) => Parameter(type, name);

    /// <summary>Adds a default-value node and returns its index.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Default(Type type) => AddRawExpressionNode(type, null, ExpressionType.Default);

    /// <summary>Adds a constant node using the runtime type of the supplied value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Constant(object value) =>
        Constant(value, value?.GetType() ?? typeof(object));

    /// <summary>Adds a constant node with an explicit constant type.</summary>
    public int Constant(object value, Type type)
    {
        if (value == null || value is string || value is Type || value is decimal)
            return AddRawExpressionNode(type, value, ExpressionType.Constant);

        if (type.IsEnum)
        {
            var underlyingTc = Type.GetTypeCode(Enum.GetUnderlyingType(type));
            if (IsSmallPrimitive(underlyingTc))
                return AddInlineConstantNode(type, (uint)System.Convert.ToInt64(value));
            // long/ulong-backed enum (extremely rare): store boxed in Obj
            return AddRawExpressionNode(type, value, ExpressionType.Constant);
        }

        if (type.IsPrimitive)
        {
            var tc = Type.GetTypeCode(type);
            if (IsSmallPrimitive(tc))
                return AddInlineConstantNode(type, ToInlineValue(value, tc));
            // long, ulong, double: primitive but too wide for _data, store boxed in Obj
            return AddRawExpressionNode(type, value, ExpressionType.Constant);
        }

        // Delegate, array types, and user-defined reference/value types go to ClosureConstants
        var constantIndex = ClosureConstants.Add(value);
        return AddRawLeafExpressionNode(type, ClosureConstantMarker, ExpressionType.Constant, childIdx: constantIndex);
    }

    /// <summary>Adds a null constant node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ConstantNull(Type type = null) => AddRawExpressionNode(type ?? typeof(object), null, ExpressionType.Constant);

    /// <summary>Adds an <see cref="int"/> constant node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ConstantInt(int value) => AddRawExpressionNode(typeof(int), value, ExpressionType.Constant);

    /// <summary>Adds a typed constant node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ConstantOf<T>(T value) => Constant(value, typeof(T));

    /// <summary>Adds a parameterless <c>new</c> node for the specified type.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    public int New(Type type)
    {
        if (type.IsValueType)
            return AddRawExpressionNode(type, null, ExpressionType.New);

        foreach (var ctor in type.GetConstructors())
            if (ctor.GetParameters().Length == 0)
                return New(ctor);

        throw new ArgumentException($"The type {type} is missing the default constructor");
    }

    /// <summary>Adds a constructor call node.</summary>
    public int New(System.Reflection.ConstructorInfo constructor, params int[] arguments) =>
        AddFactoryExpressionNode(constructor.DeclaringType, constructor, ExpressionType.New, arguments);

    /// <summary>Adds an array initialization node.</summary>
    public int NewArrayInit(Type elementType, params int[] expressions) =>
        AddFactoryExpressionNode(elementType.MakeArrayType(), null, ExpressionType.NewArrayInit, expressions);

    /// <summary>Adds an array-bounds node.</summary>
    public int NewArrayBounds(Type elementType, params int[] bounds) =>
        AddFactoryExpressionNode(elementType.MakeArrayType(), null, ExpressionType.NewArrayBounds, bounds);

    /// <summary>Adds an invocation node.</summary>
    public int Invoke(int expression, params int[] arguments) =>
        arguments == null || arguments.Length == 0
            ? AddFactoryExpressionNode(Nodes[expression].Type, null, ExpressionType.Invoke, expression)
            : AddFactoryExpressionNode(Nodes[expression].Type, null, ExpressionType.Invoke, PrependToChildList(expression, arguments));

    /// <summary>Adds a static-call node.</summary>
    public int Call(System.Reflection.MethodInfo method, params int[] arguments) =>
        AddFactoryExpressionNode(method.ReturnType, method, ExpressionType.Call, arguments);

    /// <summary>Adds an instance-call node.</summary>
    public int Call(int instance, System.Reflection.MethodInfo method, params int[] arguments) =>
        arguments == null || arguments.Length == 0
            ? AddFactoryExpressionNode(method.ReturnType, method, ExpressionType.Call, instance)
            : AddFactoryExpressionNode(method.ReturnType, method, ExpressionType.Call, PrependToChildList(instance, arguments));

    /// <summary>Adds a field or property access node.</summary>
    public int MakeMemberAccess(int? instance, System.Reflection.MemberInfo member) =>
        instance.HasValue
            ? AddFactoryExpressionNode(GetMemberType(member), member, ExpressionType.MemberAccess, instance.Value)
            : AddRawExpressionNode(GetMemberType(member), member, ExpressionType.MemberAccess);

    /// <summary>Adds a field-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Field(int instance, System.Reflection.FieldInfo field) => MakeMemberAccess(instance, field);

    /// <summary>Adds a property-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Property(int instance, System.Reflection.PropertyInfo property) => MakeMemberAccess(instance, property);

    /// <summary>Adds a static property-access node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Property(System.Reflection.PropertyInfo property) => MakeMemberAccess(null, property);

    /// <summary>Adds an indexed property-access node.</summary>
    public int Property(int instance, System.Reflection.PropertyInfo property, params int[] arguments) =>
        arguments == null || arguments.Length == 0
            ? Property(instance, property)
            : AddFactoryExpressionNode(property.PropertyType, property, ExpressionType.Index, PrependToChildList(instance, arguments));

    /// <summary>Adds a one-dimensional array index node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ArrayIndex(int array, int index) => MakeBinary(ExpressionType.ArrayIndex, array, index);

    /// <summary>Adds an array access node.</summary>
    public int ArrayAccess(int array, params int[] indexes) =>
        indexes != null && indexes.Length == 1
            ? ArrayIndex(array, indexes[0])
            : AddFactoryExpressionNode(GetArrayElementType(Nodes[array].Type, indexes?.Length ?? 0), null, ExpressionType.Index, PrependToChildList(array, indexes));

    /// <summary>Adds a conversion node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Convert(int operand, Type type, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type, method, ExpressionType.Convert, operand);

    /// <summary>Adds a type-as node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TypeAs(int operand, Type type) =>
        AddFactoryExpressionNode(type, null, ExpressionType.TypeAs, operand);

    /// <summary>Adds a numeric negation node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Negate(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Negate, operand, method: method);

    /// <summary>Adds a logical or bitwise not node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Not(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Not, operand, method: method);

    /// <summary>Adds a unary node of the specified kind.</summary>
    public int MakeUnary(ExpressionType nodeType, int operand, Type type = null, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type ?? GetUnaryResultType(nodeType, Nodes[operand].Type, method), method, nodeType, operand);

    /// <summary>Adds an assignment node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Assign(int left, int right) => MakeBinary(ExpressionType.Assign, left, right);

    /// <summary>Adds an addition node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(int left, int right, System.Reflection.MethodInfo method = null) => MakeBinary(ExpressionType.Add, left, right, method: method);

    /// <summary>Adds an equality node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Equal(int left, int right, System.Reflection.MethodInfo method = null) => MakeBinary(ExpressionType.Equal, left, right, method: method);

    /// <summary>Adds a binary node of the specified kind.</summary>
    public int MakeBinary(ExpressionType nodeType, int left, int right, bool isLiftedToNull = false,
        System.Reflection.MethodInfo method = null, int? conversion = null, Type type = null)
        => conversion.HasValue
            ? AddFactoryExpressionNode(type ?? GetBinaryResultType(nodeType, Nodes[left].Type, Nodes[right].Type, method),
                method, nodeType, isLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, left, right, conversion.Value)
            : AddFactoryExpressionNode(type ?? GetBinaryResultType(nodeType, Nodes[left].Type, Nodes[right].Type, method),
                method, nodeType, isLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, left, right);

    /// <summary>Adds a conditional node.</summary>
    public int Condition(int test, int ifTrue, int ifFalse, Type type = null) =>
        AddFactoryExpressionNode(type ?? Nodes[ifTrue].Type, null, ExpressionType.Conditional, 0, test, ifTrue, ifFalse);

    /// <summary>Adds a block node without explicit variables.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Block(params int[] expressions) =>
        Block(null, null, expressions);

    /// <summary>Adds a block node with optional explicit result type and variables.</summary>
    /// <remarks>
    /// Child layout of the Block node depends on whether there are explicit variables:
    /// <list type="bullet">
    ///   <item>With variables:    children[0] = ChildList(variable₀, variable₁, …)
    ///                            children[1] = ChildList(expr₀, expr₁, …)</item>
    ///   <item>Without variables: children[0] = ChildList(expr₀, expr₁, …)</item>
    /// </list>
    /// A <c>children.Count == 2</c> check is therefore the canonical way to detect variables.
    /// Variable parameter nodes share the same id-slot as the refs used inside the body
    /// (out-of-order: the variable decl nodes appear in children[0] before the body expressions
    /// that reference them in children[1]).
    /// <para>When the block has explicit variable declarations its node index is recorded in
    /// <see cref="BlocksWithVariables"/>, enabling callers to enumerate block-scoped variables
    /// without a full tree traversal.</para>
    /// </remarks>
    public int Block(Type type, IEnumerable<int> variables, params int[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("Block should contain at least one expression.", nameof(expressions));

        ChildList children = default;
        var hasVariables = false;
        if (variables != null)
        {
            ChildList variableChildren = default;
            foreach (var variable in variables)
                variableChildren.Add(variable);
            if (variableChildren.Count != 0)
            {
                children.Add(AddChildListNode(in variableChildren));
                hasVariables = true;
            }
        }
        ChildList bodyChildren = default;
        for (var i = 0; i < expressions.Length; ++i)
            bodyChildren.Add(expressions[i]);
        children.Add(AddChildListNode(in bodyChildren));
        var index = AddFactoryExpressionNode(type ?? Nodes[expressions[expressions.Length - 1]].Type, null, ExpressionType.Block, in children);
        if (hasVariables)
            BlocksWithVariables.Add(index);
        return index;
    }

    /// <summary>Adds a typed lambda node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Lambda<TDelegate>(int body, params int[] parameters) where TDelegate : Delegate =>
        Lambda(typeof(TDelegate), body, parameters);

    /// <summary>Adds a lambda node.</summary>
    /// <remarks>
    /// Child layout of the Lambda node:
    /// <list type="bullet">
    ///   <item>children[0]   = body expression</item>
    ///   <item>children[1…n] = parameter decl nodes (parameter₀, parameter₁, …)</item>
    /// </list>
    /// The body is stored first; parameter decl nodes follow. This means that when the
    /// body contains refs to those parameters, the ref nodes are encountered by the
    /// <see cref="Reader"/> before the corresponding decl node — an intentional
    /// out-of-order decl pattern. The Reader resolves identity through a shared id map
    /// so that all refs and the single decl resolve to the same
    /// <see cref="System.Linq.Expressions.ParameterExpression"/> object.
    /// <para>The lambda node index is recorded in <see cref="LambdaNodes"/> so callers can discover
    /// nested lambdas (all entries except <see cref="RootIndex"/>) without a full tree traversal.</para>
    /// </remarks>
    public int Lambda(Type delegateType, int body, params int[] parameters)
    {
        var index = parameters == null || parameters.Length == 0
            ? AddFactoryExpressionNode(delegateType, null, ExpressionType.Lambda, 0, body)
            : AddFactoryExpressionNode(delegateType, null, ExpressionType.Lambda, PrependToChildList(body, parameters));
        LambdaNodes.Add(index);
        return index;
    }

    /// <summary>Adds a member-assignment binding node.</summary>
    public int Bind(System.Reflection.MemberInfo member, int expression) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberAssignment, expression);

    /// <summary>Adds a nested member-binding node.</summary>
    public int MemberBind(System.Reflection.MemberInfo member, params int[] bindings) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberMemberBinding, bindings);

    /// <summary>Adds an element-initializer node.</summary>
    public int ElementInit(System.Reflection.MethodInfo addMethod, params int[] arguments) =>
        AddFactoryAuxNode(addMethod.DeclaringType, addMethod, ExprNodeKind.ElementInit, arguments);

    /// <summary>Adds a list-binding node.</summary>
    public int ListBind(System.Reflection.MemberInfo member, params int[] initializers) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberListBinding, initializers);

    /// <summary>Adds a member-init node.</summary>
    public int MemberInit(int @new, params int[] bindings) =>
        bindings == null || bindings.Length == 0
            ? AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.MemberInit, @new)
            : AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.MemberInit, PrependToChildList(@new, bindings));

    /// <summary>Adds a list-init node.</summary>
    public int ListInit(int @new, params int[] initializers) =>
        initializers == null || initializers.Length == 0
            ? AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.ListInit, @new)
            : AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.ListInit, PrependToChildList(@new, initializers));

    /// <summary>Adds a label-target node.</summary>
    public int Label(Type type = null, string name = null)
    {
        var id = Nodes.Count + 1;
        return AddRawLeafAuxNode(type ?? typeof(void), name, ExprNodeKind.LabelTarget, childIdx: id);
    }

    /// <summary>Adds a label-expression node.</summary>
    /// <remarks>The node index is recorded in <see cref="LabelNodes"/>.</remarks>
    public int Label(int target, int? defaultValue = null)
    {
        var index = defaultValue.HasValue
            ? AddFactoryExpressionNode(Nodes[target].Type, null, ExpressionType.Label, 0, target, defaultValue.Value)
            : AddFactoryExpressionNode(Nodes[target].Type, null, ExpressionType.Label, 0, target);
        LabelNodes.Add(index);
        return index;
    }

    /// <summary>Adds a goto-family node.</summary>
    /// <remarks>The node index is recorded in <see cref="GotoNodes"/>.</remarks>
    public int MakeGoto(GotoExpressionKind kind, int target, int? value = null, Type type = null)
    {
        var resultType = type ?? (value.HasValue ? Nodes[value.Value].Type : typeof(void));
        var index = value.HasValue
            ? AddFactoryExpressionNode(resultType, kind, ExpressionType.Goto, 0, target, value.Value)
            : AddFactoryExpressionNode(resultType, kind, ExpressionType.Goto, 0, target);
        GotoNodes.Add(index);
        return index;
    }

    /// <summary>Adds a goto node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Goto(int target, int? value = null, Type type = null) => MakeGoto(GotoExpressionKind.Goto, target, value, type);

    /// <summary>Adds a return node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Return(int target, int value) => MakeGoto(GotoExpressionKind.Return, target, value, Nodes[value].Type);

    /// <summary>Adds a loop node.</summary>
    public int Loop(int body, int? @break = null, int? @continue = null)
    {
        ChildList children = default;
        children.Add(body);
        if (@break.HasValue)
            children.Add(@break.Value);
        if (@continue.HasValue)
            children.Add(@continue.Value);
        return AddFactoryExpressionNode(typeof(void), null, ExpressionType.Loop,
            (byte)((@break.HasValue ? LoopHasBreakFlag : 0) | (@continue.HasValue ? LoopHasContinueFlag : 0)), in children);
    }

    /// <summary>Adds a switch-case node.</summary>
    public int SwitchCase(int body, params int[] testValues)
    {
        ChildList children = default;
        if (testValues != null && testValues.Length != 0)
            for (var i = 0; i < testValues.Length; ++i)
                children.Add(testValues[i]);
        children.Add(body);
        return AddFactoryAuxNode(Nodes[body].Type, null, ExprNodeKind.SwitchCase, children);
    }

    /// <summary>Adds a switch node without an explicit default case or comparer.</summary>
    public int Switch(int switchValue, params int[] cases) =>
        Switch(Nodes[switchValue].Type, switchValue, null, null, cases);

    /// <summary>Adds a switch node.</summary>
    public int Switch(Type type, int switchValue, int? defaultBody, System.Reflection.MethodInfo comparison, params int[] cases)
    {
        ChildList children = default;
        children.Add(switchValue);
        if (defaultBody.HasValue)
            children.Add(defaultBody.Value);
        if (cases != null && cases.Length != 0)
        {
            ChildList caseChildren = default;
            for (var i = 0; i < cases.Length; ++i)
                caseChildren.Add(cases[i]);
            children.Add(AddChildListNode(in caseChildren));
        }
        return AddFactoryExpressionNode(type, comparison, ExpressionType.Switch, in children);
    }

    /// <summary>Adds a catch block with an exception variable.</summary>
    public int Catch(int variable, int body) =>
        AddFactoryAuxNode(Nodes[variable].Type, null, ExprNodeKind.CatchBlock, CatchHasVariableFlag, variable, body);

    /// <summary>Adds a catch block without an exception variable.</summary>
    public int Catch(Type test, int body) =>
        AddFactoryAuxNode(test, null, ExprNodeKind.CatchBlock, 0, body);

    /// <summary>Adds a catch block with optional exception variable and filter.</summary>
    public int MakeCatchBlock(Type test, int? variable, int body, int? filter)
    {
        ChildList children = default;
        if (variable.HasValue)
            children.Add(variable.Value);
        children.Add(body);
        if (filter.HasValue)
            children.Add(filter.Value);
        return AddFactoryAuxNode(test, null, ExprNodeKind.CatchBlock,
            (byte)((variable.HasValue ? CatchHasVariableFlag : 0) | (filter.HasValue ? CatchHasFilterFlag : 0)), in children);
    }

    /// <summary>Adds a try/catch node.</summary>
    /// <remarks>The node index is recorded in <see cref="TryCatchNodes"/>.</remarks>
    public int TryCatch(int body, params int[] handlers)
    {
        int index;
        if (handlers == null || handlers.Length == 0)
        {
            index = AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, body);
        }
        else
        {
            ChildList handlerChildren = default;
            for (var i = 0; i < handlers.Length; ++i)
                handlerChildren.Add(handlers[i]);
            ChildList children = default;
            children.Add(body);
            children.Add(AddChildListNode(in handlerChildren));
            index = AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, in children);
        }
        TryCatchNodes.Add(index);
        return index;
    }

    /// <summary>Adds a try/finally node.</summary>
    /// <remarks>The node index is recorded in <see cref="TryCatchNodes"/>.</remarks>
    public int TryFinally(int body, int @finally)
    {
        var index = AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, body, @finally);
        TryCatchNodes.Add(index);
        return index;
    }

    /// <summary>Adds a try/fault node.</summary>
    /// <remarks>The node index is recorded in <see cref="TryCatchNodes"/>.</remarks>
    public int TryFault(int body, int fault)
    {
        var index = AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, TryFaultFlag, body, fault);
        TryCatchNodes.Add(index);
        return index;
    }

    /// <summary>Adds a try node with optional finally block and catch handlers.</summary>
    /// <remarks>The node index is recorded in <see cref="TryCatchNodes"/>.</remarks>
    public int TryCatchFinally(int body, int? @finally, params int[] handlers)
    {
        ChildList children = default;
        children.Add(body);
        if (@finally.HasValue)
            children.Add(@finally.Value);
        if (handlers != null && handlers.Length != 0)
        {
            ChildList handlerChildren = default;
            for (var i = 0; i < handlers.Length; ++i)
                handlerChildren.Add(handlers[i]);
            children.Add(AddChildListNode(in handlerChildren));
        }
        var index = AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, in children);
        TryCatchNodes.Add(index);
        return index;
    }

    /// <summary>Adds a type-test node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TypeIs(int expression, Type type) =>
        AddFactoryExpressionNode(typeof(bool), type, ExpressionType.TypeIs, expression);

    /// <summary>Adds a type-equality test node.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int TypeEqual(int expression, Type type) =>
        AddFactoryExpressionNode(typeof(bool), type, ExpressionType.TypeEqual, expression);

    /// <summary>Adds a dynamic-expression node.</summary>
    public int Dynamic(Type delegateType, CallSiteBinder binder, params int[] arguments)
    {
        ChildList children = default;
        children.Add(AddObjectReferenceNode(typeof(Type), delegateType));
        if (arguments != null && arguments.Length != 0)
            for (var i = 0; i < arguments.Length; ++i)
                children.Add(arguments[i]);
        return AddFactoryExpressionNode(typeof(object), binder, ExpressionType.Dynamic, children);
    }

    /// <summary>Adds a runtime-variables node.</summary>
    public int RuntimeVariables(params int[] variables) =>
        AddFactoryExpressionNode(typeof(IRuntimeVariables), null, ExpressionType.RuntimeVariables, variables);

    /// <summary>Adds a debug-info node.</summary>
    public int DebugInfo(string fileName, int startLine, int startColumn, int endLine, int endColumn) =>
        AddFactoryExpressionNode(typeof(void), fileName, ExpressionType.DebugInfo, CreateDebugInfoChildren(startLine, startColumn, endLine, endColumn));

    /// <summary>Flattens a System.Linq expression tree.</summary>
    public static ExprTree FromExpression(SysExpr expression) =>
        new Builder().Build(expression ?? throw new ArgumentNullException(nameof(expression)));

    /// <summary>Flattens a LightExpression tree.</summary>
    public static ExprTree FromLightExpression(LightExpression expression) =>
        FromExpression((expression ?? throw new ArgumentNullException(nameof(expression))).ToExpression());

    /// <summary>Reconstructs the flat tree as a System.Linq expression tree.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077",
        Justification = "Flat expression round-trip stores the runtime type metadata explicitly for reconstruction.")]
    public SysExpr ToExpression() =>
        Nodes.Count != 0
            ? new Reader(this).ReadExpression(RootIndex)
            : throw new InvalidOperationException("Flat expression tree is empty.");

    /// <summary>Reconstructs the flat tree as a LightExpression tree.</summary>
    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    public LightExpression ToLightExpression() => FastExpressionCompiler.LightExpression.FromSysExpressionConverter.ToLightExpression(ToExpression());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, int child) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, CloneChild(child));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int child) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(child));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3, int c4) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3), CloneChild(c4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3, int c4, int c5) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3), CloneChild(c4), CloneChild(c5));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3, int c4, int c5, int c6) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3), CloneChild(c4), CloneChild(c5), CloneChild(c6));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, int[] children)
    {
        if (children != null)
            switch (children.Length)
            {
                case 1: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0]);
                case 2: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1]);
                case 3: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1], children[2]);
                case 4: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1], children[2], children[3]);
                case 5: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1], children[2], children[3], children[4]);
                case 6: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1], children[2], children[3], children[4], children[5]);
                case 7: return AddFactoryExpressionNode(type, obj, nodeType, 0, children[0], children[1], children[2], children[3], children[4], children[5], children[6]);
            }

        var cloned = CloneChildren(children);
        return AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, in cloned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, in cloned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, in cloned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType) =>
        AddLeafNode(type, obj, nodeType, ExprNodeKind.Expression, 0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, in ChildList children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, in children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, int[] children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, int child0, int child1, int child2) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, child0, child1, child2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawLeafExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags = 0, int childIdx = 0, int childCount = 0) =>
        AddLeafNode(type, obj, nodeType, ExprNodeKind.Expression, flags, childIdx, childCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, int child) =>
        AddNode(type, obj, ExpressionType.Extension, kind, flags, CloneChild(child));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, int child) =>
        AddFactoryAuxNode(type, obj, kind, 0, child);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, int child0, int child1) =>
        AddNode(type, obj, ExpressionType.Extension, kind, flags, CloneChild(child0), CloneChild(child1));

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, int[] children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, ExpressionType.Extension, kind, 0, in cloned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, ExpressionType.Extension, kind, flags, in cloned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, in ChildList children) =>
        AddFactoryAuxNode(type, obj, kind, 0, in children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawAuxNode(Type type, object obj, ExprNodeKind kind, in ChildList children) =>
        AddNode(type, obj, ExpressionType.Extension, kind, 0, in children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddRawLeafAuxNode(Type type, object obj, ExprNodeKind kind, byte flags = 0, int childIdx = 0, int childCount = 0) =>
        AddLeafNode(type, obj, ExpressionType.Extension, kind, flags, childIdx, childCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddObjectReferenceNode(Type type, object obj) =>
        AddRawLeafAuxNode(type, obj, ExprNodeKind.ObjectReference);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddChildListNode(in ChildList children) =>
        AddRawAuxNode(null, null, ExprNodeKind.ChildList, in children);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddUInt16PairNode(int first, int second) =>
        AddRawLeafAuxNode(null, null, ExprNodeKind.UInt16Pair, childIdx: checked((ushort)first), childCount: checked((ushort)second));

    private ChildList CreateDebugInfoChildren(int startLine, int startColumn, int endLine, int endColumn)
    {
        ChildList children = default;
        children.Add(AddUInt16PairNode(startLine, startColumn));
        children.Add(AddUInt16PairNode(endLine, endColumn));
        return children;
    }

    private static ChildList PrependToChildList(int first, int[] rest)
    {
        ChildList children = default;
        children.Add(first);
        if (rest != null)
            for (var i = 0; i < rest.Length; ++i)
                children.Add(rest[i]);
        return children;
    }

    /// <summary>Builds the flat representation while preserving parameter and label identity with stack-friendly maps.</summary>
    private struct Builder
    {
        private SmallMap16<object, int, RefEq<object>> _parameterIds;
        private SmallMap16<object, int, RefEq<object>> _labelIds;
        private ExprTree _tree;

        public ExprTree Build(SysExpr expression)
        {
            _tree.RootIndex = AddExpression(expression);
            return _tree;
        }

        private int AddExpression(SysExpr expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    return AddConstant((System.Linq.Expressions.ConstantExpression)expression);
                case ExpressionType.Default:
                    return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType);
                case ExpressionType.Parameter:
                    {
                        var parameter = (SysParameterExpression)expression;
                        return _tree.AddRawLeafExpressionNode(expression.Type, parameter.Name, expression.NodeType,
                            parameter.IsByRef ? ParameterByRefFlag : (byte)0, childIdx: GetId(ref _parameterIds, parameter));
                    }
                case ExpressionType.Lambda:
                    {
                        // Layout: children[0] = body, children[1..n] = parameter decl nodes.
                        // Body is stored before parameters so that the Reader encounters parameter
                        // refs in the body before their decl nodes (out-of-order decl); identity
                        // is preserved via the shared _parametersById id-map.
                        var lambda = (System.Linq.Expressions.LambdaExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(lambda.Body));
                        for (var i = 0; i < lambda.Parameters.Count; ++i)
                            children.Add(AddExpression(lambda.Parameters[i]));
                        var lambdaIndex = _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                        _tree.LambdaNodes.Add(lambdaIndex);
                        CollectLambdaClosureParameterUsages(lambda, lambdaIndex);
                        return lambdaIndex;
                    }
                case ExpressionType.Block:
                    {
                        // Layout (with variables):    children[0] = ChildList(var₀, var₁, …)
                        //                             children[1] = ChildList(expr₀, expr₁, …)
                        // Layout (without variables): children[0] = ChildList(expr₀, expr₁, …)
                        // children.Count == 2 is the canonical test for the presence of variables.
                        var block = (System.Linq.Expressions.BlockExpression)expression;
                        ChildList children = default;
                        var hasVariables = block.Variables.Count != 0;
                        if (hasVariables)
                        {
                            ChildList variables = default;
                            for (var i = 0; i < block.Variables.Count; ++i)
                                variables.Add(AddExpression(block.Variables[i]));
                            children.Add(_tree.AddChildListNode(in variables));
                        }
                        ChildList expressions = default;
                        for (var i = 0; i < block.Expressions.Count; ++i)
                            expressions.Add(AddExpression(block.Expressions[i]));
                        children.Add(_tree.AddChildListNode(in expressions));
                        var blockIndex = _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, in children);
                        if (hasVariables)
                            _tree.BlocksWithVariables.Add(blockIndex);
                        return blockIndex;
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = (System.Linq.Expressions.MemberExpression)expression;
                        ChildList children = default;
                        if (member.Expression != null)
                            children.Add(AddExpression(member.Expression));
                        return _tree.AddRawExpressionNode(expression.Type, member.Member, expression.NodeType,
                            children);
                    }
                case ExpressionType.Call:
                    {
                        var call = (System.Linq.Expressions.MethodCallExpression)expression;
                        ChildList children = default;
                        if (call.Object != null)
                            children.Add(AddExpression(call.Object));
                        for (var i = 0; i < call.Arguments.Count; ++i)
                            children.Add(AddExpression(call.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, call.Method, expression.NodeType, children);
                    }
                case ExpressionType.New:
                    {
                        var @new = (System.Linq.Expressions.NewExpression)expression;
                        ChildList children = default;
                        for (var i = 0; i < @new.Arguments.Count; ++i)
                            children.Add(AddExpression(@new.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, @new.Constructor, expression.NodeType, children);
                    }
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var array = (System.Linq.Expressions.NewArrayExpression)expression;
                        ChildList children = default;
                        for (var i = 0; i < array.Expressions.Count; ++i)
                            children.Add(AddExpression(array.Expressions[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Invoke:
                    {
                        var invoke = (System.Linq.Expressions.InvocationExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(invoke.Expression));
                        for (var i = 0; i < invoke.Arguments.Count; ++i)
                            children.Add(AddExpression(invoke.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Index:
                    {
                        var index = (System.Linq.Expressions.IndexExpression)expression;
                        ChildList children = default;
                        if (index.Object != null)
                            children.Add(AddExpression(index.Object));
                        for (var i = 0; i < index.Arguments.Count; ++i)
                            children.Add(AddExpression(index.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, index.Indexer, expression.NodeType, children);
                    }
                case ExpressionType.Conditional:
                    {
                        var conditional = (System.Linq.Expressions.ConditionalExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(conditional.Test));
                        children.Add(AddExpression(conditional.IfTrue));
                        children.Add(AddExpression(conditional.IfFalse));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType,
                            children[0], children[1], children[2]);
                    }
                case ExpressionType.Loop:
                    {
                        var loop = (System.Linq.Expressions.LoopExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(loop.Body));
                        if (loop.BreakLabel != null)
                            children.Add(AddLabelTarget(loop.BreakLabel));
                        if (loop.ContinueLabel != null)
                            children.Add(AddLabelTarget(loop.ContinueLabel));
                        return _tree.AddNode(expression.Type, null, expression.NodeType, ExprNodeKind.Expression,
                            (byte)((loop.BreakLabel != null ? LoopHasBreakFlag : 0) | (loop.ContinueLabel != null ? LoopHasContinueFlag : 0)), in children);
                    }
                case ExpressionType.Goto:
                    {
                        var @goto = (System.Linq.Expressions.GotoExpression)expression;
                        ChildList children = default;
                        children.Add(AddLabelTarget(@goto.Target));
                        if (@goto.Value != null)
                            children.Add(AddExpression(@goto.Value));
                        var gotoIndex = _tree.AddRawExpressionNode(expression.Type, @goto.Kind, expression.NodeType, children);
                        _tree.GotoNodes.Add(gotoIndex);
                        return gotoIndex;
                    }
                case ExpressionType.Label:
                    {
                        var label = (System.Linq.Expressions.LabelExpression)expression;
                        ChildList children = default;
                        children.Add(AddLabelTarget(label.Target));
                        if (label.DefaultValue != null)
                            children.Add(AddExpression(label.DefaultValue));
                        var labelIndex = _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                        _tree.LabelNodes.Add(labelIndex);
                        return labelIndex;
                    }
                case ExpressionType.Switch:
                    {
                        var @switch = (System.Linq.Expressions.SwitchExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(@switch.SwitchValue));
                        if (@switch.DefaultBody != null)
                            children.Add(AddExpression(@switch.DefaultBody));
                        if (@switch.Cases.Count != 0)
                        {
                            ChildList cases = default;
                            for (var i = 0; i < @switch.Cases.Count; ++i)
                                cases.Add(AddSwitchCase(@switch.Cases[i]));
                            children.Add(_tree.AddChildListNode(in cases));
                        }
                        return _tree.AddRawExpressionNode(expression.Type, @switch.Comparison, expression.NodeType, in children);
                    }
                case ExpressionType.Try:
                    {
                        var @try = (System.Linq.Expressions.TryExpression)expression;
                        ChildList children = default;
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
                            ChildList handlers = default;
                            for (var i = 0; i < @try.Handlers.Count; ++i)
                                handlers.Add(AddCatchBlock(@try.Handlers[i]));
                            children.Add(_tree.AddChildListNode(in handlers));
                        }
                        var tryIndex = _tree.AddNode(expression.Type, null, expression.NodeType, ExprNodeKind.Expression, flags, in children);
                        _tree.TryCatchNodes.Add(tryIndex);
                        return tryIndex;
                    }
                case ExpressionType.MemberInit:
                    {
                        var memberInit = (System.Linq.Expressions.MemberInitExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(memberInit.NewExpression));
                        for (var i = 0; i < memberInit.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberInit.Bindings[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.ListInit:
                    {
                        var listInit = (System.Linq.Expressions.ListInitExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(listInit.NewExpression));
                        for (var i = 0; i < listInit.Initializers.Count; ++i)
                            children.Add(AddElementInit(listInit.Initializers[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    {
                        var typeBinary = (System.Linq.Expressions.TypeBinaryExpression)expression;
                        ChildList children = default;
                        children.Add(AddExpression(typeBinary.Expression));
                        return _tree.AddRawExpressionNode(expression.Type, typeBinary.TypeOperand, expression.NodeType,
                            children);
                    }
                case ExpressionType.Dynamic:
                    {
                        var dynamic = (System.Linq.Expressions.DynamicExpression)expression;
                        ChildList children = default;
                        children.Add(_tree.AddObjectReferenceNode(typeof(Type), dynamic.DelegateType));
                        for (var i = 0; i < dynamic.Arguments.Count; ++i)
                            children.Add(AddExpression(dynamic.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, dynamic.Binder, expression.NodeType, children);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var runtime = (System.Linq.Expressions.RuntimeVariablesExpression)expression;
                        ChildList children = default;
                        for (var i = 0; i < runtime.Variables.Count; ++i)
                            children.Add(AddExpression(runtime.Variables[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var debug = (System.Linq.Expressions.DebugInfoExpression)expression;
                        return _tree.AddFactoryExpressionNode(expression.Type, debug.Document.FileName, expression.NodeType,
                            _tree.CreateDebugInfoChildren(debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn));
                    }
                default:
                    if (expression is System.Linq.Expressions.UnaryExpression unary)
                    {
                        ChildList children = default;
                        children.Add(AddExpression(unary.Operand));
                        return _tree.AddRawExpressionNode(expression.Type, unary.Method, expression.NodeType,
                            children);
                    }

                    if (expression is System.Linq.Expressions.BinaryExpression binary)
                    {
                        ChildList children = default;
                        children.Add(AddExpression(binary.Left));
                        children.Add(AddExpression(binary.Right));
                        if (binary.Conversion != null)
                            children.Add(AddExpression(binary.Conversion));
                        return _tree.AddNode(expression.Type, binary.Method, expression.NodeType, ExprNodeKind.Expression,
                            binary.IsLiftedToNull ? BinaryLiftedToNullFlag : (byte)0, in children);
                    }

                    throw new NotSupportedException($"Flattening of `ExpressionType.{expression.NodeType}` is not supported yet.");
            }
        }

        private void CollectLambdaClosureParameterUsages(System.Linq.Expressions.LambdaExpression lambda, int lambdaNodeIndex)
        {
            var collector = new LambdaClosureUsageCollector(lambda);
            collector.Visit(lambda.Body);

            var captured = collector.CapturedParameters;
            for (var i = 0; i < captured.Count; ++i)
                _tree.LambdaClosureParameterUsages.Add(new LambdaClosureParameterUsage(
                    checked((short)lambdaNodeIndex),
                    checked((short)GetId(ref _parameterIds, captured[i]))));
        }

        private sealed class LambdaClosureUsageCollector : System.Linq.Expressions.ExpressionVisitor
        {
            private readonly System.Linq.Expressions.LambdaExpression _lambda;
            private readonly List<SysParameterExpression> _scopedParameters = new();
            private readonly HashSet<SysParameterExpression> _scopedParameterSet = new(ReferenceParameterComparer.Instance);
            private readonly HashSet<SysParameterExpression> _capturedParameterSet = new(ReferenceParameterComparer.Instance);

            public readonly List<SysParameterExpression> CapturedParameters = new();

            public LambdaClosureUsageCollector(System.Linq.Expressions.LambdaExpression lambda)
            {
                _lambda = lambda;
                for (var i = 0; i < lambda.Parameters.Count; ++i)
                {
                    var parameter = lambda.Parameters[i];
                    _scopedParameters.Add(parameter);
                    _scopedParameterSet.Add(parameter);
                }
            }

            protected override Expression VisitLambda<T>(System.Linq.Expressions.Expression<T> node) =>
                // Intentionally skip nested lambdas: each lambda closure map is collected independently
                // when that lambda node is visited by the parent Builder traversal.
                ReferenceEquals(node, _lambda) ? base.VisitLambda(node) : node;

            protected override Expression VisitParameter(SysParameterExpression node)
            {
                if (!_scopedParameterSet.Contains(node) && _capturedParameterSet.Add(node))
                    CapturedParameters.Add(node);
                return node;
            }

            protected override Expression VisitBlock(System.Linq.Expressions.BlockExpression node)
            {
                var initialScopeCount = _scopedParameters.Count;
                for (var i = 0; i < node.Variables.Count; ++i)
                {
                    var variable = node.Variables[i];
                    _scopedParameters.Add(variable);
                    _scopedParameterSet.Add(variable);
                }
                var result = base.VisitBlock(node);
                for (var i = _scopedParameters.Count - 1; i >= initialScopeCount; --i)
                    _scopedParameterSet.Remove(_scopedParameters[i]);
                _scopedParameters.RemoveRange(initialScopeCount, _scopedParameters.Count - initialScopeCount);
                return result;
            }

            protected override SysCatchBlock VisitCatchBlock(SysCatchBlock node)
            {
                var initialScopeCount = _scopedParameters.Count;
                if (node.Variable != null)
                {
                    _scopedParameters.Add(node.Variable);
                    _scopedParameterSet.Add(node.Variable);
                }
                var result = base.VisitCatchBlock(node);
                for (var i = _scopedParameters.Count - 1; i >= initialScopeCount; --i)
                    _scopedParameterSet.Remove(_scopedParameters[i]);
                _scopedParameters.RemoveRange(initialScopeCount, _scopedParameters.Count - initialScopeCount);
                return result;
            }

            private sealed class ReferenceParameterComparer : IEqualityComparer<SysParameterExpression>
            {
                public static readonly ReferenceParameterComparer Instance = new();

                public bool Equals(SysParameterExpression x, SysParameterExpression y) => ReferenceEquals(x, y);

                public int GetHashCode(SysParameterExpression obj) => RuntimeHelpers.GetHashCode(obj);
            }
        }

        private int AddConstant(System.Linq.Expressions.ConstantExpression constant) =>
            _tree.Constant(constant.Value, constant.Type);

        private int AddSwitchCase(SysSwitchCase switchCase)
        {
            ChildList children = default;
            for (var i = 0; i < switchCase.TestValues.Count; ++i)
                children.Add(AddExpression(switchCase.TestValues[i]));
            children.Add(AddExpression(switchCase.Body));
            return _tree.AddRawAuxNode(switchCase.Body.Type, null, ExprNodeKind.SwitchCase, children);
        }

        private int AddCatchBlock(SysCatchBlock catchBlock)
        {
            ChildList children = default;
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
                    ChildList assignmentChildren = default;
                    assignmentChildren.Add(AddExpression(((System.Linq.Expressions.MemberAssignment)binding).Expression));
                    return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberAssignment,
                        assignmentChildren);
                case MemberBindingType.MemberBinding:
                    {
                        var memberBinding = (System.Linq.Expressions.MemberMemberBinding)binding;
                        ChildList children = default;
                        for (var i = 0; i < memberBinding.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberBinding.Bindings[i]));
                        return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberMemberBinding, children);
                    }
                case MemberBindingType.ListBinding:
                    {
                        var listBinding = (System.Linq.Expressions.MemberListBinding)binding;
                        ChildList children = default;
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
            ChildList children = default;
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

        private static Type GetMemberType(System.Reflection.MemberInfo member) => member switch
        {
            System.Reflection.FieldInfo field => field.FieldType,
            System.Reflection.PropertyInfo property => property.PropertyType,
            _ => typeof(object)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddLeafNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int childIdx, int childCount)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, childIdx, childCount);
        return nodeIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int AddInlineConstantNode(Type type, uint inlineValue)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, inlineValue);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int child0)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, child0, 1);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 2);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 3);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2, int c3)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 4);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        Nodes.GetSurePresentRef(c2).SetNextIdx(c3);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2, int c3, int c4)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 5);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        Nodes.GetSurePresentRef(c2).SetNextIdx(c3);
        Nodes.GetSurePresentRef(c3).SetNextIdx(c4);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2, int c3, int c4, int c5)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 6);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        Nodes.GetSurePresentRef(c2).SetNextIdx(c3);
        Nodes.GetSurePresentRef(c3).SetNextIdx(c4);
        Nodes.GetSurePresentRef(c4).SetNextIdx(c5);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2, int c3, int c4, int c5, int c6)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 7);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        Nodes.GetSurePresentRef(c2).SetNextIdx(c3);
        Nodes.GetSurePresentRef(c3).SetNextIdx(c4);
        Nodes.GetSurePresentRef(c4).SetNextIdx(c5);
        Nodes.GetSurePresentRef(c5).SetNextIdx(c6);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int[] children)
    {
        if (children == null || children.Length == 0)
            return AddNode(type, obj, nodeType, kind, flags);

        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, children[0], children.Length);
        for (var i = 1; i < children.Length; ++i)
            Nodes.GetSurePresentRef(children[i - 1]).SetNextIdx(children[i]);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, in ChildList children)
    {
        if (children.Count == 0)
            return AddNode(type, obj, nodeType, kind, flags);

        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, children[0], children.Count);
        for (var i = 1; i < children.Count; ++i)
            Nodes.GetSurePresentRef(children[i - 1]).SetNextIdx(children[i]);
        return nodeIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSmallPrimitive(TypeCode tc) =>
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

    private static Type GetMemberType(System.Reflection.MemberInfo member) => member switch
    {
        System.Reflection.FieldInfo field => field.FieldType,
        System.Reflection.PropertyInfo property => property.PropertyType,
        _ => typeof(object)
    };

    private static Type GetUnaryResultType(ExpressionType nodeType, Type operandType, System.Reflection.MethodInfo method) =>
        nodeType switch
        {
            ExpressionType.IsFalse or ExpressionType.IsTrue or ExpressionType.TypeIs or ExpressionType.TypeEqual => typeof(bool),
            _ => method?.ReturnType ?? operandType
        };

    private static Type GetBinaryResultType(ExpressionType nodeType, Type leftType, Type rightType, System.Reflection.MethodInfo method)
    {
        if (method != null)
            return method.ReturnType;

        return nodeType switch
        {
            ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
                or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.AndAlso or ExpressionType.OrElse => typeof(bool),
            ExpressionType.ArrayIndex => leftType.GetElementType(),
            ExpressionType.Assign => leftType,
            _ => leftType
        };
    }

    private static Type GetArrayElementType(Type arrayType, int depth)
    {
        var elementType = arrayType;
        for (var i = 0; i < depth; ++i)
            elementType = elementType.GetElementType();
        return elementType ?? typeof(object);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CloneChild(int index)
    {
        ref var node = ref Nodes[index];
        if (!node.ShouldCloneWhenLinked()) return index;
        if (ReferenceEquals(node.Obj, ExprNode.InlineValueMarker))
            return AddInlineConstantNode(node.Type, node.InlineValue);
        return AddLeafNode(node.Type, node.Obj, node.NodeType, node.Kind, node.Flags, node.ChildIdx, node.ChildCount);
    }

    private ChildList CloneChildren(int[] children)
    {
        ChildList cloned = default;
        if (children == null)
            return cloned;

        for (var i = 0; i < children.Length; ++i)
            cloned.Add(CloneChild(children[i]));
        return cloned;
    }

    private ChildList CloneChildren(in ChildList children)
    {
        ChildList cloned = default;
        for (var i = 0; i < children.Count; ++i)
            cloned.Add(CloneChild(children[i]));
        return cloned;
    }

    /// <summary>Reconstructs System.Linq nodes from the flat representation while reusing parameter and label identities.</summary>
    private struct Reader
    {
        private readonly ExprTree _tree;
        private SmallMap16<int, SysParameterExpression, IntEq> _parametersById;
        private SmallMap16<int, SysLabelTarget, IntEq> _labelsById;

        public Reader(ExprTree tree)
        {
            _tree = tree;
            _parametersById = default;
            _labelsById = default;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        public SysExpr ReadExpression(int index)
        {
            ref var node = ref _tree.Nodes[index];
            if (!node.IsExpression())
                throw new InvalidOperationException($"Node at index {index} is not an expression node.");

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
                        var children = GetChildren(index);
                        var body = ReadExpression(children[0]);
                        var parameters = new SysParameterExpression[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            parameters[i - 1] = (SysParameterExpression)ReadExpression(children[i]);
                        return SysExpr.Lambda(node.Type, body, parameters);
                    }
                case ExpressionType.Block:
                    {
                        // Layout (with variables):    children[0] = ChildList(var₀, var₁, …)
                        //                             children[1] = ChildList(expr₀, expr₁, …)
                        // Layout (without variables): children[0] = ChildList(expr₀, expr₁, …)
                        // children.Count == 2 is the canonical test for the presence of variables.
                        // Variable decl nodes in children[0] are registered in _parametersById before
                        // the body expressions in children[1] are read, so refs in the body resolve
                        // to the same SysParameterExpression object as the decl (normal order here).
                        var children = GetChildren(index);
                        var hasVariables = children.Count == 2;
                        var variableIndexes = hasVariables ? GetChildren(children[0]) : default;
                        var expressionIndexes = GetChildren(children[children.Count - 1]);
                        var variables = new SysParameterExpression[variableIndexes.Count];
                        for (var i = 0; i < variables.Length; ++i)
                            variables[i] = (SysParameterExpression)ReadExpression(variableIndexes[i]);
                        var expressions = new SysExpr[expressionIndexes.Count];
                        for (var i = 0; i < expressions.Length; ++i)
                            expressions[i] = ReadExpression(expressionIndexes[i]);
                        return SysExpr.Block(node.Type, variables, expressions);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var children = GetChildren(index);
                        return SysExpr.MakeMemberAccess(children.Count != 0 ? ReadExpression(children[0]) : null, (System.Reflection.MemberInfo)node.Obj);
                    }
                case ExpressionType.Call:
                    {
                        var method = (System.Reflection.MethodInfo)node.Obj;
                        var children = GetChildren(index);
                        var hasInstance = !method.IsStatic;
                        var instance = hasInstance ? ReadExpression(children[0]) : null;
                        var arguments = new SysExpr[children.Count - (hasInstance ? 1 : 0)];
                        for (var i = hasInstance ? 1 : 0; i < children.Count; ++i)
                            arguments[i - (hasInstance ? 1 : 0)] = ReadExpression(children[i]);
                        return SysExpr.Call(instance, method, arguments);
                    }
                case ExpressionType.New:
                    {
                        var children = GetChildren(index);
                        var arguments = ReadExpressions(children);
                        return node.Obj is System.Reflection.ConstructorInfo ctor
                            ? SysExpr.New(ctor, arguments)
                            : CreateValueTypeNewExpression(node.Type);
                    }
                case ExpressionType.NewArrayInit:
                    return SysExpr.NewArrayInit(node.Type.GetElementType(), ReadExpressions(GetChildren(index)));
                case ExpressionType.NewArrayBounds:
                    return SysExpr.NewArrayBounds(node.Type.GetElementType(), ReadExpressions(GetChildren(index)));
                case ExpressionType.Invoke:
                    {
                        var children = GetChildren(index);
                        var arguments = new SysExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children[i]);
                        return SysExpr.Invoke(ReadExpression(children[0]), arguments);
                    }
                case ExpressionType.Index:
                    {
                        var children = GetChildren(index);
                        var property = (System.Reflection.PropertyInfo)node.Obj;
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
                        var children = GetChildren(index);
                        return SysExpr.Condition(ReadExpression(children[0]), ReadExpression(children[1]), ReadExpression(children[2]), node.Type);
                    }
                case ExpressionType.Loop:
                    {
                        var children = GetChildren(index);
                        var childIndex = 1;
                        var breakLabel = node.HasFlag(LoopHasBreakFlag) ? ReadLabelTarget(children[childIndex++]) : null;
                        var continueLabel = node.HasFlag(LoopHasContinueFlag) ? ReadLabelTarget(children[childIndex]) : null;
                        return SysExpr.Loop(ReadExpression(children[0]), breakLabel, continueLabel);
                    }
                case ExpressionType.Goto:
                    {
                        var children = GetChildren(index);
                        var value = children.Count > 1 ? ReadExpression(children[1]) : null;
                        return SysExpr.MakeGoto((GotoExpressionKind)node.Obj, ReadLabelTarget(children[0]), value, node.Type);
                    }
                case ExpressionType.Label:
                    {
                        var children = GetChildren(index);
                        var defaultValue = children.Count > 1 ? ReadExpression(children[1]) : null;
                        return SysExpr.Label(ReadLabelTarget(children[0]), defaultValue);
                    }
                case ExpressionType.Switch:
                    {
                        var children = GetChildren(index);
                        var defaultBody = default(SysExpr);
                        ChildList caseIndexes = default;
                        if (children.Count > 1)
                        {
                            ref var lastChild = ref _tree.Nodes[children[children.Count - 1]];
                            if (lastChild.Is(ExprNodeKind.ChildList))
                            {
                                caseIndexes = GetChildren(children[children.Count - 1]);
                                if (children.Count == 3)
                                    defaultBody = ReadExpression(children[1]);
                            }
                            else
                                defaultBody = ReadExpression(children[1]);
                        }
                        var cases = new SysSwitchCase[caseIndexes.Count];
                        for (var i = 0; i < cases.Length; ++i)
                            cases[i] = ReadSwitchCase(caseIndexes[i]);
                        return SysExpr.Switch(node.Type, ReadExpression(children[0]), defaultBody, (System.Reflection.MethodInfo)node.Obj, cases);
                    }
                case ExpressionType.Try:
                    {
                        var children = GetChildren(index);
                        if (node.HasFlag(TryFaultFlag))
                            return SysExpr.TryFault(ReadExpression(children[0]), ReadExpression(children[1]));

                        var handlers = default(SysCatchBlock[]);
                        var lastChildIsHandlerList = children.Count > 1 && _tree.Nodes[children[children.Count - 1]].Is(ExprNodeKind.ChildList);
                        if (lastChildIsHandlerList)
                        {
                            var handlerIndexes = GetChildren(children[children.Count - 1]);
                            handlers = new SysCatchBlock[handlerIndexes.Count];
                            for (var i = 0; i < handlers.Length; ++i)
                                handlers[i] = ReadCatchBlock(handlerIndexes[i]);
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
                        var children = GetChildren(index);
                        var bindings = new SysMemberBinding[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            bindings[i - 1] = ReadMemberBinding(children[i]);
                        return SysExpr.MemberInit((System.Linq.Expressions.NewExpression)ReadExpression(children[0]), bindings);
                    }
                case ExpressionType.ListInit:
                    {
                        var children = GetChildren(index);
                        var initializers = new SysElementInit[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            initializers[i - 1] = ReadElementInit(children[i]);
                        return SysExpr.ListInit((System.Linq.Expressions.NewExpression)ReadExpression(children[0]), initializers);
                    }
                case ExpressionType.TypeIs:
                    return SysExpr.TypeIs(ReadExpression(GetChildren(index)[0]), (Type)node.Obj);
                case ExpressionType.TypeEqual:
                    return SysExpr.TypeEqual(ReadExpression(GetChildren(index)[0]), (Type)node.Obj);
                case ExpressionType.Dynamic:
                    {
                        var children = GetChildren(index);
                        var delegateType = (Type)ReadObjectReference(children[0]);
                        var arguments = new SysExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children[i]);
                        return SysExpr.MakeDynamic(delegateType, (CallSiteBinder)node.Obj, arguments);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var children = GetChildren(index);
                        var variables = new SysParameterExpression[children.Count];
                        for (var i = 0; i < children.Count; ++i)
                            variables[i] = (SysParameterExpression)ReadExpression(children[i]);
                        return SysExpr.RuntimeVariables(variables);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var children = GetChildren(index);
                        ReadUInt16Pair(children[0], out var startLine, out var startColumn);
                        ReadUInt16Pair(children[1], out var endLine, out var endColumn);
                        return SysExpr.DebugInfo(SysExpr.SymbolDocument((string)node.Obj), startLine, startColumn, endLine, endColumn);
                    }
                default:
                    if (node.ChildCount == 1)
                    {
                        var method = node.Obj as System.Reflection.MethodInfo;
                        return SysExpr.MakeUnary(node.NodeType, ReadExpression(GetChildren(index)[0]), node.Type, method);
                    }

                    if (node.ChildCount >= 2)
                    {
                        var children = GetChildren(index);
                        var conversion = children.Count > 2 ? (System.Linq.Expressions.LambdaExpression)ReadExpression(children[2]) : null;
                        return SysExpr.MakeBinary(node.NodeType, ReadExpression(children[0]), ReadExpression(children[1]),
                            node.HasFlag(BinaryLiftedToNullFlag), (System.Reflection.MethodInfo)node.Obj, conversion);
                    }

                    throw new NotSupportedException($"Reconstruction of `ExpressionType.{node.NodeType}` is not supported yet.");
            }
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysSwitchCase ReadSwitchCase(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.SwitchCase));
            var children = GetChildren(index);
            var testValues = new SysExpr[children.Count - 1];
            for (var i = 0; i < testValues.Length; ++i)
                testValues[i] = ReadExpression(children[i]);
            return SysExpr.SwitchCase(ReadExpression(children[children.Count - 1]), testValues);
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysCatchBlock ReadCatchBlock(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.CatchBlock));
            var children = GetChildren(index);
            var childIndex = 0;
            var variable = node.HasFlag(CatchHasVariableFlag) ? (SysParameterExpression)ReadExpression(children[childIndex++]) : null;
            var body = ReadExpression(children[childIndex++]);
            var filter = node.HasFlag(CatchHasFilterFlag) ? ReadExpression(children[childIndex]) : null;
            return SysExpr.MakeCatchBlock(node.Type, variable, body, filter);
        }

        private SysLabelTarget ReadLabelTarget(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.LabelTarget));
            ref var label = ref _labelsById.Map.AddOrGetValueRef(node.ChildIdx, out var found);
            if (found)
                return label;

            return label = SysExpr.Label(node.Type, (string)node.Obj);
        }

        private object ReadObjectReference(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.ObjectReference));
            return node.Obj;
        }

        private void ReadUInt16Pair(int index, out int first, out int second)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.UInt16Pair));
            first = node.ChildIdx;
            second = node.ChildCount;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysMemberBinding ReadMemberBinding(int index)
        {
            ref var node = ref _tree.Nodes[index];
            var member = (System.Reflection.MemberInfo)node.Obj;
            switch (node.Kind)
            {
                case ExprNodeKind.MemberAssignment:
                    return SysExpr.Bind(member, ReadExpression(GetChildren(index)[0]));
                case ExprNodeKind.MemberMemberBinding:
                    {
                        var childIndexes = GetChildren(index);
                        var bindings = new SysMemberBinding[childIndexes.Count];
                        for (var i = 0; i < childIndexes.Count; ++i)
                            bindings[i] = ReadMemberBinding(childIndexes[i]);
                        return SysExpr.MemberBind(member, bindings);
                    }
                case ExprNodeKind.MemberListBinding:
                    {
                        var childIndexes = GetChildren(index);
                        var initializers = new SysElementInit[childIndexes.Count];
                        for (var i = 0; i < childIndexes.Count; ++i)
                            initializers[i] = ReadElementInit(childIndexes[i]);
                        return SysExpr.ListBind(member, initializers);
                    }
                default:
                    throw new InvalidOperationException($"Node at index {index} is not a member binding node.");
            }
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysElementInit ReadElementInit(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Is(ExprNodeKind.ElementInit));
            return SysExpr.ElementInit((System.Reflection.MethodInfo)node.Obj, ReadExpressions(GetChildren(index)));
        }

        private ChildList GetChildren(int index)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(index);
            var count = node.ChildCount;
            ChildList children = default;
            var childIndex = node.ChildIdx;
            for (var i = 0; i < count; ++i)
            {
                children.Add(childIndex);
                childIndex = _tree.Nodes.GetSurePresentRef(childIndex).NextIdx;
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
        private SysExpr[] ReadExpressions(in ChildList childIndexes)
        {
            var expressions = new SysExpr[childIndexes.Count];
            for (var i = 0; i < expressions.Length; ++i)
                expressions[i] = ReadExpression(childIndexes[i]);
            return expressions;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077",
            Justification = "Flat expression round-trip stores the runtime type metadata explicitly for reconstruction.")]
        private static System.Linq.Expressions.NewExpression CreateValueTypeNewExpression(Type type) => SysExpr.New(type);
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
    public static ExprTree ToFlatExpression(this SysExpr expression) => ExprTree.FromExpression(expression);

    /// <summary>Flattens a LightExpression tree.</summary>
    public static ExprTree ToFlatExpression(this LightExpression expression) => ExprTree.FromLightExpression(expression);
}
