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

/// <summary>Stores one flat expression node plus its intrusive child-link metadata in 24 bytes on 64-bit runtimes.</summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct ExprNode
{
    private const int NodeTypeShift = 24;
    private const int TagShift = 16;
    private const int CountShift = 16;
    private const uint IndexMask = 0xFFFF;
    private const uint KindMask = 0x0F;
    private const uint NextMask = IndexMask;
    private const uint KeepWithoutNextMask = ~NextMask;
    private const uint KeepWithoutTagAndNextMask = ~(NextMask | (0xFFU << TagShift));
    private const int FlagsShift = 4;

    /// <summary>Gets or sets the runtime type of the represented node.</summary>
    [FieldOffset(0)]
    public Type Type;

    /// <summary>Gets or sets the runtime payload associated with the node.</summary>
    [FieldOffset(8)]
    public object Obj;
    [FieldOffset(16)]
    private uint _data;

    [FieldOffset(20)]
    private uint _nodeTypeAndKind;

    /// <summary>Gets the expression kind encoded for this node.</summary>
    public ExpressionType NodeType => (ExpressionType)((_nodeTypeAndKind >> NodeTypeShift) & 0xFF);

    /// <summary>Gets the payload classification for this node.</summary>
    public ExprNodeKind Kind => (ExprNodeKind)((_nodeTypeAndKind >> TagShift) & KindMask);

    internal byte Flags => (byte)(((byte)(_nodeTypeAndKind >> TagShift)) >> FlagsShift);

    /// <summary>Gets the next sibling node index in the intrusive child chain.</summary>
    public int NextIdx => (int)(_nodeTypeAndKind & IndexMask);

    /// <summary>Gets the number of direct children linked from this node.</summary>
    public int ChildCount => (int)((_data >> CountShift) & IndexMask);

    /// <summary>Gets the first child index or an auxiliary payload index.</summary>
    public int ChildIdx => (int)(_data & IndexMask);

    internal int Value32 => unchecked((int)_data);

    internal ExprNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags = 0, int childIdx = 0, int childCount = 0, int nextIdx = 0)
    {
        Type = type;
        Obj = obj;
        var tag = (byte)((flags << FlagsShift) | (byte)kind);
        _data = ((uint)(ushort)childCount << CountShift)
            | (ushort)childIdx;
        _nodeTypeAndKind = ((uint)(byte)nodeType << NodeTypeShift)
            | ((uint)tag << TagShift)
            | (ushort)nextIdx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetNextIdx(int nextIdx, bool pointsToParent = false)
    {
        var tag = (byte)(_nodeTypeAndKind >> TagShift);
        var nextPointsToParentMask = (byte)(ExprTree.NextPointsToParentFlag << FlagsShift);
        tag = pointsToParent
            ? (byte)(tag | nextPointsToParentMask)
            : (byte)(tag & ~nextPointsToParentMask);
        _nodeTypeAndKind = (_nodeTypeAndKind & KeepWithoutTagAndNextMask)
            | ((uint)tag << TagShift)
            | (ushort)nextIdx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetChildInfo(int childIdx, int childCount) =>
        _data = ((uint)(ushort)childCount << CountShift)
            | (ushort)childIdx;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetValue32(int value) =>
        _data = unchecked((uint)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Is(ExprNodeKind kind) => Kind == kind;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsExpression() => Kind == ExprNodeKind.Expression;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasFlag(byte flag) => (Flags & flag) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool NextPointsToParent() => HasFlag(ExprTree.NextPointsToParentFlag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsParameterDeclaration() =>
        NodeType == ExpressionType.Parameter && HasFlag(ExprTree.ParameterDeclarationFlag);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ShouldCloneWhenLinked() =>
        Kind == ExprNodeKind.LabelTarget || NodeType == ExpressionType.Parameter || Kind == ExprNodeKind.ObjectReference || ChildCount == 0;
}

/// <summary>Stores an expression tree as a flat node array plus out-of-line closure constants.</summary>
public struct ExprTree
{
    private static readonly object ClosureConstantMarker = new();
    internal const byte ParameterByRefFlag = 1;
    internal const byte ParameterDeclarationFlag = 2;
    private const byte ConstantInlineValue32Flag = 1;
    private const byte BinaryLiftedToNullFlag = 1;
    private const byte LoopHasBreakFlag = 1;
    private const byte LoopHasContinueFlag = 2;
    private const byte CatchHasVariableFlag = 1;
    private const byte CatchHasFilterFlag = 2;
    private const byte TryFaultFlag = 1;
    internal const byte NextPointsToParentFlag = 8;
    private const ushort UnboundParameterScopeIndex = ushort.MaxValue;

    /// <summary>Gets or sets the root node index.</summary>
    public int RootIndex;

    /// <summary>Gets or sets the flat node storage.</summary>
    public SmallList<ExprNode, Stack16<ExprNode>, NoArrayPool<ExprNode>> Nodes;

    /// <summary>Gets or sets closure constants that are referenced from constant nodes.</summary>
    public SmallList<object, Stack16<object>, NoArrayPool<object>> ClosureConstants;

    /// <summary>Adds a parameter node and returns its index.</summary>
    public int Parameter(Type type, string name = null)
    {
        var parameterType = type.IsByRef ? type.GetElementType() ?? type : type;
        return AddRawLeafExpressionNode(parameterType, name, ExpressionType.Parameter,
            (byte)((type.IsByRef ? ParameterByRefFlag : 0) | ParameterDeclarationFlag),
            childIdx: UnboundParameterScopeIndex);
    }

    /// <summary>Adds a typed parameter node and returns its index.</summary>
    public int ParameterOf<T>(string name = null) => Parameter(typeof(T), name);

    /// <summary>Adds a variable node and returns its index.</summary>
    public int Variable(Type type, string name = null) => Parameter(type, name);

    /// <summary>Adds a default-value node and returns its index.</summary>
    public int Default(Type type) => AddRawExpressionNode(type, null, ExpressionType.Default);

    /// <summary>Adds a constant node using the runtime type of the supplied value.</summary>
    public int Constant(object value) =>
        Constant(value, value?.GetType() ?? typeof(object));

    /// <summary>Adds a constant node with an explicit constant type.</summary>
    public int Constant(object value, Type type)
    {
        if (TryGetInlineConstantValue32(value, type, out var value32))
            return AddRawInlineConstantNode(type, value32);

        if (ShouldStoreConstantInClosureConstants(value, type))
        {
            var constantIndex = ClosureConstants.Add(value);
            return AddRawExpressionNodeWithChildIndex(type, ClosureConstantMarker, ExpressionType.Constant, constantIndex);
        }

        return AddRawExpressionNode(type, value, ExpressionType.Constant);
    }

    /// <summary>Adds a null constant node.</summary>
    public int ConstantNull(Type type = null) => AddRawExpressionNode(type ?? typeof(object), null, ExpressionType.Constant);

    /// <summary>Adds an <see cref="int"/> constant node.</summary>
    public int ConstantInt(int value) => AddRawInlineConstantNode(typeof(int), value);

    /// <summary>Adds a typed constant node.</summary>
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
    public int Field(int instance, System.Reflection.FieldInfo field) => MakeMemberAccess(instance, field);

    /// <summary>Adds a property-access node.</summary>
    public int Property(int instance, System.Reflection.PropertyInfo property) => MakeMemberAccess(instance, property);

    /// <summary>Adds a static property-access node.</summary>
    public int Property(System.Reflection.PropertyInfo property) => MakeMemberAccess(null, property);

    /// <summary>Adds an indexed property-access node.</summary>
    public int Property(int instance, System.Reflection.PropertyInfo property, params int[] arguments) =>
        arguments == null || arguments.Length == 0
            ? Property(instance, property)
            : AddFactoryExpressionNode(property.PropertyType, property, ExpressionType.Index, PrependToChildList(instance, arguments));

    /// <summary>Adds a one-dimensional array index node.</summary>
    public int ArrayIndex(int array, int index) => MakeBinary(ExpressionType.ArrayIndex, array, index);

    /// <summary>Adds an array access node.</summary>
    public int ArrayAccess(int array, params int[] indexes) =>
        indexes != null && indexes.Length == 1
            ? ArrayIndex(array, indexes[0])
            : AddFactoryExpressionNode(GetArrayElementType(Nodes[array].Type, indexes?.Length ?? 0), null, ExpressionType.Index, PrependToChildList(array, indexes));

    /// <summary>Adds a conversion node.</summary>
    public int Convert(int operand, Type type, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type, method, ExpressionType.Convert, operand);

    /// <summary>Adds a type-as node.</summary>
    public int TypeAs(int operand, Type type) =>
        AddFactoryExpressionNode(type, null, ExpressionType.TypeAs, operand);

    /// <summary>Adds a numeric negation node.</summary>
    public int Negate(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Negate, operand, method: method);

    /// <summary>Adds a logical or bitwise not node.</summary>
    public int Not(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Not, operand, method: method);

    /// <summary>Adds a unary node of the specified kind.</summary>
    public int MakeUnary(ExpressionType nodeType, int operand, Type type = null, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type ?? GetUnaryResultType(nodeType, Nodes[operand].Type, method), method, nodeType, operand);

    /// <summary>Adds an assignment node.</summary>
    public int Assign(int left, int right) => MakeBinary(ExpressionType.Assign, left, right);

    /// <summary>Adds an addition node.</summary>
    public int Add(int left, int right, System.Reflection.MethodInfo method = null) => MakeBinary(ExpressionType.Add, left, right, method: method);

    /// <summary>Adds an equality node.</summary>
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
    public int Block(params int[] expressions) =>
        Block(null, null, expressions);

    /// <summary>Adds a block node with optional explicit result type and variables.</summary>
    public int Block(Type type, IEnumerable<int> variables, params int[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("Block should contain at least one expression.", nameof(expressions));

        ChildList children = default;
        if (variables != null)
        {
            ChildList variableChildren = default;
            foreach (var variable in variables)
                variableChildren.Add(RequireParameterDeclarationIndex(variable));
            if (variableChildren.Count != 0)
                children.Add(AddChildListNode(in variableChildren));
        }
        ChildList bodyChildren = default;
        for (var i = 0; i < expressions.Length; ++i)
            bodyChildren.Add(CloneChild(expressions[i]));
        children.Add(AddChildListNode(in bodyChildren));
        var blockIndex = AddNode(type ?? Nodes.GetSurePresentRef(expressions[expressions.Length - 1]).Type, null, ExpressionType.Block, ExprNodeKind.Expression, 0, in children);
        if (variables != null && children.Count == 2)
            BindParameterDeclarations(blockIndex, GetChildren(children[0]));
        return blockIndex;
    }

    /// <summary>Adds a typed lambda node.</summary>
    public int Lambda<TDelegate>(int body, params int[] parameters) where TDelegate : Delegate =>
        Lambda(typeof(TDelegate), body, parameters);

    /// <summary>Adds a lambda node.</summary>
    public int Lambda(Type delegateType, int body, params int[] parameters)
    {
        if (parameters == null || parameters.Length == 0)
            return AddFactoryExpressionNode(delegateType, null, ExpressionType.Lambda, 0, body);

        ChildList children = default;
        children.Add(CloneChild(body));
        ChildList declarations = default;
        for (var i = 0; i < parameters.Length; ++i)
        {
            var declarationIndex = RequireParameterDeclarationIndex(parameters[i]);
            declarations.Add(declarationIndex);
            children.Add(declarationIndex);
        }

        var lambdaIndex = AddNode(delegateType, null, ExpressionType.Lambda, ExprNodeKind.Expression, 0, in children);
        BindParameterDeclarations(lambdaIndex, declarations);
        return lambdaIndex;
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
    public int Label(int target, int? defaultValue = null) =>
        defaultValue.HasValue
            ? AddFactoryExpressionNode(Nodes[target].Type, null, ExpressionType.Label, 0, target, defaultValue.Value)
            : AddFactoryExpressionNode(Nodes[target].Type, null, ExpressionType.Label, 0, target);

    /// <summary>Adds a goto-family node.</summary>
    public int MakeGoto(GotoExpressionKind kind, int target, int? value = null, Type type = null)
    {
        var resultType = type ?? (value.HasValue ? Nodes[value.Value].Type : typeof(void));
        return value.HasValue
            ? AddFactoryExpressionNode(resultType, kind, ExpressionType.Goto, 0, target, value.Value)
            : AddFactoryExpressionNode(resultType, kind, ExpressionType.Goto, 0, target);
    }

    /// <summary>Adds a goto node.</summary>
    public int Goto(int target, int? value = null, Type type = null) => MakeGoto(GotoExpressionKind.Goto, target, value, type);

    /// <summary>Adds a return node.</summary>
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
    public int TryCatch(int body, params int[] handlers)
    {
        if (handlers == null || handlers.Length == 0)
            return AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, body);

        ChildList handlerChildren = default;
        for (var i = 0; i < handlers.Length; ++i)
            handlerChildren.Add(handlers[i]);
        ChildList children = default;
        children.Add(body);
        children.Add(AddChildListNode(in handlerChildren));
        return AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, in children);
    }

    /// <summary>Adds a try/finally node.</summary>
    public int TryFinally(int body, int @finally) =>
        AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, body, @finally);

    /// <summary>Adds a try/fault node.</summary>
    public int TryFault(int body, int fault) =>
        AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, TryFaultFlag, body, fault);

    /// <summary>Adds a try node with optional finally block and catch handlers.</summary>
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
        return AddFactoryExpressionNode(Nodes[body].Type, null, ExpressionType.Try, 0, in children);
    }

    /// <summary>Adds a type-test node.</summary>
    public int TypeIs(int expression, Type type) =>
        AddFactoryExpressionNode(typeof(bool), type, ExpressionType.TypeIs, expression);

    /// <summary>Adds a type-equality test node.</summary>
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

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, int child) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, CloneChild(child));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int child) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(child));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3, int c4) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3), CloneChild(c4));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, int c0, int c1, int c2, int c3, int c4, int c5) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, CloneChild(c0), CloneChild(c1), CloneChild(c2), CloneChild(c3), CloneChild(c4), CloneChild(c5));

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

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, in cloned);
    }

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, nodeType, ExprNodeKind.Expression, flags, in cloned);
    }

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType) =>
        AddLeafNode(type, obj, nodeType, ExprNodeKind.Expression, 0, 0, 0);

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, in ChildList children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, in children);

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, int[] children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, children);

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, int child0, int child1, int child2) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, 0, child0, child1, child2);

    private int AddRawLeafExpressionNode(Type type, object obj, ExpressionType nodeType, byte flags = 0, int childIdx = 0, int childCount = 0) =>
        AddLeafNode(type, obj, nodeType, ExprNodeKind.Expression, flags, childIdx, childCount);

    private int AddRawInlineConstantNode(Type type, int value32) =>
        AddRawLeafExpressionNode(type, null, ExpressionType.Constant, ConstantInlineValue32Flag,
            childIdx: (ushort)value32, childCount: (ushort)(value32 >> 16));

    private int AddRawExpressionNodeWithChildIndex(Type type, object obj, ExpressionType nodeType, int childIdx) =>
        AddRawLeafExpressionNode(type, obj, nodeType, childIdx: childIdx);

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, int child) =>
        AddNode(type, obj, ExpressionType.Extension, kind, flags, CloneChild(child));

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, int child) =>
        AddFactoryAuxNode(type, obj, kind, 0, child);

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, int child0, int child1) =>
        AddNode(type, obj, ExpressionType.Extension, kind, flags, CloneChild(child0), CloneChild(child1));

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, int[] children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, ExpressionType.Extension, kind, 0, in cloned);
    }

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, byte flags, in ChildList children)
    {
        var cloned = CloneChildren(children);
        return AddNode(type, obj, ExpressionType.Extension, kind, flags, in cloned);
    }

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, in ChildList children) =>
        AddFactoryAuxNode(type, obj, kind, 0, in children);

    private int AddRawAuxNode(Type type, object obj, ExprNodeKind kind, in ChildList children) =>
        AddNode(type, obj, ExpressionType.Extension, kind, 0, in children);

    private int AddRawLeafAuxNode(Type type, object obj, ExprNodeKind kind, byte flags = 0, int childIdx = 0, int childCount = 0) =>
        AddLeafNode(type, obj, ExpressionType.Extension, kind, flags, childIdx, childCount);

    private int AddObjectReferenceNode(Type type, object obj) =>
        AddRawLeafAuxNode(type, obj, ExprNodeKind.ObjectReference);

    private int AddChildListNode(in ChildList children) =>
        AddRawAuxNode(null, null, ExprNodeKind.ChildList, in children);

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
        private SmallMap16<object, int, RefEq<object>> _parameterDeclarations;
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
                        return AddParameterUsage(parameter);
                    }
                case ExpressionType.Lambda:
                    {
                        var lambda = (System.Linq.Expressions.LambdaExpression)expression;
                        ChildList declarations = default;
                        for (var i = 0; i < lambda.Parameters.Count; ++i)
                            declarations.Add(AddParameterDeclaration(lambda.Parameters[i]));
                        var body = AddExpression(lambda.Body);
                        ChildList children = default;
                        children.Add(body);
                        for (var i = 0; i < declarations.Count; ++i)
                            children.Add(declarations[i]);
                        var lambdaIndex = _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                        _tree.BindParameterDeclarations(lambdaIndex, declarations);
                        return lambdaIndex;
                    }
                case ExpressionType.Block:
                    {
                        var block = (System.Linq.Expressions.BlockExpression)expression;
                        ChildList children = default;
                        ChildList variables = default;
                        if (block.Variables.Count != 0)
                        {
                            for (var i = 0; i < block.Variables.Count; ++i)
                                variables.Add(AddParameterDeclaration(block.Variables[i]));
                            children.Add(_tree.AddChildListNode(in variables));
                        }
                        ChildList expressions = default;
                        for (var i = 0; i < block.Expressions.Count; ++i)
                            expressions.Add(AddExpression(block.Expressions[i]));
                        children.Add(_tree.AddChildListNode(in expressions));
                        var blockIndex = _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, in children);
                        if (variables.Count != 0)
                            _tree.BindParameterDeclarations(blockIndex, variables);
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
                        return _tree.AddRawExpressionNode(expression.Type, @goto.Kind, expression.NodeType, children);
                    }
                case ExpressionType.Label:
                    {
                        var label = (System.Linq.Expressions.LabelExpression)expression;
                        ChildList children = default;
                        children.Add(AddLabelTarget(label.Target));
                        if (label.DefaultValue != null)
                            children.Add(AddExpression(label.DefaultValue));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
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
                        return _tree.AddNode(expression.Type, null, expression.NodeType, ExprNodeKind.Expression, flags, in children);
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

        private int AddConstant(System.Linq.Expressions.ConstantExpression constant)
        {
            if (TryGetInlineConstantValue32(constant.Value, constant.Type, out var value32))
                return _tree.AddRawInlineConstantNode(constant.Type, value32);

            if (ShouldStoreConstantInClosureConstants(constant.Value, constant.Type))
            {
                var constantIndex = _tree.ClosureConstants.Add(constant.Value);
                return _tree.AddRawExpressionNodeWithChildIndex(constant.Type, ClosureConstantMarker, constant.NodeType, constantIndex);
            }

            return _tree.AddRawExpressionNode(constant.Type, constant.Value, constant.NodeType);
        }

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

        private int AddParameterDeclaration(SysParameterExpression parameter)
        {
            ref var declarationIndex = ref _parameterDeclarations.Map.AddOrGetValueRef(parameter, out var found);
            if (found)
                return Throw.DuplicateParameterDeclaration<int>(parameter.Name);

            var parameterType = parameter.IsByRef ? parameter.Type.GetElementType() ?? parameter.Type : parameter.Type;
            declarationIndex = _tree.AddRawLeafExpressionNode(parameterType, parameter.Name, ExpressionType.Parameter,
                (byte)((parameter.IsByRef ? ParameterByRefFlag : 0) | ParameterDeclarationFlag),
                childIdx: UnboundParameterScopeIndex);
            return declarationIndex;
        }

        private int AddParameterUsage(SysParameterExpression parameter)
        {
            ref var declarationIndex = ref _parameterDeclarations.Map.AddOrGetValueRef(parameter, out var found);
            if (!found)
            {
                var parameterType = parameter.IsByRef ? parameter.Type.GetElementType() ?? parameter.Type : parameter.Type;
                declarationIndex = _tree.AddRawLeafExpressionNode(parameterType, parameter.Name, ExpressionType.Parameter,
                    (byte)((parameter.IsByRef ? ParameterByRefFlag : 0) | ParameterDeclarationFlag),
                    childIdx: UnboundParameterScopeIndex);
            }

            return _tree.AddParameterUsageNode(in _tree.Nodes.GetSurePresentRef(declarationIndex), declarationIndex);
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

    private int AddLeafNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int childIdx, int childCount)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, childIdx, childCount);
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
        Nodes.GetSurePresentRef(child0).SetNextIdx(nodeIndex, pointsToParent: true);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 2);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(nodeIndex, pointsToParent: true);
        return nodeIndex;
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, byte flags, int c0, int c1, int c2)
    {
        var nodeIndex = Nodes.Count;
        ref var newNode = ref Nodes.AddDefaultAndGetRef();
        newNode = new ExprNode(type, obj, nodeType, kind, flags, c0, 3);
        Nodes.GetSurePresentRef(c0).SetNextIdx(c1);
        Nodes.GetSurePresentRef(c1).SetNextIdx(c2);
        Nodes.GetSurePresentRef(c2).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(c3).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(c4).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(c5).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(c6).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(children[children.Length - 1]).SetNextIdx(nodeIndex, pointsToParent: true);
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
        Nodes.GetSurePresentRef(children[children.Count - 1]).SetNextIdx(nodeIndex, pointsToParent: true);
        return nodeIndex;
    }

    private int RequireParameterDeclarationIndex(int index)
    {
        ref var node = ref Nodes.GetSurePresentRef(index);
        if (node.IsParameterDeclaration())
            return index;
        return Throw.ParameterDeclarationExpected<int>(index);
    }

    private void BindParameterDeclarations(int ownerIndex, in ChildList declarations)
    {
        for (var i = 0; i < declarations.Count; ++i)
            BindParameterDeclaration(ownerIndex, declarations[i], i);
    }

    private void BindParameterDeclaration(int ownerIndex, int declarationIndex, int position)
    {
        ref var declaration = ref Nodes.GetSurePresentRef(declarationIndex);
        if (!declaration.IsParameterDeclaration())
            Throw.ParameterDeclarationExpected(declarationIndex);
        if (declaration.ChildIdx != UnboundParameterScopeIndex)
            Throw.DuplicateParameterDeclaration(declaration.Obj as string);

        declaration.SetChildInfo(ownerIndex, position);
    }

    private int AddParameterUsageNode(in ExprNode node, int declarationIndex) =>
        AddRawLeafExpressionNode(node.Type, node.Obj, ExpressionType.Parameter,
            (byte)(node.Flags & ~(ParameterDeclarationFlag | NextPointsToParentFlag)), childIdx: declarationIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetInlineConstantValue32(object value, Type type, out int value32)
    {
        if (value == null)
        {
            value32 = default;
            return false;
        }

        var valueType = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
        switch (Type.GetTypeCode(valueType))
        {
            case TypeCode.Boolean:
                value32 = (bool)value ? 1 : 0;
                return true;
            case TypeCode.Char:
                value32 = (char)value;
                return true;
            case TypeCode.SByte:
                value32 = (sbyte)value;
                return true;
            case TypeCode.Byte:
                value32 = (byte)value;
                return true;
            case TypeCode.Int16:
                value32 = (short)value;
                return true;
            case TypeCode.UInt16:
                value32 = (ushort)value;
                return true;
            case TypeCode.Int32:
                value32 = (int)value;
                return true;
            case TypeCode.UInt32:
                value32 = unchecked((int)(uint)value);
                return true;
            case TypeCode.Single:
                value32 = ConvertSingleToInt32Bits((float)value);
                return true;
            default:
                value32 = default;
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldStoreConstantInClosureConstants(object value, Type type) =>
        value != null && value is not string && value is not Type && !type.IsEnum && Type.GetTypeCode(type) == TypeCode.Object;

    private static object ReadInlineConstantValue32(Type type, int value32)
    {
        if (type.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(type);
            return Enum.ToObject(type, ReadInlineConstantValue32(underlyingType, value32));
        }

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => value32 != 0,
            TypeCode.Char => (char)value32,
            TypeCode.SByte => (sbyte)value32,
            TypeCode.Byte => (byte)value32,
            TypeCode.Int16 => (short)value32,
            TypeCode.UInt16 => (ushort)value32,
            TypeCode.Int32 => value32,
            TypeCode.UInt32 => unchecked((uint)value32),
            TypeCode.Single => ConvertInt32BitsToSingle(value32),
            _ => Throw.UnsupportedInlineConstantType<object>(type)
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct SingleInt32Union
    {
        [FieldOffset(0)]
        public float Single;

        [FieldOffset(0)]
        public int Int32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ConvertSingleToInt32Bits(float value) =>
        new SingleInt32Union { Single = value }.Int32;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ConvertInt32BitsToSingle(int value) =>
        new SingleInt32Union { Int32 = value }.Single;

    private ChildList GetChildren(int index)
    {
        ref var node = ref Nodes.GetSurePresentRef(index);
        var count = node.ChildCount;
        ChildList children = default;
        var childIndex = node.ChildIdx;
        for (var i = 0; i < count; ++i)
        {
            children.Add(childIndex);
            childIndex = Nodes.GetSurePresentRef(childIndex).NextIdx;
        }
        return children;
    }

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

    private int CloneChild(int index)
    {
        ref var node = ref Nodes.GetSurePresentRef(index);
        if (node.NodeType == ExpressionType.Parameter)
            return AddParameterUsageNode(in node, node.IsParameterDeclaration() ? index : node.ChildIdx);

        return node.ShouldCloneWhenLinked()
            ? AddLeafNode(node.Type, node.Obj, node.NodeType, node.Kind, (byte)(node.Flags & ~NextPointsToParentFlag), node.ChildIdx, node.ChildCount)
            : index;
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
        private SmallMap16<int, SysParameterExpression, IntEq> _parametersByDeclarationIndex;
        private SmallMap16<int, SysLabelTarget, IntEq> _labelsById;

        public Reader(ExprTree tree)
        {
            _tree = tree;
            _parametersByDeclarationIndex = default;
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
                    return SysExpr.Constant(
                        node.HasFlag(ConstantInlineValue32Flag)
                            ? ReadInlineConstantValue32(node.Type, node.Value32)
                            : ReferenceEquals(node.Obj, ClosureConstantMarker)
                                ? _tree.ClosureConstants[node.ChildIdx]
                                : node.Obj,
                        node.Type);
                case ExpressionType.Default:
                    return SysExpr.Default(node.Type);
                case ExpressionType.Parameter:
                    return ReadParameter(index, in node);
                case ExpressionType.Lambda:
                    {
                        var children = GetChildren(index);
                        var parameters = new SysParameterExpression[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            parameters[i - 1] = (SysParameterExpression)ReadExpression(children[i]);
                        var body = ReadExpression(children[0]);
                        return SysExpr.Lambda(node.Type, body, parameters);
                    }
                case ExpressionType.Block:
                    {
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

        private SysParameterExpression ReadParameter(int index, in ExprNode node)
        {
            var declarationIndex = node.IsParameterDeclaration() ? index : node.ChildIdx;
            if (!node.IsParameterDeclaration())
            {
                ref var existing = ref _parametersByDeclarationIndex.Map.TryGetValueRef(declarationIndex, out var foundExisting);
                return foundExisting && existing != null
                    ? existing
                    : ReadParameter(declarationIndex, in _tree.Nodes[declarationIndex]);
            }

            ref var parameter = ref _parametersByDeclarationIndex.Map.AddOrGetValueRef(declarationIndex, out var found);
            if (found && parameter != null)
                return parameter;

            var parameterType = node.HasFlag(ParameterByRefFlag) && !node.Type.IsByRef ? node.Type.MakeByRefType() : node.Type;
            return parameter = SysExpr.Parameter(parameterType, (string)node.Obj);
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
            ref var node = ref _tree.Nodes[index];
            var count = node.ChildCount;
            ChildList children = default;
            var childIndex = node.ChildIdx;
            for (var i = 0; i < count; ++i)
            {
                children.Add(childIndex);
                childIndex = _tree.Nodes[childIndex].NextIdx;
            }
            return children;
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

internal static class Throw
{
    public static void DuplicateParameterDeclaration(string name) =>
        throw new ArgumentException($"The parameter or variable `{name ?? "?"}` is declared more than once.");

    public static void ParameterDeclarationExpected(int index) =>
        throw new ArgumentException($"Expected a parameter declaration node at index {index}.");

    public static void UndeclaredParameterUsage(string name) =>
        throw new InvalidOperationException($"The parameter or variable `{name ?? "?"}` is used before it is declared.");

    public static void UnsupportedInlineConstantType(Type type) =>
        throw new NotSupportedException($"Inline 32-bit constant storage does not support `{type}`.");

    public static T DuplicateParameterDeclaration<T>(string name)
    {
        DuplicateParameterDeclaration(name);
        return default;
    }

    public static T ParameterDeclarationExpected<T>(int index)
    {
        ParameterDeclarationExpected(index);
        return default;
    }

    public static T UndeclaredParameterUsage<T>(string name)
    {
        UndeclaredParameterUsage(name);
        return default;
    }

    public static T UnsupportedInlineConstantType<T>(Type type)
    {
        UnsupportedInlineConstantType(type);
        return default;
    }
}

/// <summary>Provides conversions from System and LightExpression trees to <see cref="ExprTree"/>.</summary>
public static class FlatExpressionExtensions
{
    /// <summary>Flattens a System.Linq expression tree.</summary>
    public static ExprTree ToFlatExpression(this SysExpr expression) => ExprTree.FromExpression(expression);

    /// <summary>Flattens a LightExpression tree.</summary>
    public static ExprTree ToFlatExpression(this LightExpression expression) => ExprTree.FromLightExpression(expression);
}
