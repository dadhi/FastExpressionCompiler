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
                    return AddNode(expression.Type, null, expression.NodeType);
                case ExpressionType.Parameter:
                    {
                        var parameter = (SysParameterExpression)expression;
                        return AddNode(expression.Type, new ParameterData(GetId(_parameterIds, parameter), parameter.Name, parameter.IsByRef), expression.NodeType);
                    }
                case ExpressionType.Lambda:
                    {
                        var lambda = (System.Linq.Expressions.LambdaExpression)expression;
                        var children = new List<int>(lambda.Parameters.Count + 1) { AddExpression(lambda.Body) };
                        for (var i = 0; i < lambda.Parameters.Count; ++i)
                            children.Add(AddExpression(lambda.Parameters[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Block:
                    {
                        var block = (System.Linq.Expressions.BlockExpression)expression;
                        var children = new List<int>(block.Variables.Count + block.Expressions.Count);
                        for (var i = 0; i < block.Variables.Count; ++i)
                            children.Add(AddExpression(block.Variables[i]));
                        for (var i = 0; i < block.Expressions.Count; ++i)
                            children.Add(AddExpression(block.Expressions[i]));
                        return AddNode(expression.Type, new BlockData(block.Variables.Count), expression.NodeType, children);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var member = (System.Linq.Expressions.MemberExpression)expression;
                        return AddNode(expression.Type, member.Member, expression.NodeType,
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
                        return AddNode(expression.Type, call.Method, expression.NodeType, children);
                    }
                case ExpressionType.New:
                    {
                        var @new = (System.Linq.Expressions.NewExpression)expression;
                        var children = new List<int>(@new.Arguments.Count);
                        for (var i = 0; i < @new.Arguments.Count; ++i)
                            children.Add(AddExpression(@new.Arguments[i]));
                        return AddNode(expression.Type, @new.Constructor, expression.NodeType, children);
                    }
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var array = (System.Linq.Expressions.NewArrayExpression)expression;
                        var children = new List<int>(array.Expressions.Count);
                        for (var i = 0; i < array.Expressions.Count; ++i)
                            children.Add(AddExpression(array.Expressions[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Invoke:
                    {
                        var invoke = (System.Linq.Expressions.InvocationExpression)expression;
                        var children = new List<int>(invoke.Arguments.Count + 1) { AddExpression(invoke.Expression) };
                        for (var i = 0; i < invoke.Arguments.Count; ++i)
                            children.Add(AddExpression(invoke.Arguments[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Index:
                    {
                        var index = (System.Linq.Expressions.IndexExpression)expression;
                        var children = new List<int>(index.Arguments.Count + (index.Object != null ? 1 : 0));
                        if (index.Object != null)
                            children.Add(AddExpression(index.Object));
                        for (var i = 0; i < index.Arguments.Count; ++i)
                            children.Add(AddExpression(index.Arguments[i]));
                        return AddNode(expression.Type, index.Indexer, expression.NodeType, children);
                    }
                case ExpressionType.Conditional:
                    {
                        var conditional = (System.Linq.Expressions.ConditionalExpression)expression;
                        return AddNode(expression.Type, null, expression.NodeType,
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
                        return AddNode(expression.Type, data, expression.NodeType, children);
                    }
                case ExpressionType.Goto:
                    {
                        var @goto = (System.Linq.Expressions.GotoExpression)expression;
                        var children = new List<int>(2) { AddLabelTarget(@goto.Target) };
                        if (@goto.Value != null)
                            children.Add(AddExpression(@goto.Value));
                        return AddNode(expression.Type, @goto.Kind, expression.NodeType, children);
                    }
                case ExpressionType.Label:
                    {
                        var label = (System.Linq.Expressions.LabelExpression)expression;
                        var children = new List<int>(2) { AddLabelTarget(label.Target) };
                        if (label.DefaultValue != null)
                            children.Add(AddExpression(label.DefaultValue));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.Switch:
                    {
                        var @switch = (System.Linq.Expressions.SwitchExpression)expression;
                        var children = new List<int>(@switch.Cases.Count + 2) { AddExpression(@switch.SwitchValue) };
                        if (@switch.DefaultBody != null)
                            children.Add(AddExpression(@switch.DefaultBody));
                        for (var i = 0; i < @switch.Cases.Count; ++i)
                            children.Add(AddSwitchCase(@switch.Cases[i]));
                        return AddNode(expression.Type, new SwitchData(@switch.DefaultBody != null, @switch.Comparison), expression.NodeType, children);
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
                        return AddNode(expression.Type, new TryData(@try.Finally != null, @try.Fault != null), expression.NodeType, children);
                    }
                case ExpressionType.MemberInit:
                    {
                        var memberInit = (System.Linq.Expressions.MemberInitExpression)expression;
                        var children = new List<int>(memberInit.Bindings.Count + 1) { AddExpression(memberInit.NewExpression) };
                        for (var i = 0; i < memberInit.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberInit.Bindings[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.ListInit:
                    {
                        var listInit = (System.Linq.Expressions.ListInitExpression)expression;
                        var children = new List<int>(listInit.Initializers.Count + 1) { AddExpression(listInit.NewExpression) };
                        for (var i = 0; i < listInit.Initializers.Count; ++i)
                            children.Add(AddElementInit(listInit.Initializers[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.TypeIs:
                case ExpressionType.TypeEqual:
                    {
                        var typeBinary = (System.Linq.Expressions.TypeBinaryExpression)expression;
                        return AddNode(expression.Type, typeBinary.TypeOperand, expression.NodeType,
                            new List<int>(1) { AddExpression(typeBinary.Expression) });
                    }
                case ExpressionType.Dynamic:
                    {
                        var dynamic = (System.Linq.Expressions.DynamicExpression)expression;
                        var children = new List<int>(dynamic.Arguments.Count);
                        for (var i = 0; i < dynamic.Arguments.Count; ++i)
                            children.Add(AddExpression(dynamic.Arguments[i]));
                        return AddNode(expression.Type, new DynamicData(dynamic.DelegateType, dynamic.Binder), expression.NodeType, children);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var runtime = (System.Linq.Expressions.RuntimeVariablesExpression)expression;
                        var children = new List<int>(runtime.Variables.Count);
                        for (var i = 0; i < runtime.Variables.Count; ++i)
                            children.Add(AddExpression(runtime.Variables[i]));
                        return AddNode(expression.Type, null, expression.NodeType, children);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var debug = (System.Linq.Expressions.DebugInfoExpression)expression;
                        return AddNode(expression.Type,
                            new DebugInfoData(debug.Document.FileName, debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn),
                            expression.NodeType);
                    }
                default:
                    if (expression is System.Linq.Expressions.UnaryExpression unary)
                    {
                        return AddNode(expression.Type, unary.Method, expression.NodeType,
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
                        return AddNode(expression.Type, new BinaryData(binary.Method, binary.IsLiftedToNull), expression.NodeType, children);
                    }

                    throw new NotSupportedException($"Flattening of `ExpressionType.{expression.NodeType}` is not supported yet.");
            }
        }

        private int AddConstant(System.Linq.Expressions.ConstantExpression constant)
        {
            if (ShouldInlineConstant(constant.Value, constant.Type))
                return AddNode(constant.Type, constant.Value, constant.NodeType);

            var constantIndex = _tree.ClosureConstants.Add(constant.Value);
            return AddNode(constant.Type, ClosureConstantMarker, constant.NodeType, ExprNodeKind.Expression, childIdx: constantIndex);
        }

        private int AddSwitchCase(SysSwitchCase switchCase)
        {
            var children = new List<int>(switchCase.TestValues.Count + 1);
            for (var i = 0; i < switchCase.TestValues.Count; ++i)
                children.Add(AddExpression(switchCase.TestValues[i]));
            children.Add(AddExpression(switchCase.Body));
            return AddNode(switchCase.Body.Type, null, ExpressionType.Extension, ExprNodeKind.SwitchCase, children);
        }

        private int AddCatchBlock(SysCatchBlock catchBlock)
        {
            var children = new List<int>(3);
            if (catchBlock.Variable != null)
                children.Add(AddExpression(catchBlock.Variable));
            children.Add(AddExpression(catchBlock.Body));
            if (catchBlock.Filter != null)
                children.Add(AddExpression(catchBlock.Filter));
            return AddNode(catchBlock.Test, new CatchData(catchBlock.Variable != null, catchBlock.Filter != null),
                ExpressionType.Extension, ExprNodeKind.CatchBlock, children);
        }

        private int AddLabelTarget(SysLabelTarget target) =>
            AddNode(target.Type, new LabelTargetData(GetId(_labelIds, target), target.Name), ExpressionType.Extension, ExprNodeKind.LabelTarget);

        private int AddMemberBinding(SysMemberBinding binding)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    return AddNode(GetMemberType(binding.Member), binding.Member, ExpressionType.Extension, ExprNodeKind.MemberAssignment,
                        new List<int>(1) { AddExpression(((System.Linq.Expressions.MemberAssignment)binding).Expression) });
                case MemberBindingType.MemberBinding:
                    {
                        var memberBinding = (System.Linq.Expressions.MemberMemberBinding)binding;
                        var children = new List<int>(memberBinding.Bindings.Count);
                        for (var i = 0; i < memberBinding.Bindings.Count; ++i)
                            children.Add(AddMemberBinding(memberBinding.Bindings[i]));
                        return AddNode(GetMemberType(binding.Member), binding.Member, ExpressionType.Extension, ExprNodeKind.MemberMemberBinding, children);
                    }
                case MemberBindingType.ListBinding:
                    {
                        var listBinding = (System.Linq.Expressions.MemberListBinding)binding;
                        var children = new List<int>(listBinding.Initializers.Count);
                        for (var i = 0; i < listBinding.Initializers.Count; ++i)
                            children.Add(AddElementInit(listBinding.Initializers[i]));
                        return AddNode(GetMemberType(binding.Member), binding.Member, ExpressionType.Extension, ExprNodeKind.MemberListBinding, children);
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
            return AddNode(init.AddMethod.DeclaringType, init.AddMethod, ExpressionType.Extension, ExprNodeKind.ElementInit, children);
        }

        private int AddNode(Type type, object obj, ExpressionType nodeType, List<int> children = null) =>
            AddNode(type, obj, nodeType, ExprNodeKind.Expression, children);

        private int AddNode(Type type, object obj, ExpressionType nodeType, ExprNodeKind kind, List<int> children = null, int childIdx = 0)
        {
            var nodeIndex = _tree.Nodes.Add(new ExprNode(type, obj, nodeType, kind, childIdx, 0, 0));
            if (children != null && children.Count != 0)
            {
                for (var i = 0; i < children.Count - 1; ++i)
                {
                    ref var child = ref _tree.Nodes[children[i]];
                    child.SetNextIdx(children[i + 1]);
                }

                ref var node = ref _tree.Nodes[nodeIndex];
                node.SetChildInfo(children[0], children.Count);
            }
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
