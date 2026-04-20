namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FastExpressionCompiler.LightExpression.ImTools;
using LightExpression = FastExpressionCompiler.LightExpression.Expression;
using SysCatchBlock = System.Linq.Expressions.CatchBlock;
using SysElementInit = System.Linq.Expressions.ElementInit;
using SysExpr = System.Linq.Expressions.Expression;
using SysLabelTarget = System.Linq.Expressions.LabelTarget;
using SysMemberBinding = System.Linq.Expressions.MemberBinding;
using SysParameterExpression = System.Linq.Expressions.ParameterExpression;
using SysSwitchCase = System.Linq.Expressions.SwitchCase;

public enum ExprNodeKind : byte
{
    Expression,
    SwitchCase,
    CatchBlock,
    LabelTarget,
    MemberAssignment,
    MemberMemberBinding,
    MemberListBinding,
    ElementInit,
}

public struct ExprNode
{
    private const int NodeTypeShift = 56;
    private const int KindShift = 48;
    private const int NextShift = 32;
    private const int CountShift = 16;
    private const ulong IndexMask = 0xFFFF;

    public Type Type;
    public object Obj;
    private ulong _data;

    public ExpressionType NodeType => (ExpressionType)((_data >> NodeTypeShift) & 0xFF);
    public ExprNodeKind Kind => (ExprNodeKind)((_data >> KindShift) & 0xFF);
    public int NextIdx => (int)((_data >> NextShift) & IndexMask);
    public int ChildCount => (int)((_data >> CountShift) & IndexMask);
    public int ChildIdx => (int)(_data & IndexMask);

    internal ExprNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, int childIdx = 0, int childCount = 0, int nextIdx = 0)
    {
        Type = type;
        Obj = obj;
        _data = ((ulong)(byte)nodeType << NodeTypeShift)
            | ((ulong)(byte)kind << KindShift)
            | ((ulong)(ushort)nextIdx << NextShift)
            | ((ulong)(ushort)childCount << CountShift)
            | (ushort)childIdx;
    }

    internal void SetNextIdx(int nextIdx) =>
        _data = (_data & ~(IndexMask << NextShift)) | ((ulong)(ushort)nextIdx << NextShift);

    internal void SetChildInfo(int childIdx, int childCount) =>
        _data = (_data & ~((IndexMask << CountShift) | IndexMask))
            | ((ulong)(ushort)childCount << CountShift)
            | (ushort)childIdx;
}

public struct ExprTree
{
    private static readonly object ClosureConstantMarker = new();

    public int RootIndex;
    public SmallList<ExprNode, Stack16<ExprNode>, NoArrayPool<ExprNode>> Nodes;
    public SmallList<object, Stack16<object>, NoArrayPool<object>> ClosureConstants;

    public int Parameter(Type type, string name = null)
    {
        var id = Nodes.Count + 1;
        return AddRawExpressionNode(type, new ParameterData(id, name, type.IsByRef), ExpressionType.Parameter);
    }

    public int ParameterOf<T>(string name = null) => Parameter(typeof(T), name);

    public int Variable(Type type, string name = null) => Parameter(type, name);

    public int Default(Type type) => AddRawExpressionNode(type, null, ExpressionType.Default);

    public int Constant(object value) =>
        Constant(value, value?.GetType() ?? typeof(object));

    public int Constant(object value, Type type)
    {
        if (ShouldInlineConstant(value, type))
            return AddRawExpressionNode(type, value, ExpressionType.Constant);

        var constantIndex = ClosureConstants.Add(value);
        return AddRawExpressionNodeWithChildIndex(type, ClosureConstantMarker, ExpressionType.Constant, constantIndex);
    }

    public int ConstantNull(Type type = null) => AddRawExpressionNode(type ?? typeof(object), null, ExpressionType.Constant);

    public int ConstantInt(int value) => AddRawExpressionNode(typeof(int), value, ExpressionType.Constant);

    public int ConstantOf<T>(T value) => Constant(value, typeof(T));

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

    public int New(System.Reflection.ConstructorInfo constructor, params int[] arguments) =>
        AddFactoryExpressionNode(constructor.DeclaringType, constructor, ExpressionType.New, arguments);

    public int NewArrayInit(Type elementType, params int[] expressions) =>
        AddFactoryExpressionNode(elementType.MakeArrayType(), null, ExpressionType.NewArrayInit, expressions);

    public int NewArrayBounds(Type elementType, params int[] bounds) =>
        AddFactoryExpressionNode(elementType.MakeArrayType(), null, ExpressionType.NewArrayBounds, bounds);

    public int Invoke(int expression, params int[] arguments) =>
        AddFactoryExpressionNode(Nodes[expression].Type, null, ExpressionType.Invoke, Prepend(expression, arguments));

    public int Call(System.Reflection.MethodInfo method, params int[] arguments) =>
        AddFactoryExpressionNode(method.ReturnType, method, ExpressionType.Call, arguments);

    public int Call(int instance, System.Reflection.MethodInfo method, params int[] arguments) =>
        AddFactoryExpressionNode(method.ReturnType, method, ExpressionType.Call, Prepend(instance, arguments));

    public int MakeMemberAccess(int? instance, System.Reflection.MemberInfo member) =>
        AddFactoryExpressionNode(GetMemberType(member), member, ExpressionType.MemberAccess,
            instance.HasValue ? Single(instance.Value) : null);

