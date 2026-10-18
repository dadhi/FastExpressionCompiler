using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ComputedInvocationLocalReturnRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Argument_local_return_produces_the_computed_invocation_argument(t);
        Argument_local_return_preserves_the_enclosing_inlined_caller(t);
    }

    private static int Calls;
    public static int Identity(int value) { ++Calls; return value; }

    private static System.Linq.Expressions.Expression Receiver(bool runtimeExpression)
    {
        if (!runtimeExpression) return S.Constant((Func<int, int>)Identity);
        var parameter = S.Parameter(typeof(int));
        var expression = S.Lambda<Func<int, int>>(S.Call(
            typeof(ComputedInvocationLocalReturnRegressionTests).GetMethod(nameof(Identity)), parameter), parameter);
        return S.Constant(expression, typeof(System.Linq.Expressions.Expression<Func<int, int>>));
    }

    private static System.Linq.Expressions.Expression Argument()
    {
        var target = S.Label(typeof(int));
        return S.Block(S.Return(target, S.Constant(5)), S.Label(target, S.Constant(0)));
    }

    public void Argument_local_return_produces_the_computed_invocation_argument(TestContext t)
    {
        foreach (var runtimeExpression in new[] { false, true })
            Verify(t, S.Lambda<Func<int>>(S.Invoke(Receiver(runtimeExpression), Argument())), 5);
    }

    public void Argument_local_return_preserves_the_enclosing_inlined_caller(TestContext t)
    {
        foreach (var runtimeExpression in new[] { false, true })
        {
            var inner = S.Lambda<Func<int>>(S.Add(
                S.Invoke(Receiver(runtimeExpression), Argument()), S.Constant(10)));
            Verify(t, S.Lambda<Func<int>>(S.Invoke(inner)), 15);
        }
    }

    private static void Verify(TestContext t, System.Linq.Expressions.Expression<Func<int>> source, int expected)
    {
        Calls = 0; t.AreEqual(expected, source.Compile()()); t.AreEqual(1, Calls);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Calls = 0; t.AreEqual(expected, fast()); t.AreEqual(1, Calls);
        }
    }
}
