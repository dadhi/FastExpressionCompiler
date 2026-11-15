#nullable disable
namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler.LightExpression.ImTools;
using LExpr = FastExpressionCompiler.LightExpression.Expression;
using LParameter = FastExpressionCompiler.LightExpression.ParameterExpression;
using LLabelTarget = FastExpressionCompiler.LightExpression.LabelTarget;
using LSwitchCase = FastExpressionCompiler.LightExpression.SwitchCase;
using LCatchBlock = FastExpressionCompiler.LightExpression.CatchBlock;
using LMemberBinding = FastExpressionCompiler.LightExpression.MemberBinding;
using LElementInit = FastExpressionCompiler.LightExpression.ElementInit;
using ChildIdxs = LightExpression.ImTools.SmallList<ushort, LightExpression.ImTools.Stack8<ushort>, LightExpression.ImTools.NoArrayPool<ushort>>;

public partial struct ExprTree
{
    /// <summary>Reconstructs the flat tree as a LightExpression tree directly, without the System.Linq expressions in between.
    /// Mirrors <c>SysExprBuilder</c>: keep the two in sync.</summary>
    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    public LightExpression.Expression ToLightExpression() =>
        Nodes.Count != 0
            ? new LightExprBuilder(ref this).ReadExpression(RootIdx)
            : throw new InvalidOperationException("Flat expression tree is empty.");

