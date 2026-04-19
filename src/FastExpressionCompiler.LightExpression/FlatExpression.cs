#nullable disable
#pragma warning disable CS1591

namespace FastExpressionCompiler.LightExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using FastExpressionCompiler.LightExpression.ImTools;

using LE = FastExpressionCompiler.LightExpression;

/// <summary>
/// A single node in the flat expression tree. Packs all node metadata into a 64-bit
/// <c>_data</c> field and two object references (<c>Type</c>, <c>Obj</c>).
/// <para>
/// <c>_data</c> layout (high-to-low bits):
/// <list type="bullet">
///   <item>bits 63–56: <see cref="ExpressionType"/> (8 bits)</item>
///   <item>bits 55–48: Extra – node-type-specific auxiliary byte (8 bits)</item>
///   <item>bits 47–32: unused / reserved (16 bits)</item>
///   <item>bits 31–16: ChildCount – number of children (16 bits)</item>
///   <item>bits 15– 0: FirstChildPoolIdx – offset into <see cref="ExprTree"/> child pool (16 bits)</item>
/// </list>
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ExprNode
{
    /// <summary>Sentinel value meaning "no index".</summary>
    public const ushort NoIdx = 0xFFFF;

    /// <summary>The CLR type of the expression result.</summary>
    public Type Type;

    /// <summary>
    /// Node-type-specific payload:
    /// <list type="bullet">
    ///   <item>Constant  – the constant value (boxed)</item>
    ///   <item>Parameter – the parameter name (<c>string</c>, may be <c>null</c>)</item>
    ///   <item>New       – <see cref="ConstructorInfo"/> (null for struct default-ctor)</item>
    ///   <item>Call      – <see cref="MethodInfo"/></item>
    ///   <item>Field     – <see cref="FieldInfo"/></item>
    ///   <item>Property (MemberAccess) – <see cref="PropertyInfo"/></item>
    ///   <item>Index     – <see cref="PropertyInfo"/> (null for array element access)</item>
    ///   <item>Binary    – <see cref="MethodInfo"/> or null</item>
    ///   <item>Unary     – <see cref="MethodInfo"/> or null</item>
    ///   <item>Conditional – explicit result <see cref="Type"/> or null (derive from IfTrue)</item>
    ///   <item>Loop      – <c>LabelTarget[]</c> {breakLabel, continueLabel}, either may be null</item>
    ///   <item>Try       – <see cref="CatchBlock"/>[] (may be null)</item>
    ///   <item>Label     – <see cref="LabelTarget"/></item>
    ///   <item>Goto      – <see cref="LabelTarget"/></item>
    ///   <item>Switch    – <c>object[]</c> {<see cref="SwitchCase"/>[], <see cref="MethodInfo"/> comparison (may be null)}</item>
    ///   <item>TypeBinary – the operand <see cref="Type"/> to test against</item>
    ///   <item>MemberInit – <see cref="MemberBinding"/>[]</item>
    ///   <item>ListInit  – <see cref="ElementInit"/>[]</item>
    ///   <item>NewArrayInit/NewArrayBounds – <c>null</c> (element type is derived from <c>Type</c>)</item>
    /// </list>
    /// </summary>
    public object Obj;

    private ulong _data;

    /// <summary>The <see cref="System.Linq.Expressions.ExpressionType"/> of this node.</summary>
    public ExpressionType NodeType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ExpressionType)((_data >> 56) & 0xFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _data = (_data & ~(0xFFUL << 56)) | ((ulong)(byte)value << 56);
    }

    /// <summary>
    /// Node-type-specific auxiliary byte:
    /// <list type="bullet">
    ///   <item>Parameter  – 1 if by-ref, 0 otherwise</item>
    ///   <item>Lambda     – number of parameters (the body is the last child)</item>
    ///   <item>Block      – number of variables (the rest of children are expressions)</item>
    ///   <item>Goto       – <see cref="GotoExpressionKind"/> value</item>
    ///   <item>Try        – variant: 0=Catch, 1=Finally, 2=CatchFinally, 3=Fault</item>
    ///   <item>Call       – 1 if first child is the instance, 0 if fully static</item>
    ///   <item>MemberAccess – 1 if first child is the instance, 0 if static</item>
    ///   <item>Index      – 1 if first child is the instance (indexer or array), always 1 for this node type</item>
    /// </list>
    /// </summary>
    internal byte Extra
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)((_data >> 48) & 0xFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _data = (_data & ~(0xFFUL << 48)) | ((ulong)value << 48);
    }

    /// <summary>Number of child nodes.</summary>
    public ushort ChildCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)((_data >> 16) & 0xFFFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _data = (_data & ~(0xFFFFUL << 16)) | ((ulong)value << 16);
    }

    /// <summary>
    /// Offset into <see cref="ExprTree.ChildPool"/> where the children of this node start.
    /// <see cref="NoIdx"/> means no children.
    /// </summary>
    public ushort FirstChildPoolIdx
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (ushort)(_data & 0xFFFF);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _data = (_data & ~0xFFFFUL) | (ulong)value;
    }

    internal void SetData(ExpressionType nodeType, byte extra, ushort childCount, ushort firstChildPoolIdx)
    {
        _data = ((ulong)(byte)nodeType << 56) |
                ((ulong)extra << 48) |
                ((ulong)childCount << 16) |
                (ulong)firstChildPoolIdx;
    }
}

