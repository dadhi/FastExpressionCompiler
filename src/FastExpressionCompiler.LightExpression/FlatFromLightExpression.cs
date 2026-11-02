#nullable disable
namespace FastExpressionCompiler.FlatExpression;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression.ImTools;
using LExpr = FastExpressionCompiler.LightExpression.Expression;
using LParameter = FastExpressionCompiler.LightExpression.ParameterExpression;
using LMemberBinding = FastExpressionCompiler.LightExpression.MemberBinding;
using LElementInit = FastExpressionCompiler.LightExpression.ElementInit;

public partial struct ExprTree
{
    // todo: @perf the ushort[] per call, use the SmallList of the child ids as the ToLightExpression does
    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    private ushort[] AddLightArguments(LightExpression.IArgumentProvider args)
    {
        var result = new ushort[args.ArgumentCount];
        for (var i = 0; i < result.Length; ++i)
            result[i] = AddLightExpression(args.GetArgument(i));
        return result;
    }

    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    private ushort[] AddLightExpressions(System.Collections.Generic.IReadOnlyList<LExpr> exprs)
    {
        var result = new ushort[exprs.Count];
        for (var i = 0; i < result.Length; ++i)
            result[i] = AddLightExpression(exprs[i]);
        return result;
    }

    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    private ushort AddLightExpression(LExpr expr)
    {
        switch (expr.NodeType)
        {
            case ExpressionType.Constant:
                return Constant(((LightExpression.ConstantExpression)expr).Value, expr.Type);

            case ExpressionType.Default:
                return Default(expr.Type);

            case ExpressionType.Parameter:
                {
                    var parameter = (LParameter)expr;
                    var id = checked((ushort)GetId(ref _parameterIds, parameter));
                    return ParameterWithId(parameter.IsByRef ? parameter.Type : expr.Type, parameter.Name, id);
                }
            case ExpressionType.Lambda:
                {
                    var lambda = (LightExpression.LambdaExpression)expr;
                    var pars = new ushort[lambda.ParameterCount];
                    for (var i = 0; i < pars.Length; ++i)
                        pars[i] = AddLightExpression(lambda.GetParameter(i));
                    return Lambda(expr.Type, AddLightExpression(lambda.Body), pars);
                }
            case ExpressionType.Block:
                {
                    var block = (LightExpression.BlockExpression)expr;
                    ushort[] vars = null;
                    if (block.Variables.Count != 0)
                    {
                        vars = new ushort[block.Variables.Count];
                        for (var i = 0; i < vars.Length; ++i)
                            vars[i] = AddLightExpression(block.Variables[i]);
                    }
                    var exprs = new ushort[block.Expressions.Count];
                    for (var i = 0; i < exprs.Length; ++i)
                        exprs[i] = AddLightExpression(block.Expressions[i]);
                    return Block(expr.Type, vars, exprs);
                }
            case ExpressionType.MemberAccess:
                {
                    var member = (LightExpression.MemberExpression)expr;
                    return member.Expression != null
                        ? MakeMemberAccess(AddLightExpression(member.Expression), member.Member)
                        : MakeMemberAccess(member.Member);
                }
            case ExpressionType.Call:
                {
                    var call = (LightExpression.MethodCallExpression)expr;
                    var args = AddLightArguments(call);
                    return call.Object != null
                        ? Call(AddLightExpression(call.Object), call.Method, args)
                        : Call(call.Method, args);
                }
            case ExpressionType.New:
                {
                    var @new = (LightExpression.NewExpression)expr;
                    if (@new.Constructor == null)
                        return New(expr.Type);
                    if (@new.ArgumentCount == 0)
                        return New(@new.Constructor);
                    return New(@new.Constructor, AddLightArguments(@new));
                }
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
                {
                    var items = AddLightArguments((LightExpression.NewArrayExpression)expr);
                    return expr.NodeType == ExpressionType.NewArrayInit
                        ? NewArrayInit(expr.Type.GetElementType(), items)
                        : NewArrayBounds(expr.Type.GetElementType(), items);
                }
            case ExpressionType.Invoke:
                {
                    var invoke = (LightExpression.InvocationExpression)expr;
                    var args = AddLightArguments(invoke);
                    return Invoke(AddLightExpression(invoke.Expression), args);
                }
            case ExpressionType.Index:
                {
                    var indexExpr = (LightExpression.IndexExpression)expr;
                    var args = AddLightArguments(indexExpr);
                    var instance = indexExpr.Object != null ? AddLightExpression(indexExpr.Object) : (ushort)0;
                    return indexExpr.Indexer != null
                        ? Property(instance, indexExpr.Indexer, args)
                        : ArrayAccess(instance, args);
                }
            case ExpressionType.Conditional:
                {
                    var conditional = (LightExpression.ConditionalExpression)expr;
                    return Condition(
                        AddLightExpression(conditional.Test),
                        AddLightExpression(conditional.IfTrue),
                        AddLightExpression(conditional.IfFalse),
                        expr.Type);
                }
            case ExpressionType.Loop:
                {
                    var loop = (LightExpression.LoopExpression)expr;
                    return Loop(
                        AddLightExpression(loop.Body),
                        loop.BreakLabel != null ? AddLightLabelTarget(loop.BreakLabel) : (ushort)0,
                        loop.ContinueLabel != null ? AddLightLabelTarget(loop.ContinueLabel) : (ushort)0);
                }
            case ExpressionType.Goto:
                {
                    var @goto = (LightExpression.GotoExpression)expr;
                    return MakeGoto(@goto.Kind, AddLightLabelTarget(@goto.Target),
                        @goto.Value != null ? AddLightExpression(@goto.Value) : (ushort)0, expr.Type);
                }
            case ExpressionType.Label:
                {
                    var label = (LightExpression.LabelExpression)expr;
                    return Label(AddLightLabelTarget(label.Target),
                        label.DefaultValue != null ? AddLightExpression(label.DefaultValue) : (ushort)0);
                }
            case ExpressionType.Switch:
                {
                    var @switch = (LightExpression.SwitchExpression)expr;
                    var cases = new ushort[@switch.Cases.Count];
                    for (var i = 0; i < cases.Length; ++i)
                    {
                        var sc = @switch.Cases[i];
                        var tests = AddLightExpressions(sc.TestValues);
                        cases[i] = SwitchCase(AddLightExpression(sc.Body), tests);
                    }
                    return Switch(expr.Type, AddLightExpression(@switch.SwitchValue),
                        @switch.DefaultBody != null ? AddLightExpression(@switch.DefaultBody) : (ushort)0,
                        @switch.Comparison, cases);
                }
            case ExpressionType.Try:
                {
                    var @try = (LightExpression.TryExpression)expr;
                    if (@try.Fault != null)
                        return TryFault(AddLightExpression(@try.Body), AddLightExpression(@try.Fault));

                    var handlerCount = @try.Handlers?.Count ?? 0;
                    var handlers = new ushort[handlerCount];
                    for (var i = 0; i < handlers.Length; ++i)
                    {
                        var h = @try.Handlers[i];
                        var variable = h.Variable != null ? AddLightExpression(h.Variable) : (ushort)0;
                        var filter = h.Filter != null ? AddLightExpression(h.Filter) : (ushort)0;
                        handlers[i] = MakeCatchBlock(h.Test, variable, AddLightExpression(h.Body), filter);
                    }

                    if (@try.Finally != null)
                        return handlers.Length != 0
                            ? TryCatchFinally(AddLightExpression(@try.Body), AddLightExpression(@try.Finally), handlers)
                            : TryFinally(AddLightExpression(@try.Body), AddLightExpression(@try.Finally));

                    return TryCatch(AddLightExpression(@try.Body), handlers);
                }
            case ExpressionType.MemberInit:
                {
                    var memberInit = (LightExpression.MemberInitExpression)expr;
                    var bindings = new ushort[memberInit.Bindings.Count];
                    for (var i = 0; i < bindings.Length; ++i)
                        bindings[i] = AddLightMemberBinding(memberInit.Bindings[i]);
                    return MemberInit(AddLightExpression(memberInit.NewExpression), bindings);
                }
            case ExpressionType.ListInit:
                {
                    var listInit = (LightExpression.ListInitExpression)expr;
                    var inits = new ushort[listInit.Initializers.Count];
                    for (var i = 0; i < inits.Length; ++i)
                        inits[i] = AddLightElementInit(listInit.Initializers[i]);
                    return ListInit(AddLightExpression(listInit.NewExpression), inits);
                }
            case ExpressionType.TypeIs:
                {
                    var typeBinary = (LightExpression.TypeBinaryExpression)expr;
                    return TypeIs(AddLightExpression(typeBinary.Expression), typeBinary.TypeOperand);
                }
            case ExpressionType.TypeEqual:
                {
                    var typeBinary = (LightExpression.TypeBinaryExpression)expr;
                    return TypeEqual(AddLightExpression(typeBinary.Expression), typeBinary.TypeOperand);
                }
            case ExpressionType.Dynamic:
                {
                    var dynamic = (LightExpression.DynamicExpression)expr;
                    return Dynamic(dynamic.DelegateType, dynamic.Binder, AddLightExpressions(dynamic.Arguments));
                }
            case ExpressionType.RuntimeVariables:
                {
                    var runtime = (LightExpression.RuntimeVariablesExpression)expr;
                    var vars = new ushort[runtime.Variables.Count];
                    for (var i = 0; i < vars.Length; ++i)
                        vars[i] = AddLightExpression(runtime.Variables[i]);
                    return RuntimeVariables(vars);
                }
            case ExpressionType.DebugInfo:
                {
                    var debug = (LightExpression.DebugInfoExpression)expr;
                    return DebugInfo(debug.Document.FileName, debug.StartLine, debug.StartColumn, debug.EndLine, debug.EndColumn);
                }
            default:
                if (expr is LightExpression.UnaryExpression unary)
                    return MakeUnary(expr.NodeType, AddLightExpression(unary.Operand), expr.Type, unary.Method);

                if (expr is LightExpression.BinaryExpression binary)
                    return MakeBinary(expr.NodeType,
                        AddLightExpression(binary.Left),
                        AddLightExpression(binary.Right),
                        binary.IsLiftedToNull,
                        binary.Method,
                        binary.Conversion != null ? AddLightExpression(binary.Conversion) : (ushort)0,
                        expr.Type);

                throw new NotSupportedException($"Flattening of `ExpressionType.{expr.NodeType}` is not supported yet.");
        }
    }