    public int Field(int instance, System.Reflection.FieldInfo field) => MakeMemberAccess(instance, field);

    public int Property(int instance, System.Reflection.PropertyInfo property) => MakeMemberAccess(instance, property);

    public int Property(System.Reflection.PropertyInfo property) => MakeMemberAccess(null, property);

    public int Property(int instance, System.Reflection.PropertyInfo property, params int[] arguments) =>
        arguments == null || arguments.Length == 0
            ? Property(instance, property)
            : AddFactoryExpressionNode(property.PropertyType, property, ExpressionType.Index, Prepend(instance, arguments));

    public int ArrayIndex(int array, int index) => MakeBinary(ExpressionType.ArrayIndex, array, index);

    public int ArrayAccess(int array, params int[] indexes) =>
        indexes != null && indexes.Length == 1
            ? ArrayIndex(array, indexes[0])
            : AddFactoryExpressionNode(GetArrayElementType(Nodes[array].Type, indexes?.Length ?? 0), null, ExpressionType.Index, Prepend(array, indexes));

    public int Convert(int operand, Type type, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type, method, ExpressionType.Convert, operand);

    public int TypeAs(int operand, Type type) =>
        AddFactoryExpressionNode(type, null, ExpressionType.TypeAs, operand);

    public int Negate(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Negate, operand, method: method);

    public int Not(int operand, System.Reflection.MethodInfo method = null) =>
        MakeUnary(ExpressionType.Not, operand, method: method);

    public int MakeUnary(ExpressionType nodeType, int operand, Type type = null, System.Reflection.MethodInfo method = null) =>
        AddFactoryExpressionNode(type ?? GetUnaryResultType(nodeType, Nodes[operand].Type, method), method, nodeType, operand);

    public int Assign(int left, int right) => MakeBinary(ExpressionType.Assign, left, right);

    public int Add(int left, int right, System.Reflection.MethodInfo method = null) => MakeBinary(ExpressionType.Add, left, right, method: method);

    public int Equal(int left, int right, System.Reflection.MethodInfo method = null) => MakeBinary(ExpressionType.Equal, left, right, method: method);

    public int MakeBinary(ExpressionType nodeType, int left, int right, bool isLiftedToNull = false,
        System.Reflection.MethodInfo method = null, int? conversion = null, Type type = null)
    {
        var children = conversion.HasValue ? new[] { left, right, conversion.Value } : new[] { left, right };
        return AddFactoryExpressionNode(type ?? GetBinaryResultType(nodeType, Nodes[left].Type, Nodes[right].Type, method),
            new BinaryData(method, isLiftedToNull), nodeType, children);
    }

    public int Condition(int test, int ifTrue, int ifFalse, Type type = null) =>
        AddFactoryExpressionNode(type ?? Nodes[ifTrue].Type, null, ExpressionType.Conditional, new[] { test, ifTrue, ifFalse });

    public int Block(params int[] expressions) =>
        Block(null, null, expressions);