/// <summary>
/// Data-oriented, flat representation of an expression tree.
/// <para>
/// Small trees (up to 16 nodes, 16 child-pool slots) stay fully on the stack.
/// Nodes are appended to <see cref="Nodes"/>; child indices are appended to <see cref="ChildPool"/>
/// using a non-intrusive approach that allows the same node to appear as a child of multiple parents.
/// </para>
/// <para>
/// Factory methods return a <see cref="ushort"/> node index.
/// Call <see cref="ToLightExpression(ushort)"/> to convert back to a LightExpression for compilation.
/// </para>
/// </summary>
public struct ExprTree
{
    /// <summary>All expression nodes; first 16 slots live on the stack.</summary>
    public SmallList<ExprNode, Stack16<ExprNode>, NoArrayPool<ExprNode>> Nodes;

    /// <summary>
    /// Non-intrusive child-index pool.  Each parent node holds a contiguous slice
    /// <c>[FirstChildPoolIdx .. FirstChildPoolIdx + ChildCount - 1]</c>.
    /// First 16 slots live on the stack.
    /// </summary>
    public SmallList<ushort, Stack16<ushort>, NoArrayPool<ushort>> ChildPool;

    // ── private helpers ───────────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort AddNode(ExpressionType nodeType, Type type, object obj,
        byte extra, ushort childCount, ushort firstChildPoolIdx)
    {
        var node = new ExprNode { Type = type, Obj = obj };
        node.SetData(nodeType, extra, childCount, firstChildPoolIdx);
        return (ushort)Nodes.Add(in node);
    }

    /// <summary>Appends <paramref name="children"/> to the child pool and returns the first pool index.</summary>
    private ushort AppendChildren(ushort[] children)
    {
        var start = (ushort)ChildPool.Count;
        foreach (var c in children)
            ChildPool.Add(c);
        return start;
    }

    private ushort AddNodeWithChildren(ExpressionType nodeType, Type type, object obj, byte extra, ushort[] children)
    {
        ushort poolStart = ExprNode.NoIdx;
        if (children != null && children.Length > 0)
            poolStart = AppendChildren(children);
        return AddNode(nodeType, type, obj, extra, (ushort)(children?.Length ?? 0), poolStart);
    }

    // ── Constant ──────────────────────────────────────────────────────────────

    /// <summary>Creates a Constant node.</summary>
    public ushort Constant(object value, Type type = null)
    {
        if (value == null)
            return AddNode(ExpressionType.Constant, type ?? typeof(object), null, 0, 0, ExprNode.NoIdx);
        type ??= value.GetType();
        return AddNode(ExpressionType.Constant, type, value, 0, 0, ExprNode.NoIdx);
    }

    /// <summary>Creates a Constant node with a strongly typed value.</summary>
    public ushort Constant<T>(T value) =>
        AddNode(ExpressionType.Constant, typeof(T), value, 0, 0, ExprNode.NoIdx);

    // ── Parameter / Variable ──────────────────────────────────────────────────

    /// <summary>Creates a Parameter/Variable node.</summary>
    public ushort Parameter(Type type, string name = null)
    {
        if (type.IsByRef)
            return AddNode(ExpressionType.Parameter, type.GetElementType(), name, extra: 1, 0, ExprNode.NoIdx);
        return AddNode(ExpressionType.Parameter, type, name, 0, 0, ExprNode.NoIdx);
    }

    /// <summary>Creates a strongly-typed Parameter/Variable node.</summary>
    public ushort Parameter<T>(string name = null) =>
        AddNode(ExpressionType.Parameter, typeof(T), name, 0, 0, ExprNode.NoIdx);

    /// <summary>Alias for <see cref="Parameter(Type, string)"/>.</summary>
    public ushort Variable(Type type, string name = null) => Parameter(type, name);

    /// <summary>Alias for <see cref="Parameter{T}(string)"/>.</summary>
    public ushort Variable<T>(string name = null) => Parameter<T>(name);

    // ── New ───────────────────────────────────────────────────────────────────

    /// <summary>Creates a New node for a value type default constructor.</summary>
    public ushort New(Type valueType) =>
        AddNode(ExpressionType.New, valueType, null, 0, 0, ExprNode.NoIdx);

    /// <summary>Creates a New node with no arguments.</summary>
    public ushort New(ConstructorInfo ctor) =>
        AddNode(ExpressionType.New, ctor.DeclaringType, ctor, 0, 0, ExprNode.NoIdx);

    /// <summary>Creates a New node with arguments.</summary>
    public ushort New(ConstructorInfo ctor, params ushort[] args) =>
        AddNodeWithChildren(ExpressionType.New, ctor.DeclaringType, ctor, 0, args);

    // ── Call ──────────────────────────────────────────────────────────────────

    /// <summary>Creates a static method call node.</summary>
    public ushort Call(MethodInfo method, params ushort[] args) =>
        AddNodeWithChildren(ExpressionType.Call, method.ReturnType, method, extra: 0, args);

    /// <summary>Creates an instance method call node; <paramref name="instance"/> is the first child.</summary>
    public ushort Call(ushort instance, MethodInfo method, params ushort[] args)
    {
        var allChildren = new ushort[1 + (args?.Length ?? 0)];
        allChildren[0] = instance;
        if (args != null)
            for (var i = 0; i < args.Length; i++)
                allChildren[i + 1] = args[i];
        return AddNodeWithChildren(ExpressionType.Call, method.ReturnType, method, extra: 1, allChildren);
    }

    // ── Field ─────────────────────────────────────────────────────────────────

    /// <summary>Creates a static field access node.</summary>
    public ushort Field(FieldInfo field) =>
        AddNode(ExpressionType.MemberAccess, field.FieldType, field, 0, 0, ExprNode.NoIdx);

    /// <summary>Creates an instance field access node.</summary>
    public ushort Field(ushort instance, FieldInfo field) =>
        AddNodeWithChildren(ExpressionType.MemberAccess, field.FieldType, field, extra: 1, new[] { instance });

    // ── Property ─────────────────────────────────────────────────────────────

    /// <summary>Creates a static property access node.</summary>
    public ushort Property(PropertyInfo property) =>
        AddNode(ExpressionType.MemberAccess, property.PropertyType, property, 0, 0, ExprNode.NoIdx);

    /// <summary>Creates an instance property access node.</summary>
    public ushort Property(ushort instance, PropertyInfo property) =>
        AddNodeWithChildren(ExpressionType.MemberAccess, property.PropertyType, property, extra: 1, new[] { instance });

    // ── Index (array access / indexer) ────────────────────────────────────────

    /// <summary>Creates an array element access node.</summary>
    public ushort ArrayAccess(ushort array, params ushort[] indexes)
    {
        var elemType = Nodes.GetSurePresentRef(array).Type.GetElementType();
        var all = new ushort[1 + indexes.Length];
        all[0] = array;
        for (var i = 0; i < indexes.Length; i++) all[i + 1] = indexes[i];
        return AddNodeWithChildren(ExpressionType.Index, elemType, null, extra: 1, all);
    }

    /// <summary>Creates a property indexer node.</summary>
    public ushort Index(ushort instance, PropertyInfo indexer, params ushort[] args)
    {
        var all = new ushort[1 + args.Length];
        all[0] = instance;
        for (var i = 0; i < args.Length; i++) all[i + 1] = args[i];
        return AddNodeWithChildren(ExpressionType.Index, indexer.PropertyType, indexer, extra: 0, all);
    }

    // ── Binary ────────────────────────────────────────────────────────────────

    /// <summary>Creates a binary expression node.  <paramref name="type"/> is inferred from left if null.</summary>
    public ushort Binary(ExpressionType op, ushort left, ushort right, Type type = null, MethodInfo method = null)
    {
        type ??= Nodes.GetSurePresentRef(left).Type;
        return AddNodeWithChildren(op, type, method, 0, new[] { left, right });
    }

    /// <summary>Creates an Assign node.</summary>
    public ushort Assign(ushort left, ushort right) =>
        Binary(ExpressionType.Assign, left, right);

    /// <summary>Creates an Add node.</summary>
    public ushort Add(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Add, left, right, null, method);

    /// <summary>Creates a Subtract node.</summary>
    public ushort Subtract(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Subtract, left, right, null, method);

    /// <summary>Creates a Multiply node.</summary>
    public ushort Multiply(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Multiply, left, right, null, method);

    /// <summary>Creates a Divide node.</summary>
    public ushort Divide(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Divide, left, right, null, method);

    /// <summary>Creates a Modulo node.</summary>
    public ushort Modulo(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Modulo, left, right, null, method);

    /// <summary>Creates an Equal node.</summary>
    public ushort Equal(ushort left, ushort right) =>
        Binary(ExpressionType.Equal, left, right, typeof(bool));

    /// <summary>Creates a NotEqual node.</summary>
    public ushort NotEqual(ushort left, ushort right) =>
        Binary(ExpressionType.NotEqual, left, right, typeof(bool));

    /// <summary>Creates a LessThan node.</summary>
    public ushort LessThan(ushort left, ushort right) =>
        Binary(ExpressionType.LessThan, left, right, typeof(bool));

    /// <summary>Creates a LessThanOrEqual node.</summary>
    public ushort LessThanOrEqual(ushort left, ushort right) =>
        Binary(ExpressionType.LessThanOrEqual, left, right, typeof(bool));

    /// <summary>Creates a GreaterThan node.</summary>
    public ushort GreaterThan(ushort left, ushort right) =>
        Binary(ExpressionType.GreaterThan, left, right, typeof(bool));

    /// <summary>Creates a GreaterThanOrEqual node.</summary>
    public ushort GreaterThanOrEqual(ushort left, ushort right) =>
        Binary(ExpressionType.GreaterThanOrEqual, left, right, typeof(bool));

    /// <summary>Creates an AndAlso (short-circuit AND) node.</summary>
    public ushort AndAlso(ushort left, ushort right) =>
        Binary(ExpressionType.AndAlso, left, right, typeof(bool));

    /// <summary>Creates an OrElse (short-circuit OR) node.</summary>
    public ushort OrElse(ushort left, ushort right) =>
        Binary(ExpressionType.OrElse, left, right, typeof(bool));

    /// <summary>Creates an And (bitwise) node.</summary>
    public ushort And(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.And, left, right, null, method);

    /// <summary>Creates an Or (bitwise) node.</summary>
    public ushort Or(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.Or, left, right, null, method);

    /// <summary>Creates an ExclusiveOr node.</summary>
    public ushort ExclusiveOr(ushort left, ushort right, MethodInfo method = null) =>
        Binary(ExpressionType.ExclusiveOr, left, right, null, method);

    /// <summary>Creates a Coalesce node.</summary>
    public ushort Coalesce(ushort left, ushort right)
    {
        var leftType = Nodes.GetSurePresentRef(left).Type;
        var rightType = Nodes.GetSurePresentRef(right).Type;
        var resultType = leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Nullable<>)
            ? leftType.GetGenericArguments()[0]
            : leftType;
        if (!resultType.IsAssignableFrom(rightType))
            resultType = rightType;
        return Binary(ExpressionType.Coalesce, left, right, resultType);
    }

    /// <summary>Creates an ArrayIndex node.</summary>
    public ushort ArrayIndex(ushort array, ushort index)
    {
        var elemType = Nodes.GetSurePresentRef(array).Type.GetElementType();
        return Binary(ExpressionType.ArrayIndex, array, index, elemType);
    }

    // ── Unary ─────────────────────────────────────────────────────────────────

    /// <summary>Creates a unary expression node.</summary>
    public ushort Unary(ExpressionType op, ushort operand, Type type = null, MethodInfo method = null)
    {
        type ??= Nodes.GetSurePresentRef(operand).Type;
        return AddNodeWithChildren(op, type, method, 0, new[] { operand });
    }

    /// <summary>Creates a Convert node.</summary>
    public ushort Convert(ushort operand, Type type, MethodInfo method = null) =>
        Unary(ExpressionType.Convert, operand, type, method);

    /// <summary>Creates a Not node.</summary>
    public ushort Not(ushort operand) => Unary(ExpressionType.Not, operand);

    /// <summary>Creates a Negate node.</summary>
    public ushort Negate(ushort operand) => Unary(ExpressionType.Negate, operand);

    /// <summary>Creates an ArrayLength node.</summary>
    public ushort ArrayLength(ushort array) => Unary(ExpressionType.ArrayLength, array, typeof(int));

    /// <summary>Creates a TypeAs node.</summary>
    public ushort TypeAs(ushort operand, Type type) => Unary(ExpressionType.TypeAs, operand, type);

    /// <summary>Creates a Throw node.</summary>
    public ushort Throw(ushort operand, Type type = null) =>
        Unary(ExpressionType.Throw, operand, type ?? typeof(void));

    /// <summary>Creates an Unbox node.</summary>
    public ushort Unbox(ushort operand, Type type) => Unary(ExpressionType.Unbox, operand, type);

    /// <summary>Creates a Quote node.</summary>
    public ushort Quote(ushort operand) => Unary(ExpressionType.Quote, operand);

    // ── Lambda ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Lambda node.  Extra = paramCount.  children = params[0..n-1] + body.
    /// </summary>
    public ushort Lambda(Type delegateType, ushort body, params ushort[] parameters)
    {
        var paramCount = parameters?.Length ?? 0;
        var all = new ushort[paramCount + 1];
        if (parameters != null)
            for (var i = 0; i < paramCount; i++) all[i] = parameters[i];
        all[paramCount] = body;
        return AddNodeWithChildren(ExpressionType.Lambda, delegateType, null, (byte)paramCount, all);
    }

    /// <summary>Creates a strongly-typed Lambda node.</summary>
    public ushort Lambda<TDelegate>(ushort body, params ushort[] parameters)
        where TDelegate : System.Delegate =>
        Lambda(typeof(TDelegate), body, parameters);

    // ── Block ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Block node.  Extra = varCount.  children = vars[0..m-1] + exprs[0..k-1].
    /// </summary>
    public ushort Block(ushort[] variables, ushort[] expressions, Type type = null)
    {
        var varCount = variables?.Length ?? 0;
        var exprCount = expressions?.Length ?? 0;
        var all = new ushort[varCount + exprCount];
        if (variables != null)
            for (var i = 0; i < varCount; i++) all[i] = variables[i];
        if (expressions != null)
            for (var i = 0; i < exprCount; i++) all[varCount + i] = expressions[i];

        if (exprCount == 0)
            type ??= typeof(void);
        else
            type ??= Nodes.GetSurePresentRef(expressions[exprCount - 1]).Type;
        return AddNodeWithChildren(ExpressionType.Block, type, null, (byte)varCount, all);
    }

    /// <summary>Creates a Block node with no variables.</summary>
    public ushort Block(params ushort[] expressions) => Block(null, expressions);

    /// <summary>Creates a Block node with no variables and an explicit result type.</summary>
    public ushort Block(Type type, params ushort[] expressions) => Block(null, expressions, type);

    // ── Conditional ───────────────────────────────────────────────────────────

    /// <summary>Creates a Conditional (if-then-else) node.</summary>
    public ushort Condition(ushort test, ushort ifTrue, ushort ifFalse, Type type = null)
    {
        type ??= Nodes.GetSurePresentRef(ifTrue).Type;
        return AddNodeWithChildren(ExpressionType.Conditional, type, null, 0, new[] { test, ifTrue, ifFalse });
    }

    /// <summary>Creates an IfThen (void conditional) node.</summary>
    public ushort IfThen(ushort test, ushort ifTrue) =>
        AddNodeWithChildren(ExpressionType.Conditional, typeof(void), null, 0, new[] { test, ifTrue });

    /// <summary>Creates an IfThenElse node (type derived from ifTrue).</summary>
    public ushort IfThenElse(ushort test, ushort ifTrue, ushort ifFalse) =>
        Condition(test, ifTrue, ifFalse);

    // ── Loop ──────────────────────────────────────────────────────────────────

    /// <summary>Creates a Loop node.</summary>
    public ushort Loop(ushort body, LabelTarget breakLabel = null, LabelTarget continueLabel = null)
    {
        var loopType = breakLabel?.Type ?? typeof(void);
        return AddNodeWithChildren(ExpressionType.Loop, loopType,
            new LabelTarget[] { breakLabel, continueLabel }, 0, new[] { body });
    }

    // ── Try ───────────────────────────────────────────────────────────────────

    // Extra: 0=Catch, 1=Finally, 2=CatchFinally, 3=Fault

    /// <summary>Creates a TryCatch node.</summary>
    public ushort TryCatch(ushort body, params CatchBlock[] handlers)
    {
        var bodyType = Nodes.GetSurePresentRef(body).Type;
        return AddNodeWithChildren(ExpressionType.Try, bodyType, handlers, extra: 0, new[] { body });
    }

    /// <summary>Creates a TryFinally node.</summary>
    public ushort TryFinally(ushort body, ushort @finally)
    {
        var bodyType = Nodes.GetSurePresentRef(body).Type;
        return AddNodeWithChildren(ExpressionType.Try, bodyType, null, extra: 1, new[] { body, @finally });
    }

    /// <summary>Creates a TryCatchFinally node.</summary>
    public ushort TryCatchFinally(ushort body, ushort @finally, params CatchBlock[] handlers)
    {
        var bodyType = Nodes.GetSurePresentRef(body).Type;
        return AddNodeWithChildren(ExpressionType.Try, bodyType, handlers, extra: 2, new[] { body, @finally });
    }

    /// <summary>Creates a TryFault node.</summary>
    public ushort TryFault(ushort body, ushort fault)
    {
        var bodyType = Nodes.GetSurePresentRef(body).Type;
        return AddNodeWithChildren(ExpressionType.Try, bodyType, null, extra: 3, new[] { body, fault });
    }

    // ── Label / Goto ──────────────────────────────────────────────────────────

    /// <summary>Creates a Label node.</summary>
    public ushort Label(LabelTarget target) =>
        AddNode(ExpressionType.Label, target.Type, target, 0, 0, ExprNode.NoIdx);

    /// <summary>Creates a Label node with a default value expression.</summary>
    public ushort Label(LabelTarget target, ushort defaultValue) =>
        AddNodeWithChildren(ExpressionType.Label, target.Type, target, 0, new[] { defaultValue });

    // Goto Extra: 0=Goto, 1=Return, 2=Break, 3=Continue
    private ushort MakeGoto(GotoExpressionKind kind, LabelTarget target, ushort valueIdx, Type type)
    {
        type ??= typeof(void);
        if (valueIdx != ExprNode.NoIdx)
            return AddNodeWithChildren(ExpressionType.Goto, type, target, (byte)kind, new[] { valueIdx });
        return AddNode(ExpressionType.Goto, type, target, (byte)kind, 0, ExprNode.NoIdx);
    }

    /// <summary>Creates a Goto node.</summary>
    public ushort Goto(LabelTarget target, Type type = null) =>
        MakeGoto(GotoExpressionKind.Goto, target, ExprNode.NoIdx, type);

    /// <summary>Creates a Goto node with a value.</summary>
    public ushort Goto(LabelTarget target, ushort value, Type type = null) =>
        MakeGoto(GotoExpressionKind.Goto, target, value, type ?? Nodes.GetSurePresentRef(value).Type);

    /// <summary>Creates a Return node.</summary>
    public ushort Return(LabelTarget target, Type type = null) =>
        MakeGoto(GotoExpressionKind.Return, target, ExprNode.NoIdx, type);

    /// <summary>Creates a Return node with a value.</summary>
    public ushort Return(LabelTarget target, ushort value, Type type = null) =>
        MakeGoto(GotoExpressionKind.Return, target, value, type ?? Nodes.GetSurePresentRef(value).Type);

    /// <summary>Creates a Break node.</summary>
    public ushort Break(LabelTarget target, Type type = null) =>
        MakeGoto(GotoExpressionKind.Break, target, ExprNode.NoIdx, type);

    /// <summary>Creates a Break node with a value.</summary>
    public ushort Break(LabelTarget target, ushort value, Type type = null) =>
        MakeGoto(GotoExpressionKind.Break, target, value, type ?? Nodes.GetSurePresentRef(value).Type);

    /// <summary>Creates a Continue node.</summary>
    public ushort Continue(LabelTarget target, Type type = null) =>
        MakeGoto(GotoExpressionKind.Continue, target, ExprNode.NoIdx, type);

    // ── Switch ────────────────────────────────────────────────────────────────

    /// <summary>Creates a Switch node.</summary>
    public ushort Switch(ushort switchValue, SwitchCase[] cases, ushort defaultBody = ExprNode.NoIdx,
        Type type = null, MethodInfo comparison = null)
    {
        type ??= cases?.Length > 0 ? cases[0].Body.Type :
                 defaultBody != ExprNode.NoIdx ? Nodes.GetSurePresentRef(defaultBody).Type : typeof(void);

        var children = defaultBody != ExprNode.NoIdx
            ? new[] { switchValue, defaultBody }
            : new[] { switchValue };

        return AddNodeWithChildren(ExpressionType.Switch, type,
            new object[] { cases ?? Array.Empty<SwitchCase>(), comparison }, 0, children);
    }

    // ── TypeBinary ────────────────────────────────────────────────────────────

    /// <summary>Creates a TypeIs node.</summary>
    public ushort TypeIs(ushort operand, Type type) =>
        AddNodeWithChildren(ExpressionType.TypeIs, typeof(bool), type, 0, new[] { operand });

    /// <summary>Creates a TypeEqual node.</summary>
    public ushort TypeEqual(ushort operand, Type type) =>
        AddNodeWithChildren(ExpressionType.TypeEqual, typeof(bool), type, 0, new[] { operand });

    // ── Default ───────────────────────────────────────────────────────────────

    /// <summary>Creates a Default node.</summary>
    public ushort Default(Type type) =>
        AddNode(ExpressionType.Default, type, null, 0, 0, ExprNode.NoIdx);

    // ── Invoke ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an Invocation node.  The first child is the expression to invoke,
    /// followed by the arguments.
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2080",
        Justification = "The Invoke method exists on all delegate types")]
    public ushort Invoke(ushort expression, params ushort[] args)
    {
        var exprType = Nodes.GetSurePresentRef(expression).Type;
        Type returnType;
        if (exprType.IsSubclassOf(typeof(System.Delegate)) || exprType == typeof(System.Delegate))
        {
            var invokeMethod = exprType.GetMethod("Invoke");
            returnType = invokeMethod?.ReturnType ?? typeof(object);
        }
        else
            returnType = typeof(object);

        var all = new ushort[1 + (args?.Length ?? 0)];
        all[0] = expression;
        if (args != null)
            for (var i = 0; i < args.Length; i++) all[i + 1] = args[i];
        return AddNodeWithChildren(ExpressionType.Invoke, returnType, null, 0, all);
    }

    // ── NewArray ──────────────────────────────────────────────────────────────

    /// <summary>Creates a NewArrayInit node (array with initializers).</summary>
    public ushort NewArrayInit(Type elementType, params ushort[] elements) =>
        AddNodeWithChildren(ExpressionType.NewArrayInit, elementType.MakeArrayType(), null, 0, elements);

    /// <summary>Creates a NewArrayBounds node (multi-dim array with bounds).</summary>
    public ushort NewArrayBounds(Type elementType, params ushort[] bounds) =>
        AddNodeWithChildren(ExpressionType.NewArrayBounds, elementType.MakeArrayType(), null, 0, bounds);

    // ── MemberInit ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a MemberInit node.  The single child is the New expression;
    /// bindings are stored in <see cref="ExprNode.Obj"/>.
    /// </summary>
    public ushort MemberInit(ushort newExpr, params MemberBinding[] bindings)
    {
        var type = Nodes.GetSurePresentRef(newExpr).Type;
        return AddNodeWithChildren(ExpressionType.MemberInit, type, bindings, 0, new[] { newExpr });
    }

    // ── ListInit ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a ListInit node.  The single child is the New expression;
    /// initializers are stored in <see cref="ExprNode.Obj"/>.
    /// </summary>
    public ushort ListInit(ushort newExpr, params ElementInit[] initializers)
    {
        var type = Nodes.GetSurePresentRef(newExpr).Type;
        return AddNodeWithChildren(ExpressionType.ListInit, type, initializers, 0, new[] { newExpr });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Conversion to LightExpression
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts the node at <paramref name="rootIdx"/> to a <see cref="LE.Expression"/>.
    /// The returned expression can be compiled with FastExpressionCompiler.
    /// </summary>
    [RequiresUnreferencedCode(Trimming.Message)]
    public LE.Expression ToLightExpression(ushort rootIdx)
    {
        var paramCache = new LE.ParameterExpression[Nodes.Count];
        return ConvertNode(rootIdx, paramCache);
    }

    /// <summary>
    /// Converts the node at <paramref name="rootIdx"/> to a <see cref="LE.LambdaExpression"/>.
    /// Throws if the node is not a Lambda.
    /// </summary>
    [RequiresUnreferencedCode(Trimming.Message)]
    public LE.LambdaExpression ToLambdaExpression(ushort rootIdx)
    {
        if (Nodes.GetSurePresentRef(rootIdx).NodeType != ExpressionType.Lambda)
            throw new ArgumentException("The node at rootIdx is not a Lambda node.", nameof(rootIdx));
        return (LE.LambdaExpression)ToLightExpression(rootIdx);
    }

    /// <summary>
    /// Converts the node at <paramref name="rootIdx"/> to a strongly typed
    /// <see cref="LE.Expression{TDelegate}"/>.
    /// </summary>
    [RequiresUnreferencedCode(Trimming.Message)]
    public LE.Expression<TDelegate> ToExpression<TDelegate>(ushort rootIdx)
        where TDelegate : System.Delegate =>
        (LE.Expression<TDelegate>)ToLightExpression(rootIdx);

    // ── private conversion helpers ────────────────────────────────────────────

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertNode(ushort idx, LE.ParameterExpression[] paramCache)
    {
        ref var node = ref Nodes.GetSurePresentRef(idx);
        switch (node.NodeType)
        {
            case ExpressionType.Constant:
                return LE.Expression.Constant(node.Obj, node.Type);

            case ExpressionType.Parameter:
            {
                var cached = paramCache[idx];
                if (cached != null) return cached;
                var param = node.Extra == 1
                    ? LE.Expression.Parameter(node.Type.MakeByRefType(), (string)node.Obj)
                    : LE.Expression.Parameter(node.Type, (string)node.Obj);
                return paramCache[idx] = param;
            }

            case ExpressionType.New:
                return ConvertNew(ref node, idx, paramCache);

            case ExpressionType.Call:
                return ConvertCall(ref node, idx, paramCache);

            case ExpressionType.MemberAccess:
                return ConvertMemberAccess(ref node, idx, paramCache);

            case ExpressionType.Index:
                return ConvertIndex(ref node, idx, paramCache);

            case ExpressionType.Lambda:
                return ConvertLambda(ref node, idx, paramCache);

            case ExpressionType.Block:
                return ConvertBlock(ref node, idx, paramCache);

            case ExpressionType.Conditional:
                return ConvertConditional(ref node, idx, paramCache);

            case ExpressionType.Loop:
                return ConvertLoop(ref node, idx, paramCache);

            case ExpressionType.Try:
                return ConvertTry(ref node, idx, paramCache);

            case ExpressionType.Label:
                return ConvertLabel(ref node, idx, paramCache);

            case ExpressionType.Goto:
                return ConvertGoto(ref node, idx, paramCache);

            case ExpressionType.Switch:
                return ConvertSwitch(ref node, idx, paramCache);

            case ExpressionType.TypeIs:
                return LE.Expression.TypeIs(ConvertChild(idx, 0, paramCache), (Type)node.Obj);

            case ExpressionType.TypeEqual:
                return LE.Expression.TypeEqual(ConvertChild(idx, 0, paramCache), (Type)node.Obj);

            case ExpressionType.Default:
                return LE.Expression.Default(node.Type);

            case ExpressionType.Invoke:
                return ConvertInvoke(ref node, idx, paramCache);

            case ExpressionType.NewArrayInit:
                return LE.Expression.NewArrayInit(node.Type.GetElementType(), ConvertChildren(idx, paramCache));

            case ExpressionType.NewArrayBounds:
                return LE.Expression.NewArrayBounds(node.Type.GetElementType(), ConvertChildren(idx, paramCache));

            case ExpressionType.MemberInit:
                return LE.Expression.MemberInit(
                    (LE.NewExpression)ConvertChild(idx, 0, paramCache),
                    (MemberBinding[])node.Obj);

            case ExpressionType.ListInit:
                return LE.Expression.ListInit(
                    (LE.NewExpression)ConvertChild(idx, 0, paramCache),
                    (ElementInit[])node.Obj);

            default:
                return ConvertBinaryOrUnary(ref node, idx, paramCache);
        }
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertNew(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        if (node.Obj == null)
            return LE.Expression.New(node.Type);
        var ctor = (ConstructorInfo)node.Obj;
        if (node.ChildCount == 0)
            return LE.Expression.New(ctor);
        return LE.Expression.New(ctor, ConvertChildren(idx, paramCache));
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertCall(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var method = (MethodInfo)node.Obj;
        if (node.Extra == 1) // has instance
        {
            var instance = ConvertChild(idx, 0, paramCache);
            var args = ConvertChildrenFrom(idx, 1, paramCache);
            return LE.Expression.Call(instance, method, args);
        }
        return LE.Expression.Call(method, ConvertChildren(idx, paramCache));
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertMemberAccess(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        if (node.Obj is FieldInfo field)
            return node.Extra == 1
                ? LE.Expression.Field(ConvertChild(idx, 0, paramCache), field)
                : LE.Expression.Field(null, field);
        var prop = (PropertyInfo)node.Obj;
        return node.Extra == 1
            ? LE.Expression.Property(ConvertChild(idx, 0, paramCache), prop)
            : LE.Expression.Property(null, prop);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertIndex(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var obj = ConvertChild(idx, 0, paramCache);
        var args = ConvertChildrenFrom(idx, 1, paramCache);
        if (node.Obj is PropertyInfo indexer)
            return LE.Expression.Property(obj, indexer, args);
        return LE.Expression.ArrayAccess(obj, args);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertLambda(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var paramCount = node.Extra;
        var parameters = new LE.ParameterExpression[paramCount];
        for (var i = 0; i < paramCount; i++)
        {
            var paramIdx = GetChildIdx(idx, i);
            // ensure the parameter is in the cache before converting the body
            if (paramCache[paramIdx] == null)
                ConvertNode(paramIdx, paramCache);
            parameters[i] = paramCache[paramIdx];
        }
        var body = ConvertChild(idx, paramCount, paramCache);
        return LE.Expression.Lambda(node.Type, body, parameters);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertBlock(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var varCount = node.Extra;
        var exprCount = node.ChildCount - varCount;

        var variables = new LE.ParameterExpression[varCount];
        for (var i = 0; i < varCount; i++)
        {
            var varIdx = GetChildIdx(idx, i);
            if (paramCache[varIdx] == null)
                ConvertNode(varIdx, paramCache);
            variables[i] = paramCache[varIdx];
        }

        var exprs = new LE.Expression[exprCount];
        for (var i = 0; i < exprCount; i++)
            exprs[i] = ConvertChild(idx, varCount + i, paramCache);

        if (varCount == 0)
            return LE.Expression.Block(node.Type, exprs);
        return LE.Expression.Block(node.Type, variables, exprs);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertConditional(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var test = ConvertChild(idx, 0, paramCache);
        var ifTrue = ConvertChild(idx, 1, paramCache);
        if (node.ChildCount == 2)
            return LE.Expression.IfThen(test, ifTrue);
        var ifFalse = ConvertChild(idx, 2, paramCache);
        return LE.Expression.Condition(test, ifTrue, ifFalse, node.Type);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertLoop(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var body = ConvertChild(idx, 0, paramCache);
        var labels = (LabelTarget[])node.Obj;
        var breakLabel = labels?[0];
        var continueLabel = labels?[1];
        return LE.Expression.Loop(body, breakLabel, continueLabel);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertTry(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var body = ConvertChild(idx, 0, paramCache);
        var handlers = (CatchBlock[])node.Obj;
        return node.Extra switch
        {
            0 => LE.Expression.TryCatch(body, handlers ?? Array.Empty<CatchBlock>()),
            1 => LE.Expression.TryFinally(body, ConvertChild(idx, 1, paramCache)),
            2 => LE.Expression.TryCatchFinally(body, ConvertChild(idx, 1, paramCache),
                     handlers ?? Array.Empty<CatchBlock>()),
            3 => LE.Expression.TryFault(body, ConvertChild(idx, 1, paramCache)),
            _ => throw new InvalidOperationException($"Unknown Try variant {node.Extra}")
        };
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertLabel(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var target = (LabelTarget)node.Obj;
        if (node.ChildCount == 0)
            return LE.Expression.Label(target);
        return LE.Expression.Label(target, ConvertChild(idx, 0, paramCache));
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertGoto(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var target = (LabelTarget)node.Obj;
        var kind = (GotoExpressionKind)node.Extra;
        LE.Expression value = node.ChildCount > 0 ? ConvertChild(idx, 0, paramCache) : null;
        return LE.Expression.MakeGoto(kind, target, value, node.Type);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertSwitch(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var payload = (object[])node.Obj;
        var cases = (SwitchCase[])payload[0];
        var comparison = (MethodInfo)payload[1];
        var switchValue = ConvertChild(idx, 0, paramCache);
        LE.Expression defaultBody = node.ChildCount > 1 ? ConvertChild(idx, 1, paramCache) : null;
        return LE.Expression.Switch(node.Type, switchValue, defaultBody, comparison, cases);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertInvoke(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        var expr = ConvertChild(idx, 0, paramCache);
        var args = ConvertChildrenFrom(idx, 1, paramCache);
        return LE.Expression.Invoke(expr, args);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertBinaryOrUnary(ref ExprNode node, ushort idx, LE.ParameterExpression[] paramCache)
    {
        if (node.ChildCount == 2)
        {
            var left = ConvertChild(idx, 0, paramCache);
            var right = ConvertChild(idx, 1, paramCache);
            var method = node.Obj as MethodInfo;
            if (node.NodeType == ExpressionType.Coalesce)
                return LE.Expression.Coalesce(left, right);
            if (node.NodeType == ExpressionType.ArrayIndex)
                return LE.Expression.ArrayIndex(left, right);
            return LE.Expression.MakeBinary(node.NodeType, left, right,
                liftToNull: false, method, conversion: null);
        }
        if (node.ChildCount == 1)
        {
            var operand = ConvertChild(idx, 0, paramCache);
            var method = node.Obj as MethodInfo;
            return LE.Expression.MakeUnary(node.NodeType, operand, node.Type, method);
        }
        throw new NotSupportedException($"Cannot convert ExprNode with NodeType={node.NodeType} and ChildCount={node.ChildCount}");
    }

    // ── child access helpers ──────────────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetChildIdx(ushort parentIdx, int childOffset)
    {
        ref var parent = ref Nodes.GetSurePresentRef(parentIdx);
        return ChildPool.GetSurePresentRef(parent.FirstChildPoolIdx + childOffset);
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression ConvertChild(ushort parentIdx, int childOffset, LE.ParameterExpression[] paramCache) =>
        ConvertNode(GetChildIdx(parentIdx, childOffset), paramCache);

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression[] ConvertChildren(ushort parentIdx, LE.ParameterExpression[] paramCache)
    {
        ref var parent = ref Nodes.GetSurePresentRef(parentIdx);
        var count = parent.ChildCount;
        if (count == 0) return Array.Empty<LE.Expression>();
        var result = new LE.Expression[count];
        var poolBase = parent.FirstChildPoolIdx;
        for (var i = 0; i < count; i++)
            result[i] = ConvertNode(ChildPool.GetSurePresentRef(poolBase + i), paramCache);
        return result;
    }

    [RequiresUnreferencedCode(Trimming.Message)]
    private LE.Expression[] ConvertChildrenFrom(ushort parentIdx, int startOffset, LE.ParameterExpression[] paramCache)
    {
        ref var parent = ref Nodes.GetSurePresentRef(parentIdx);
        var count = parent.ChildCount - startOffset;
        if (count <= 0) return Array.Empty<LE.Expression>();
        var result = new LE.Expression[count];
        var poolBase = parent.FirstChildPoolIdx;
        for (var i = 0; i < count; i++)
            result[i] = ConvertNode(ChildPool.GetSurePresentRef(poolBase + startOffset + i), paramCache);
        return result;
    }
}