    private ushort AddLightLabelTarget(LightExpression.LabelTarget target) => AddLabelTarget(target, target.Type, target.Name);

    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    private ushort AddLightMemberBinding(LMemberBinding binding)
    {
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                return Bind(binding.Member, AddLightExpression(((LightExpression.MemberAssignment)binding).Expression));
            case MemberBindingType.MemberBinding:
                {
                    var memberBinding = (LightExpression.MemberMemberBinding)binding;
                    var bindings = new ushort[memberBinding.Bindings.Count];
                    for (var i = 0; i < bindings.Length; ++i)
                        bindings[i] = AddLightMemberBinding(memberBinding.Bindings[i]);
                    return MemberBind(binding.Member, bindings);
                }
            case MemberBindingType.ListBinding:
                {
                    var listBinding = (LightExpression.MemberListBinding)binding;
                    var inits = new ushort[listBinding.Initializers.Count];
                    for (var i = 0; i < inits.Length; ++i)
                        inits[i] = AddLightElementInit(listBinding.Initializers[i]);
                    return ListBind(binding.Member, inits);
                }
            default:
                throw new NotSupportedException($"Flattening of member binding `{binding.BindingType}` is not supported yet.");
        }
    }

    [RequiresUnreferencedCode(LightExpression.Trimming.Message)]
    private ushort AddLightElementInit(LElementInit init) => ElementInit(init.AddMethod, AddLightArguments(init));
}