    public int Block(Type type, IEnumerable<int> variables, params int[] expressions)
    {
        if (expressions == null || expressions.Length == 0)
            throw new ArgumentException("Block should contain at least one expression.", nameof(expressions));

        var children = new List<int>();
        var variableCount = 0;
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                children.Add(variable);
                ++variableCount;
            }
        }
        children.AddRange(expressions);
        return AddFactoryExpressionNode(type ?? Nodes[expressions[expressions.Length - 1]].Type,
            new BlockData(variableCount), ExpressionType.Block, children);
    }

    public int Lambda<TDelegate>(int body, params int[] parameters) where TDelegate : Delegate =>
        Lambda(typeof(TDelegate), body, parameters);

    public int Lambda(Type delegateType, int body, params int[] parameters) =>
        AddFactoryExpressionNode(delegateType, null, ExpressionType.Lambda, Prepend(body, parameters));

    public int Bind(System.Reflection.MemberInfo member, int expression) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberAssignment, expression);

    public int MemberBind(System.Reflection.MemberInfo member, params int[] bindings) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberMemberBinding, bindings);

    public int ElementInit(System.Reflection.MethodInfo addMethod, params int[] arguments) =>
        AddFactoryAuxNode(addMethod.DeclaringType, addMethod, ExprNodeKind.ElementInit, arguments);

    public int ListBind(System.Reflection.MemberInfo member, params int[] initializers) =>
        AddFactoryAuxNode(GetMemberType(member), member, ExprNodeKind.MemberListBinding, initializers);

    public int MemberInit(int @new, params int[] bindings) =>
        AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.MemberInit, Prepend(@new, bindings));

    public int ListInit(int @new, params int[] initializers) =>
        AddFactoryExpressionNode(Nodes[@new].Type, null, ExpressionType.ListInit, Prepend(@new, initializers));

    public int Label(Type type = null, string name = null)
    {
        var id = Nodes.Count + 1;
        return AddRawAuxNode(type ?? typeof(void), new LabelTargetData(id, name), ExprNodeKind.LabelTarget);
    }

    public int Label(int target, int? defaultValue = null) =>
        AddFactoryExpressionNode(Nodes[target].Type, null, ExpressionType.Label,
            defaultValue.HasValue ? new[] { target, defaultValue.Value } : new[] { target });

    public int MakeGoto(GotoExpressionKind kind, int target, int? value = null, Type type = null)
    {
        var resultType = type ?? (value.HasValue ? Nodes[value.Value].Type : typeof(void));
        return AddFactoryExpressionNode(resultType, kind, ExpressionType.Goto,
            value.HasValue ? new[] { target, value.Value } : new[] { target });
    }

    public int Goto(int target, int? value = null, Type type = null) => MakeGoto(GotoExpressionKind.Goto, target, value, type);

    public int Return(int target, int value) => MakeGoto(GotoExpressionKind.Return, target, value, Nodes[value].Type);

    public int Loop(int body, int? @break = null, int? @continue = null)
    {
        var children = new List<int> { body };
        if (@break.HasValue)
            children.Add(@break.Value);
        if (@continue.HasValue)
            children.Add(@continue.Value);
        return AddFactoryExpressionNode(typeof(void), new LoopData(@break.HasValue, @continue.HasValue), ExpressionType.Loop, children);
    }

    public int SwitchCase(int body, params int[] testValues)
    {
        var children = new List<int>(testValues?.Length + 1 ?? 1);
        if (testValues != null && testValues.Length != 0)
            children.AddRange(testValues);
        children.Add(body);
        return AddFactoryAuxNode(Nodes[body].Type, null, ExprNodeKind.SwitchCase, children);
    }

    public int Switch(int switchValue, params int[] cases) =>
        Switch(Nodes[switchValue].Type, switchValue, null, null, cases);

    public int Switch(Type type, int switchValue, int? defaultBody, System.Reflection.MethodInfo comparison, params int[] cases)
    {
        var children = new List<int>(cases?.Length + 2 ?? 1) { switchValue };
        if (defaultBody.HasValue)
            children.Add(defaultBody.Value);
        if (cases != null && cases.Length != 0)
            children.AddRange(cases);
        return AddFactoryExpressionNode(type, new SwitchData(defaultBody.HasValue, comparison), ExpressionType.Switch, children);
    }

    public int Catch(int variable, int body) =>
        AddFactoryAuxNode(Nodes[variable].Type, new CatchData(true, false), ExprNodeKind.CatchBlock, new[] { variable, body });

    public int Catch(Type test, int body) =>
        AddFactoryAuxNode(test, new CatchData(false, false), ExprNodeKind.CatchBlock, new[] { body });

    public int MakeCatchBlock(Type test, int? variable, int body, int? filter)
    {
        var children = new List<int>(3);
        if (variable.HasValue)
            children.Add(variable.Value);
        children.Add(body);
        if (filter.HasValue)
            children.Add(filter.Value);
        return AddFactoryAuxNode(test, new CatchData(variable.HasValue, filter.HasValue), ExprNodeKind.CatchBlock, children);
    }

    public int TryCatch(int body, params int[] handlers) =>
        AddFactoryExpressionNode(Nodes[body].Type, new TryData(false, false), ExpressionType.Try, Prepend(body, handlers));

    public int TryFinally(int body, int @finally) =>
        AddFactoryExpressionNode(Nodes[body].Type, new TryData(true, false), ExpressionType.Try, new[] { body, @finally });

    public int TryFault(int body, int fault) =>
        AddFactoryExpressionNode(Nodes[body].Type, new TryData(false, true), ExpressionType.Try, new[] { body, fault });

    public int TryCatchFinally(int body, int? @finally, params int[] handlers)
    {
        var children = new List<int> { body };
        if (@finally.HasValue)
            children.Add(@finally.Value);
        if (handlers != null && handlers.Length != 0)
            children.AddRange(handlers);
        return AddFactoryExpressionNode(Nodes[body].Type, new TryData(@finally.HasValue, false), ExpressionType.Try, children);
    }

    public int TypeIs(int expression, Type type) =>
        AddFactoryExpressionNode(typeof(bool), type, ExpressionType.TypeIs, expression);

    public int TypeEqual(int expression, Type type) =>
        AddFactoryExpressionNode(typeof(bool), type, ExpressionType.TypeEqual, expression);

    public int Dynamic(Type delegateType, CallSiteBinder binder, params int[] arguments) =>
        AddFactoryExpressionNode(typeof(object), new DynamicData(delegateType, binder), ExpressionType.Dynamic, arguments);

    public int RuntimeVariables(params int[] variables) =>
        AddFactoryExpressionNode(typeof(IRuntimeVariables), null, ExpressionType.RuntimeVariables, variables);

    public int DebugInfo(string fileName, int startLine, int startColumn, int endLine, int endColumn) =>
        AddRawExpressionNode(typeof(void), new DebugInfoData(fileName, startLine, startColumn, endLine, endColumn), ExpressionType.DebugInfo);

    public static ExprTree FromExpression(SysExpr expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        var builder = new Builder();
        return builder.Build(expression);
    }

    public static ExprTree FromLightExpression(LightExpression expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        return FromExpression(expression.ToExpression());
    }

    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077",
        Justification = "Flat expression round-trip stores the runtime type metadata explicitly for reconstruction.")]
    public SysExpr ToExpression()
    {
        if (Nodes.Count == 0)
            throw new InvalidOperationException("Flat expression tree is empty.");

        return new Reader(this).ReadExpression(RootIndex);
    }

    [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
    public LightExpression ToLightExpression() => FastExpressionCompiler.LightExpression.FromSysExpressionConverter.ToLightExpression(ToExpression());

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, int child) =>
        AddRawExpressionNode(type, obj, nodeType, CloneChild(child));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, params int[] children) =>
        AddRawExpressionNode(type, obj, nodeType, CloneChildrenToArray(children));

    private int AddFactoryExpressionNode(Type type, object obj, ExpressionType nodeType, IEnumerable<int> children) =>
        AddRawExpressionNode(type, obj, nodeType, CloneChildrenToArray(children));

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, int child) =>
        AddRawExpressionNode(type, obj, nodeType, new[] { child });

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, params int[] children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, children, 0);

    private int AddRawExpressionNode(Type type, object obj, ExpressionType nodeType, IEnumerable<int> children) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, children, 0);

    private int AddRawExpressionNodeWithChildIndex(Type type, object obj, ExpressionType nodeType, int childIdx) =>
        AddNode(type, obj, nodeType, ExprNodeKind.Expression, null, childIdx);

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, int child) =>
        AddRawAuxNode(type, obj, kind, CloneChild(child));

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, params int[] children) =>
        AddRawAuxNode(type, obj, kind, CloneChildrenToArray(children));

    private int AddFactoryAuxNode(Type type, object obj, ExprNodeKind kind, IEnumerable<int> children) =>
        AddRawAuxNode(type, obj, kind, CloneChildrenToArray(children));

    private int AddRawAuxNode(Type type, object obj, ExprNodeKind kind, params int[] children) =>
        AddNode(type, obj, ExpressionType.Extension, kind, children, 0);

    private int AddRawAuxNode(Type type, object obj, ExprNodeKind kind, IEnumerable<int> children) =>
        AddNode(type, obj, ExpressionType.Extension, kind, children, 0);

    private sealed class Builder
    {
        private readonly Dictionary<object, int> _parameterIds = new(ReferenceEqComparer.Instance);
        private readonly Dictionary<object, int> _labelIds = new(ReferenceEqComparer.Instance);
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
                        return _tree.AddRawExpressionNode(expression.Type, new ParameterData(GetId(_parameterIds, parameter), parameter.Name, parameter.IsByRef), expression.NodeType);
                    }
                case ExpressionType.Lambda:
                    {
                        var lambda = (System.Linq.Expressions.LambdaExpression)expression;
                        var children = new List<int>(lambda.Parameters.Count + 1) { AddExpression(lambda.Body) };
                        for (var i = 0; i < lambda.Parameters.Count; ++i)
                            children.Add(AddExpression(lambda.Parameters[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Block:
                    {
                        var block = (System.Linq.Expressions.BlockExpression)expression;
                        var children = new List<int>(block.Variables.Count + block.Expressions.Count);
                        for (var i = 0; i < block.Variables.Count; ++i)
                            children.Add(AddExpression(block.Variables[i]));
                        for (var i = 0; i < block.Expressions.Count; ++i)
                            children.Add(AddExpression(block.Expressions[i]));
                        return _tree.AddRawExpressionNode(expression.Type, new BlockData(block.Variables.Count), expression.NodeType, children);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = (System.Linq.Expressions.MemberExpression)expression;
                        return _tree.AddRawExpressionNode(expression.Type, member.Member, expression.NodeType,
                            member.Expression != null ? new List<int>(1) { AddExpression(member.Expression) } : null);
                    }
                case ExpressionType.Call:
                    {
                        var call = (System.Linq.Expressions.MethodCallExpression)expression;
                        var children = new List<int>(call.Arguments.Count + (call.Object != null ? 1 : 0));
                        if (call.Object != null)
                            children.Add(AddExpression(call.Object));
                        for (var i = 0; i < call.Arguments.Count; ++i)
                            children.Add(AddExpression(call.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, call.Method, expression.NodeType, children);
                    }
                case ExpressionType.New:
                    {
                        var @new = (System.Linq.Expressions.NewExpression)expression;
                        var children = new List<int>(@new.Arguments.Count);
                        for (var i = 0; i < @new.Arguments.Count; ++i)
                            children.Add(AddExpression(@new.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, @new.Constructor, expression.NodeType, children);
                    }
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var array = (System.Linq.Expressions.NewArrayExpression)expression;
                        var children = new List<int>(array.Expressions.Count);
                        for (var i = 0; i < array.Expressions.Count; ++i)
                            children.Add(AddExpression(array.Expressions[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Invoke:
                    {
                        var invoke = (System.Linq.Expressions.InvocationExpression)expression;
                        var children = new List<int>(invoke.Arguments.Count + 1) { AddExpression(invoke.Expression) };
                        for (var i = 0; i < invoke.Arguments.Count; ++i)
                            children.Add(AddExpression(invoke.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Index:
                    {
                        var index = (System.Linq.Expressions.IndexExpression)expression;
                        var children = new List<int>(index.Arguments.Count + (index.Object != null ? 1 : 0));
                        if (index.Object != null)
                            children.Add(AddExpression(index.Object));
                        for (var i = 0; i < index.Arguments.Count; ++i)
                            children.Add(AddExpression(index.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, index.Indexer, expression.NodeType, children);
                    }
                case ExpressionType.Conditional:
                    {
                        var conditional = (System.Linq.Expressions.ConditionalExpression)expression;
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType,
                            new List<int>(3)
                            {
                                AddExpression(conditional.Test),
                                AddExpression(conditional.IfTrue),
                                AddExpression(conditional.IfFalse),
                            });
                    }
                case ExpressionType.Loop:
                    {
                        var loop = (System.Linq.Expressions.LoopExpression)expression;
                        var data = new LoopData(loop.BreakLabel != null, loop.ContinueLabel != null);
                        var children = new List<int>(3) { AddExpression(loop.Body) };
                        if (loop.BreakLabel != null)
                            children.Add(AddLabelTarget(loop.BreakLabel));
                        if (loop.ContinueLabel != null)
                            children.Add(AddLabelTarget(loop.ContinueLabel));
                        return _tree.AddRawExpressionNode(expression.Type, data, expression.NodeType, children);
                    }
                case ExpressionType.Goto:
                    {
                        var @goto = (System.Linq.Expressions.GotoExpression)expression;
                        var children = new List<int>(2) { AddLabelTarget(@goto.Target) };
                        if (@goto.Value != null)
                            children.Add(AddExpression(@goto.Value));
                        return _tree.AddRawExpressionNode(expression.Type, @goto.Kind, expression.NodeType, children);
                    }
                case ExpressionType.Label:
                    {
                        var label = (System.Linq.Expressions.LabelExpression)expression;
                        var children = new List<int>(2) { AddLabelTarget(label.Target) };
                        if (label.DefaultValue != null)
                            children.Add(AddExpression(label.DefaultValue));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Switch:
                    {
                        var @switch = (System.Linq.Expressions.SwitchExpression)expression;
                        var children = new List<int>(@switch.Cases.Count + 2) { AddExpression(@switch.SwitchValue) };
                        if (@switch.DefaultBody != null)
                            children.Add(AddExpression(@switch.DefaultBody));
                        for (var i = 0; i < @switch.Cases.Count; ++i)
                            children.Add(AddSwitchCase(@switch.Cases[i]));
                        return _tree.AddRawExpressionNode(expression.Type, new SwitchData(@switch.DefaultBody != null, @switch.Comparison), expression.NodeType, children);
                    }
                case ExpressionType.Try:
                    {
                        var @try = (System.Linq.Expressions.TryExpression)expression;
                        var children = new List<int>(@try.Handlers.Count + 2) { AddExpression(@try.Body) };
                        if (@try.Fault != null)
                            children.Add(AddExpression(@try.Fault));
                        else if (@try.Finally != null)
                            children.Add(AddExpression(@try.Finally));
                        for (var i = 0; i < @try.Handlers.Count; ++i)
                            children.Add(AddCatchBlock(@try.Handlers[i]));
                        return _tree.AddRawExpressionNode(expression.Type, new TryData(@try.Finally != null, @try.Fault != null), expression.NodeType, children);
                    }
                case ExpressionType.MemberInit:
                    {
                        var memberInit = (System.Linq.Expressions.MemberInitExpression)expression;
                        var children = new List<int>(memberInit.Bindings.Count + 1) { AddExpression(memberInit.NewExpression) };
                        for (var i = 0; i < memberInit.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberInit.Bindings[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.ListInit:
                    {
                        var listInit = (System.Linq.Expressions.ListInitExpression)expression;
                        var children = new List<int>(listInit.Initializers.Count + 1) { AddExpression(listInit.NewExpression) };
                        for (var i = 0; i < listInit.Initializers.Count; ++i)
                            children.Add(AddElementInit(listInit.Initializers[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    {
                        var typeBinary = (System.Linq.Expressions.TypeBinaryExpression)expression;
                        return _tree.AddRawExpressionNode(expression.Type, typeBinary.TypeOperand, expression.NodeType,
                            new List<int>(1) { AddExpression(typeBinary.Expression) });
                    }
                case ExpressionType.Dynamic:
                    {
                        var dynamic = (System.Linq.Expressions.DynamicExpression)expression;
                        var children = new List<int>(dynamic.Arguments.Count);
                        for (var i = 0; i < dynamic.Arguments.Count; ++i)
                            children.Add(AddExpression(dynamic.Arguments[i]));
                        return _tree.AddRawExpressionNode(expression.Type, new DynamicData(dynamic.DelegateType, dynamic.Binder), expression.NodeType, children);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var runtime = (System.Linq.Expressions.RuntimeVariablesExpression)expression;
                        var children = new List<int>(runtime.Variables.Count);
                        for (var i = 0; i < runtime.Variables.Count; ++i)
                            children.Add(AddExpression(runtime.Variables[i]));
                        return _tree.AddRawExpressionNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var debug = (System.Linq.Expressions.DebugInfoExpression)expression;
                        return _tree.AddRawExpressionNode(expression.Type,
                            new DebugInfoData(debug.Document.FileName, debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn),
                            expression.NodeType);
                    }
                default:
                    if (expression is System.Linq.Expressions.UnaryExpression unary)
                    {
                        return _tree.AddRawExpressionNode(expression.Type, unary.Method, expression.NodeType,
                            new List<int>(1) { AddExpression(unary.Operand) });
                    }

                    if (expression is System.Linq.Expressions.BinaryExpression binary)
                    {
                        var children = new List<int>(binary.Conversion != null ? 3 : 2)
                        {
                            AddExpression(binary.Left),
                            AddExpression(binary.Right)
                        };
                        if (binary.Conversion != null)
                            children.Add(AddExpression(binary.Conversion));
                        return _tree.AddRawExpressionNode(expression.Type, new BinaryData(binary.Method, binary.IsLiftedToNull), expression.NodeType, children);
                    }

                    throw new NotSupportedException($"Flattening of `ExpressionType.{expression.NodeType}` is not supported yet.");
            }
        }

        private int AddConstant(System.Linq.Expressions.ConstantExpression constant)
        {
            if (ShouldInlineConstant(constant.Value, constant.Type))
                return _tree.AddRawExpressionNode(constant.Type, constant.Value, constant.NodeType);

            var constantIndex = _tree.ClosureConstants.Add(constant.Value);
            return _tree.AddRawExpressionNodeWithChildIndex(constant.Type, ClosureConstantMarker, constant.NodeType, constantIndex);
        }

        private int AddSwitchCase(SysSwitchCase switchCase)
        {
            var children = new List<int>(switchCase.TestValues.Count + 1);
            for (var i = 0; i < switchCase.TestValues.Count; ++i)
                children.Add(AddExpression(switchCase.TestValues[i]));
            children.Add(AddExpression(switchCase.Body));
            return _tree.AddRawAuxNode(switchCase.Body.Type, null, ExprNodeKind.SwitchCase, children);
        }

        private int AddCatchBlock(SysCatchBlock catchBlock)
        {
            var children = new List<int>(3);
            if (catchBlock.Variable != null)
                children.Add(AddExpression(catchBlock.Variable));
            children.Add(AddExpression(catchBlock.Body));
            if (catchBlock.Filter != null)
                children.Add(AddExpression(catchBlock.Filter));
            return _tree.AddRawAuxNode(catchBlock.Test, new CatchData(catchBlock.Variable != null, catchBlock.Filter != null),
                ExprNodeKind.CatchBlock, children);
        }

        private int AddLabelTarget(SysLabelTarget target) =>
            _tree.AddRawAuxNode(target.Type, new LabelTargetData(GetId(_labelIds, target), target.Name), ExprNodeKind.LabelTarget);

        private int AddMemberBinding(SysMemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberAssignment,
                        new List<int>(1) { AddExpression(((System.Linq.Expressions.MemberAssignment)binding).Expression) });
                case MemberBindingType.MemberBinding:
                    {
                        var memberBinding = (System.Linq.Expressions.MemberMemberBinding)binding;
                        var children = new List<int>(memberBinding.Bindings.Count);
                        for (var i = 0; i < memberBinding.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberBinding.Bindings[i]));
                        return _tree.AddRawAuxNode(GetMemberType(binding.Member), binding.Member, ExprNodeKind.MemberMemberBinding, children);
                    }
                case MemberBindingType.ListBinding:
                    {
                        var listBinding = (System.Linq.Expressions.MemberListBinding)binding;
                        var children = new List<int>(listBinding.Initializers.Count);
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
            var children = new List<int>(init.Arguments.Count);
            for (var i = 0; i < init.Arguments.Count; ++i)
                children.Add(AddExpression(init.Arguments[i]));
            return _tree.AddRawAuxNode(init.AddMethod.DeclaringType, init.AddMethod, ExprNodeKind.ElementInit, children);
        }

        private static int GetId(Dictionary<object, int> ids, object item)
        {
            if (ids.TryGetValue(item, out var id))
                return id;

            id = ids.Count + 1;
            ids[item] = id;
            return id;
        }

        private static Type GetMemberType(System.Reflection.MemberInfo member) => member switch
        {
            System.Reflection.FieldInfo field => field.FieldType,
            System.Reflection.PropertyInfo property => property.PropertyType,
            _ => typeof(object)
        };
    }

    private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, IEnumerable<int> children, int childIdx)
    {
        var nodeIndex = Nodes.Add(new ExprNode(type, obj, nodeType, kind, childIdx, 0, 0));
        if (children == null)
            return nodeIndex;

        using var enumerator = children.GetEnumerator();
        if (!enumerator.MoveNext())
            return nodeIndex;

        var firstChildIndex = enumerator.Current;
        var previousChildIndex = firstChildIndex;
        var childCount = 1;
        while (enumerator.MoveNext())
        {
            ref var child = ref Nodes[previousChildIndex];
            child.SetNextIdx(enumerator.Current);
            previousChildIndex = enumerator.Current;
            ++childCount;
        }

        ref var node = ref Nodes[nodeIndex];
        node.SetChildInfo(firstChildIndex, childCount);
        return nodeIndex;
    }

    private static bool ShouldInlineConstant(object value, Type type)
    {
        if (value == null || value is string || value is Type)
            return true;

        if (type.IsEnum)
            return true;

        return Type.GetTypeCode(type) != TypeCode.Object;
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

    private static Type GetArrayElementType(Type arrayType, int rank)
    {
        var elementType = arrayType;
        for (var i = 0; i < rank; ++i)
            elementType = elementType.GetElementType();
        return elementType ?? typeof(object);
    }

    private int CloneChild(int index)
    {
        ref var node = ref Nodes[index];
        return node.ChildCount == 0
            ? AddNode(node.Type, node.Obj, node.NodeType, node.Kind, null, node.ChildIdx)
            : index;
    }

    private int[] CloneChildrenToArray(IEnumerable<int> children)
    {
        if (children == null)
            return Array.Empty<int>();

        var cloned = new List<int>();
        foreach (var child in children)
            cloned.Add(CloneChild(child));
        return cloned.ToArray();
    }

    private static IEnumerable<int> Single(int item)
    {
        yield return item;
    }

    private static int[] Prepend(int first, int[] rest)
    {
        if (rest == null || rest.Length == 0)
            return new[] { first };

        var items = new int[rest.Length + 1];
        items[0] = first;
        Array.Copy(rest, 0, items, 1, rest.Length);
        return items;
    }

    private readonly struct Reader
    {
        private readonly ExprTree _tree;
        private readonly Dictionary<int, SysParameterExpression> _parametersById;
        private readonly Dictionary<int, SysLabelTarget> _labelsById;

        public Reader(ExprTree tree)
        {
            _tree = tree;
            _parametersById = new Dictionary<int, SysParameterExpression>();
            _labelsById = new Dictionary<int, SysLabelTarget>();
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        public SysExpr ReadExpression(int index)
        {
            ref var node = ref _tree.Nodes[index];
            if (node.Kind != ExprNodeKind.Expression)
                throw new InvalidOperationException($"Node at index {index} is not an expression node.");

            switch (node.NodeType)
            {
                case ExpressionType.Constant:
                    return SysExpr.Constant(ReferenceEquals(node.Obj, ClosureConstantMarker)
                        ? _tree.ClosureConstants[node.ChildIdx]
                        : node.Obj, node.Type);
                case ExpressionType.Default:
                    return SysExpr.Default(node.Type);
                case ExpressionType.Parameter:
                    {
                        var data = (ParameterData)node.Obj;
                        if (_parametersById.TryGetValue(data.Id, out var parameter))
                            return parameter;

                        var parameterType = data.IsByRef && !node.Type.IsByRef ? node.Type.MakeByRefType() : node.Type;
                        parameter = SysExpr.Parameter(parameterType, data.Name);
                        _parametersById[data.Id] = parameter;
                        return parameter;
                    }
                case ExpressionType.Lambda:
                    {
                        var children = GetChildren(index);
                        var body = ReadExpression(children[0]);
                        var parameters = new SysParameterExpression[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            parameters[i - 1] = (SysParameterExpression)ReadExpression(children[i]);
                        return SysExpr.Lambda(node.Type, body, parameters);
                    }
                case ExpressionType.Block:
                    {
                        var data = (BlockData)node.Obj;
                        var children = GetChildren(index);
                        var variables = new SysParameterExpression[data.VariableCount];
                        for (var i = 0; i < variables.Length; ++i)
                            variables[i] = (SysParameterExpression)ReadExpression(children[i]);
                        var expressions = new SysExpr[children.Count - data.VariableCount];
                        for (var i = data.VariableCount; i < children.Count; ++i)
                            expressions[i - data.VariableCount] = ReadExpression(children[i]);
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
                        var data = (LoopData)node.Obj;
                        var children = GetChildren(index);
                        var childIndex = 1;
                        var breakLabel = data.HasBreak ? ReadLabelTarget(children[childIndex++]) : null;
                        var continueLabel = data.HasContinue ? ReadLabelTarget(children[childIndex]) : null;
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
                        var data = (SwitchData)node.Obj;
                        var children = GetChildren(index);
                        var childIndex = 1;
                        var defaultBody = data.HasDefault ? ReadExpression(children[childIndex++]) : null;
                        var cases = new SysSwitchCase[children.Count - childIndex];
                        for (var i = childIndex; i < children.Count; ++i)
                            cases[i - childIndex] = ReadSwitchCase(children[i]);
                        return SysExpr.Switch(node.Type, ReadExpression(children[0]), defaultBody, data.Comparison, cases);
                    }
                case ExpressionType.Try:
                    {
                        var data = (TryData)node.Obj;
                        var children = GetChildren(index);
                        var childIndex = 1;
                        if (data.HasFault)
                            return SysExpr.TryFault(ReadExpression(children[0]), ReadExpression(children[1]));

                        var @finally = data.HasFinally ? ReadExpression(children[childIndex++]) : null;
                        var handlers = new SysCatchBlock[children.Count - childIndex];
                        for (var i = childIndex; i < children.Count; ++i)
                            handlers[i - childIndex] = ReadCatchBlock(children[i]);
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
                        var data = (DynamicData)node.Obj;
                        return SysExpr.MakeDynamic(data.DelegateType, data.Binder, ReadExpressions(GetChildren(index)));
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
                        var data = (DebugInfoData)node.Obj;
                        return SysExpr.DebugInfo(SysExpr.SymbolDocument(data.FileName),
                            data.StartLine, data.StartColumn, data.EndLine, data.EndColumn);
                    }
                default:
                    if (node.ChildCount == 1)
                    {
                        var method = node.Obj as System.Reflection.MethodInfo;
                        return SysExpr.MakeUnary(node.NodeType, ReadExpression(GetChildren(index)[0]), node.Type, method);
                    }

                    if (node.ChildCount >= 2)
                    {
                        var data = node.Obj as BinaryData;
                        var children = GetChildren(index);
                        var conversion = children.Count > 2 ? (System.Linq.Expressions.LambdaExpression)ReadExpression(children[2]) : null;
                        return SysExpr.MakeBinary(node.NodeType, ReadExpression(children[0]), ReadExpression(children[1]),
                            data != null && data.IsLiftedToNull, data?.Method, conversion);
                    }

                    throw new NotSupportedException($"Reconstruction of `ExpressionType.{node.NodeType}` is not supported yet.");
            }
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysSwitchCase ReadSwitchCase(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Kind == ExprNodeKind.SwitchCase);
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
            Debug.Assert(node.Kind == ExprNodeKind.CatchBlock);
            var data = (CatchData)node.Obj;
            var children = GetChildren(index);
            var childIndex = 0;
            var variable = data.HasVariable ? (SysParameterExpression)ReadExpression(children[childIndex++]) : null;
            var body = ReadExpression(children[childIndex++]);
            var filter = data.HasFilter ? ReadExpression(children[childIndex]) : null;
            return SysExpr.MakeCatchBlock(node.Type, variable, body, filter);
        }

        private SysLabelTarget ReadLabelTarget(int index)
        {
            ref var node = ref _tree.Nodes[index];
            Debug.Assert(node.Kind == ExprNodeKind.LabelTarget);
            var data = (LabelTargetData)node.Obj;
            if (_labelsById.TryGetValue(data.Id, out var label))
                return label;

            label = SysExpr.Label(node.Type, data.Name);
            _labelsById[data.Id] = label;
            return label;
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
            Debug.Assert(node.Kind == ExprNodeKind.ElementInit);
            return SysExpr.ElementInit((System.Reflection.MethodInfo)node.Obj, ReadExpressions(GetChildren(index)));
        }

        private List<int> GetChildren(int index)
        {
            ref var node = ref _tree.Nodes[index];
            var count = node.ChildCount;
            var children = new List<int>(count);
            var childIndex = node.ChildIdx;
            for (var i = 0; i < count; ++i)
            {
                children.Add(childIndex);
                childIndex = _tree.Nodes[childIndex].NextIdx;
            }
            return children;
        }

        [RequiresUnreferencedCode(FastExpressionCompiler.LightExpression.Trimming.Message)]
        private SysExpr[] ReadExpressions(List<int> childIndexes)
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

    private sealed class ParameterData
    {
        public readonly int Id;
        public readonly string Name;
        public readonly bool IsByRef;

        public ParameterData(int id, string name, bool isByRef)
        {
            Id = id;
            Name = name;
            IsByRef = isByRef;
        }
    }

    private sealed class LabelTargetData
    {
        public readonly int Id;
        public readonly string Name;

        public LabelTargetData(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    private sealed class BlockData
    {
        public readonly int VariableCount;
        public BlockData(int variableCount) => VariableCount = variableCount;
    }

    private sealed class SwitchData
    {
        public readonly bool HasDefault;
        public readonly System.Reflection.MethodInfo Comparison;

        public SwitchData(bool hasDefault, System.Reflection.MethodInfo comparison)
        {
            HasDefault = hasDefault;
            Comparison = comparison;
        }
    }

    private sealed class TryData
    {
        public readonly bool HasFinally;
        public readonly bool HasFault;

        public TryData(bool hasFinally, bool hasFault)
        {
            HasFinally = hasFinally;
            HasFault = hasFault;
        }
    }

    private sealed class LoopData
    {
        public readonly bool HasBreak;
        public readonly bool HasContinue;

        public LoopData(bool hasBreak, bool hasContinue)
        {
            HasBreak = hasBreak;
            HasContinue = hasContinue;
        }
    }

    private sealed class CatchData
    {
        public readonly bool HasVariable;
        public readonly bool HasFilter;

        public CatchData(bool hasVariable, bool hasFilter)
        {
            HasVariable = hasVariable;
            HasFilter = hasFilter;
        }
    }

    private sealed class BinaryData
    {
        public readonly System.Reflection.MethodInfo Method;
        public readonly bool IsLiftedToNull;

        public BinaryData(System.Reflection.MethodInfo method, bool isLiftedToNull)
        {
            Method = method;
            IsLiftedToNull = isLiftedToNull;
        }
    }

    private sealed class DynamicData
    {
        public readonly Type DelegateType;
        public readonly CallSiteBinder Binder;

        public DynamicData(Type delegateType, CallSiteBinder binder)
        {
            DelegateType = delegateType;
            Binder = binder;
        }
    }

    private sealed class DebugInfoData
    {
        public readonly string FileName;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public DebugInfoData(string fileName, int startLine, int startColumn, int endLine, int endColumn)
        {
            FileName = fileName;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }

    private sealed class ReferenceEqComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqComparer Instance = new();
        public new bool Equals(object x, object y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}

public static class FlatExpressionExtensions
{
    public static ExprTree ToFlatExpression(this SysExpr expression) => ExprTree.FromExpression(expression);

    public static ExprTree ToFlatExpression(this LightExpression expression) => ExprTree.FromLightExpression(expression);
}