    private ref struct LightExprBuilder
    {
        private ref readonly ExprTree _tree;
        private SmallMap16<int, LParameter, IntEq> _parametersById;
        private SmallMap16<int, LLabelTarget, IntEq> _labelsById;

        public LightExprBuilder(ref readonly ExprTree tree)
        {
            _tree = ref tree;
            _parametersById = default;
            _labelsById = default;
        }

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        public LExpr ReadExpression(int idx)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            if (node.Kind != ExprNodeKind.Expression)
                throw new InvalidOperationException($"Node at idx {idx} is not an expression node.");

            switch (node.NodeType)
            {
                case ExpressionType.Constant:
                    if (ReferenceEquals(node.Obj, ClosureConstantMarker))
                        return LExpr.Constant(_tree.ClosureConstants.GetSurePresentRef(node.ChildIdx), node.Type);
                    if (ReferenceEquals(node.Obj, ExprNode.InlineValueMarker))
                        return LExpr.Constant(ReadInlineValue(node.Type, node.InlineValue), node.Type);
                    return LExpr.Constant(node.Obj, node.Type);
                case ExpressionType.Default:
                    return LExpr.Default(node.Type);
                case ExpressionType.Parameter:
                    {
                        ref var parameter = ref _parametersById.Map.AddOrGetValueRef(node.ChildIdx, out var found);
                        if (found)
                            return parameter;

                        var parameterType = node.HasFlag(ParameterByRefFlag) && !node.Type.IsByRef ? node.Type.MakeByRefType() : node.Type;
                        return parameter = LExpr.Parameter(parameterType, (string)node.Obj);
                    }
                case ExpressionType.Lambda:
                    {
                        // Layout: children[0] = body, children[1..n] = parameter decl nodes.
                        var children = GetChildren(idx);
                        var body = ReadExpression(children.GetSurePresentRef(0));
                        var parameters = new LParameter[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            parameters[i - 1] = (LParameter)ReadExpression(children.GetSurePresentRef(i));
                        return LExpr.Lambda(node.Type, body, parameters);
                    }
                case ExpressionType.Block:
                    {
                        // Layout: children[0] = BlockExprs; children[1..] = variable decls.
                        var children = GetChildren(idx);
                        var variables = new LParameter[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            variables[i - 1] = (LParameter)ReadExpression(children.GetSurePresentRef(i));
                        var expressionIdxs = GetChildren(children.GetSurePresentRef(0));
                        var expressions = new LExpr[expressionIdxs.Count];
                        for (var i = 0; i < expressions.Length; ++i)
                            expressions[i] = ReadExpression(expressionIdxs.GetSurePresentRef(i));
                        return LExpr.Block(node.Type, variables, expressions);
                    }
                case ExpressionType.MemberAccess:
                    {
                        var children = GetChildren(idx);
                        return LExpr.MakeMemberAccess(children.Count != 0 ? ReadExpression(children.GetSurePresentRef(0)) : null, (MemberInfo)node.Obj);
                    }
                case ExpressionType.Call:
                    {
                        var method = (MethodInfo)node.Obj;
                        var children = GetChildren(idx);
                        var hasInstance = !method.IsStatic;
                        var instance = hasInstance ? ReadExpression(children.GetSurePresentRef(0)) : null;
                        var arguments = new LExpr[children.Count - (hasInstance ? 1 : 0)];
                        for (var i = hasInstance ? 1 : 0; i < children.Count; ++i)
                            arguments[i - (hasInstance ? 1 : 0)] = ReadExpression(children.GetSurePresentRef(i));
                        return LExpr.Call(instance, method, arguments);
                    }
                case ExpressionType.New:
                    {
                        var arguments = ReadExpressions(GetChildren(idx));
                        return node.Obj is ConstructorInfo ctor ? LExpr.New(ctor, arguments) : LExpr.New(node.Type);
                    }
                case ExpressionType.NewArrayInit:
                    return LExpr.NewArrayInit(node.Type.GetElementType(), ReadExpressions(GetChildren(idx)));
                case ExpressionType.NewArrayBounds:
                    return LExpr.NewArrayBounds(node.Type.GetElementType(), ReadExpressions(GetChildren(idx)));
                case ExpressionType.Invoke:
                    {
                        var children = GetChildren(idx);
                        var arguments = new LExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children.GetSurePresentRef(i));
                        return LExpr.Invoke(ReadExpression(children.GetSurePresentRef(0)), arguments);
                    }
                case ExpressionType.Index:
                    {
                        var children = GetChildren(idx);
                        var property = (PropertyInfo)node.Obj;
                        var hasInstance = property != null || children.Count > 1;
                        var instance = hasInstance ? ReadExpression(children.GetSurePresentRef(0)) : null;
                        var arguments = new LExpr[children.Count - (hasInstance ? 1 : 0)];
                        for (var i = hasInstance ? 1 : 0; i < children.Count; ++i)
                            arguments[i - (hasInstance ? 1 : 0)] = ReadExpression(children.GetSurePresentRef(i));
                        return property != null
                            ? LExpr.Property(instance, property, arguments)
                            : LExpr.ArrayAccess(instance, arguments);
                    }
                case ExpressionType.Conditional:
                    {
                        var children = GetChildren(idx);
                        return LExpr.Condition(ReadExpression(children.GetSurePresentRef(0)), ReadExpression(children.GetSurePresentRef(1)), ReadExpression(children.GetSurePresentRef(2)), node.Type);
                    }
                case ExpressionType.Loop:
                    {
                        var children = GetChildren(idx);
                        var childIdx = 1;
                        var breakLabel = node.HasFlag(LoopHasBreakFlag) ? ReadLabelTarget(children.GetSurePresentRef(childIdx++)) : null;
                        var continueLabel = node.HasFlag(LoopHasContinueFlag) ? ReadLabelTarget(children.GetSurePresentRef(childIdx)) : null;
                        return LExpr.Loop(ReadExpression(children.GetSurePresentRef(0)), breakLabel, continueLabel);
                    }
                case ExpressionType.Goto:
                    {
                        var children = GetChildren(idx);
                        var value = children.Count > 1 ? ReadExpression(children.GetSurePresentRef(1)) : null;
                        return LExpr.MakeGoto((GotoExpressionKind)node.Flags, ReadLabelTarget(children.GetSurePresentRef(0)), value, node.Type);
                    }
                case ExpressionType.Label:
                    {
                        var children = GetChildren(idx);
                        var defaultValue = children.Count > 1 ? ReadExpression(children.GetSurePresentRef(1)) : null;
                        return LExpr.Label(ReadLabelTarget(children.GetSurePresentRef(0)), defaultValue);
                    }
                case ExpressionType.Switch:
                    {
                        // Layout: switchValue, optional defaultBody, optional SwitchCases group.
                        var children = GetChildren(idx);
                        var hasDefault = node.HasFlag(SwitchHasDefaultFlag);
                        var defaultBody = hasDefault ? ReadExpression(children.GetSurePresentRef(1)) : null;
                        var casesIdx = hasDefault ? 2 : 1;
                        var caseIdxs = children.Count > casesIdx ? GetChildren(children.GetSurePresentRef(casesIdx)) : default;
                        var cases = new LSwitchCase[caseIdxs.Count];
                        for (var i = 0; i < cases.Length; ++i)
                        {
                            Debug.Assert(_tree.Nodes.GetSurePresentRef(caseIdxs.GetSurePresentRef(i)).Is(ExprNodeKind.SwitchCase));
                            var caseChildren = GetChildren(caseIdxs.GetSurePresentRef(i));
                            var testValues = new LExpr[caseChildren.Count - 1];
                            for (var t = 0; t < testValues.Length; ++t)
                                testValues[t] = ReadExpression(caseChildren.GetSurePresentRef(t));
                            cases[i] = LExpr.SwitchCase(ReadExpression(caseChildren.GetSurePresentRef(caseChildren.Count - 1)), testValues);
                        }
                        return LExpr.Switch(node.Type, ReadExpression(children.GetSurePresentRef(0)), defaultBody, (MethodInfo)node.Obj, cases);
                    }
                case ExpressionType.Try:
                    {
                        // Layout: body, optional finally/fault, then CatchBlock handlers as direct children.
                        var children = GetChildren(idx);
                        if (node.HasFlag(TryFaultFlag))
                            return LExpr.TryFault(ReadExpression(children.GetSurePresentRef(0)), ReadExpression(children.GetSurePresentRef(1)));

                        var hasFinally = node.HasFlag(TryHasFinallyFlag);
                        var handlerStart = hasFinally ? 2 : 1;
                        var handlers = new LCatchBlock[children.Count - handlerStart];
                        for (var i = 0; i < handlers.Length; ++i)
                        {
                            ref var handler = ref _tree.Nodes.GetSurePresentRef(children.GetSurePresentRef(handlerStart + i));
                            Debug.Assert(handler.Is(ExprNodeKind.CatchBlock));
                            var handlerChildren = GetChildren(children.GetSurePresentRef(handlerStart + i));
                            var handlerChild = 0;
                            var variable = handler.HasFlag(CatchHasVariableFlag) ? (LParameter)ReadExpression(handlerChildren.GetSurePresentRef(handlerChild++)) : null;
                            var handlerBody = ReadExpression(handlerChildren.GetSurePresentRef(handlerChild++));
                            var filter = handler.HasFlag(CatchHasFilterFlag) ? ReadExpression(handlerChildren.GetSurePresentRef(handlerChild)) : null;
                            handlers[i] = LExpr.MakeCatchBlock(handler.Type, variable, handlerBody, filter);
                        }
                        var body = ReadExpression(children.GetSurePresentRef(0));
                        return hasFinally
                            ? LExpr.TryCatchFinally(body, ReadExpression(children.GetSurePresentRef(1)), handlers)
                            : LExpr.TryCatch(body, handlers);
                    }
                case ExpressionType.MemberInit:
                    {
                        var children = GetChildren(idx);
                        var bindings = new LMemberBinding[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            bindings[i - 1] = ReadMemberBinding(children.GetSurePresentRef(i));
                        return LExpr.MemberInit((LightExpression.NewExpression)ReadExpression(children.GetSurePresentRef(0)), bindings);
                    }
                case ExpressionType.ListInit:
                    {
                        var children = GetChildren(idx);
                        var initializers = new LElementInit[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            initializers[i - 1] = ReadElementInit(children.GetSurePresentRef(i));
                        return LExpr.ListInit((LightExpression.NewExpression)ReadExpression(children.GetSurePresentRef(0)), initializers);
                    }
                case ExpressionType.TypeIs:
                    return LExpr.TypeIs(ReadExpression(GetChildren(idx).GetSurePresentRef(0)), (Type)node.Obj);
                case ExpressionType.TypeEqual:
                    return LExpr.TypeEqual(ReadExpression(GetChildren(idx).GetSurePresentRef(0)), (Type)node.Obj);
                case ExpressionType.Dynamic:
                    {
                        var children = GetChildren(idx);
                        ref var delegateNode = ref _tree.Nodes.GetSurePresentRef(children.GetSurePresentRef(0));
                        Debug.Assert(delegateNode.Is(ExprNodeKind.ObjectReference));
                        var delegateType = (Type)delegateNode.Obj;
                        var arguments = new LExpr[children.Count - 1];
                        for (var i = 1; i < children.Count; ++i)
                            arguments[i - 1] = ReadExpression(children.GetSurePresentRef(i));
                        return new LightExpression.DynamicExpression(delegateType, (System.Runtime.CompilerServices.CallSiteBinder)node.Obj, arguments);
                    }
                case ExpressionType.RuntimeVariables:
                    {
                        var children = GetChildren(idx);
                        var variables = new LParameter[children.Count];
                        for (var i = 0; i < children.Count; ++i)
                            variables[i] = (LParameter)ReadExpression(children.GetSurePresentRef(i));
                        return new LightExpression.RuntimeVariablesExpression(variables);
                    }
                case ExpressionType.DebugInfo:
                    {
                        var children = GetChildren(idx);
                        ReadUInt16Pair(children.GetSurePresentRef(0), out var startLine, out var startColumn);
                        ReadUInt16Pair(children.GetSurePresentRef(1), out var endLine, out var endColumn);
                        return LExpr.DebugInfo(LExpr.SymbolDocument((string)node.Obj), startLine, startColumn, endLine, endColumn);
                    }
                default:
                    if (node.ChildCount == 1)
                        return LExpr.MakeUnary(node.NodeType, ReadExpression(GetChildren(idx).GetSurePresentRef(0)), node.Type, node.Obj as MethodInfo);

                    if (node.ChildCount >= 2)
                    {
                        var children = GetChildren(idx);
                        var conversion = children.Count > 2 ? (LightExpression.LambdaExpression)ReadExpression(children.GetSurePresentRef(2)) : null;
                        return LExpr.MakeBinary(node.NodeType, ReadExpression(children.GetSurePresentRef(0)), ReadExpression(children.GetSurePresentRef(1)),
                            node.HasFlag(BinaryLiftedToNullFlag), (MethodInfo)node.Obj, conversion);
                    }

                    throw new NotSupportedException($"Reconstruction of `ExpressionType.{node.NodeType}` is not supported yet.");
            }
        }

        private LLabelTarget ReadLabelTarget(int idx)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            Debug.Assert(node.Is(ExprNodeKind.LabelTarget));
            ref var label = ref _labelsById.Map.AddOrGetValueRef(node.ChildIdx, out var found);
            return found ? label : label = LExpr.Label(node.Type, (string)node.Obj);
        }

        private void ReadUInt16Pair(int idx, out int first, out int second)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            Debug.Assert(node.Is(ExprNodeKind.UInt16Pair));
            first = node.ChildIdx;
            second = node.ChildCount;
        }

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        private LMemberBinding ReadMemberBinding(int idx)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            var member = (MemberInfo)node.Obj;
            switch (node.Kind)
            {
                case ExprNodeKind.MemberAssignment:
                    return LExpr.Bind(member, ReadExpression(GetChildren(idx).GetSurePresentRef(0)));
                case ExprNodeKind.MemberMemberBinding:
                    {
                        var childIdxs = GetChildren(idx);
                        var bindings = new LMemberBinding[childIdxs.Count];
                        for (var i = 0; i < childIdxs.Count; ++i)
                            bindings[i] = ReadMemberBinding(childIdxs.GetSurePresentRef(i));
                        return LExpr.MemberBind(member, bindings);
                    }
                case ExprNodeKind.MemberListBinding:
                    {
                        var childIdxs = GetChildren(idx);
                        var initializers = new LElementInit[childIdxs.Count];
                        for (var i = 0; i < childIdxs.Count; ++i)
                            initializers[i] = ReadElementInit(childIdxs.GetSurePresentRef(i));
                        return LExpr.ListBind(member, initializers);
                    }
                default:
                    throw new InvalidOperationException($"Node at idx {idx} is not a member binding node.");
            }
        }

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        private LElementInit ReadElementInit(int idx)
        {
            ref var node = ref _tree.Nodes.GetSurePresentRef(idx);
            Debug.Assert(node.Is(ExprNodeKind.ElementInit));
            return LExpr.ElementInit((MethodInfo)node.Obj, ReadExpressions(GetChildren(idx)));
        }

        private ChildIdxs GetChildren(int idx) => ExprTree.GetChildren(_tree, idx);

        [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
        private LExpr[] ReadExpressions(in ChildIdxs childIdxs)
        {
            var expressions = new LExpr[childIdxs.Count];
            for (var i = 0; i < expressions.Length; ++i)
                expressions[i] = ReadExpression(childIdxs.GetSurePresentRef(i));
            return expressions;
        }
    }
}
