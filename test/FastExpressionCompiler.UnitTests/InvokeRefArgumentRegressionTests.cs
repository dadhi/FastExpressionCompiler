using System;
using System.Reflection;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct InvokeRefArgumentRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Delegate_invoke_value_can_be_ref_call_argument(t);
        Inlined_lambda_invoke_value_can_be_ref_call_argument(t);
        Ref_return_delegate_invoke_preserves_reference(t);
        Ignored_invocation_results_keep_effects(t);
#if !LIGHT_EXPRESSION
        Delegate_invoke_value_can_be_ref_conversion_operand(t);
#endif
    }
    public void Delegate_invoke_value_can_be_ref_call_argument(TestContext t)
    {
        var get = Expr.Parameter(typeof(Func<int>));
        var expression = Expr.Lambda<Func<Func<int>, int>>(Expr.Call(typeof(InvokeRefArgumentRegressionTests).GetMethod(nameof(Mutate)), Expr.Invoke(get)), get);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        t.AreEqual(5, fast(() => { ++calls; return 5; }));
        t.AreEqual(1, calls);
    }
    public void Inlined_lambda_invoke_value_can_be_ref_call_argument(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining })
        foreach (var labeledReturn in new[] { false, true })
        {
            var get = Expr.Parameter(typeof(Func<int>));
            var label = Expr.Label(typeof(int));
            var body = labeledReturn ? Expr.Block(Expr.Return(label, Expr.Invoke(get)), Expr.Label(label, Expr.Constant(0))) : (Expr)Expr.Invoke(get);
            var lambda = Expr.Lambda<Func<int>>(body);
            var expression = Expr.Lambda<Func<Func<int>, int>>(Expr.Call(typeof(InvokeRefArgumentRegressionTests).GetMethod(nameof(Mutate)), Expr.Invoke(lambda)), get);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            var calls = 0;
            t.AreEqual(5, fast(() => { ++calls; return 5; }));
            t.AreEqual(1, calls);
        }
    }
    public void Ref_return_delegate_invoke_preserves_reference(TestContext t)
    {
        var get = Expr.Parameter(typeof(RefReturn));
        var expression = Expr.Lambda<ForwardRef>(Expr.Invoke(get), get);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Value = 5;
        ref var value = ref fast(ReturnReference);
        t.AreEqual(5, value);
        value = 42;
        t.AreEqual(42, Value);
    }
    public void Ignored_invocation_results_keep_effects(TestContext t)
    {
        foreach (var inline in new[] { false, true })
        {
            var calls = 0;
            Expr target = Expr.Constant((Func<int>)(() => { ++calls; return 5; }));
            if (inline) target = Expr.Lambda<Func<int>>(Expr.Invoke(target));
            var fast = Expr.Lambda<Action>(Expr.Block(Expr.Invoke(target), Expr.Empty())).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            fast();
            t.AreEqual(1, calls);
        }
        var ignoredRef = Expr.Lambda<Action>(Expr.Block(Expr.Invoke(Expr.Constant((RefReturn)ReturnReference)), Expr.Empty())).CompileFast(true);
        if (!t.IsNotNull(ignoredRef)) return;
        RefCalls = 0;
        ignoredRef();
        t.AreEqual(1, RefCalls);
    }
#if !LIGHT_EXPRESSION
    public void Delegate_invoke_value_can_be_ref_conversion_operand(TestContext t)
    {
        foreach (var inline in new[] { false, true })
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining })
        {
            var get = Expr.Parameter(typeof(Func<int>));
            Expr invocation = Expr.Invoke(get);
            if (inline) invocation = Expr.Invoke(Expr.Lambda<Func<int>>(invocation));
            var body = Expr.Convert(invocation, typeof(Converted), typeof(InvokeRefArgumentRegressionTests).GetMethod(nameof(ConvertRef)));
            var expression = Expr.Lambda<Func<Func<int>, Converted>>(body, get);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            var calls = 0;
            t.AreEqual(5, fast(() => { ++calls; return 5; }).Value);
            t.AreEqual(1, calls);
        }
    }
#endif
    public static int Mutate(ref int value) => value++;
    public static Converted ConvertRef(ref int value) => new Converted { Value = value++ };
    public class Converted { public int Value; }
    public static int Value;
    public delegate ref int RefReturn();
    public delegate ref int ForwardRef(RefReturn getter);
    public static int RefCalls;
    public static ref int ReturnReference() { ++RefCalls; return ref Value; }
}
