using System;
using S = System.Linq.Expressions.Expression;
using E = System.Linq.Expressions;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct LexicalJumpScopeRegressionTests : ITestX
{
    public void Run(TestRun t)
    {

        Switch_cases_share_direct_label_scope(t);
        Reused_invocations_have_independent_scope_roots(t);

    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining };
    private static void Reject(TestContext t, E.Expression<Func<int>> source)
    {
        t.Throws<InvalidOperationException>(() => source.Compile());
        foreach (var flags in Flags)
            t.Throws<InvalidOperationException>(() => source.FromSysExpression().CompileFast(true, flags));
    }
    private static void Verify(TestContext t, E.Expression<Func<int>> source, int expected)
    {
        t.AreEqual(expected, source.Compile()());
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
        }
    }

    public sealed class Holder
    {
        public int Value;
        public Holder(int value) { Value = value; }
    }

    public void Switch_cases_share_direct_label_scope(TestContext t)
    {
        var target = S.Label(typeof(int), "case");
        var input = S.Parameter(typeof(int));
        var body = S.Switch(typeof(int), input, S.Constant(0), null,
            S.SwitchCase(S.Block(S.Goto(target, S.Constant(29)), S.Constant(1)), S.Constant(0)),
            S.SwitchCase(S.Block(S.Label(target, S.Constant(2))), S.Constant(1)));
        Verify(t, S.Lambda<Func<int>>(S.Invoke(S.Lambda<Func<int, int>>(body, input), S.Constant(0))), 29);
    }
    public void Reused_invocations_have_independent_scope_roots(TestContext t)
    {
        var target = S.Label(typeof(int), "reused");
        var invocation = S.Invoke(S.Lambda<Func<int>>(S.Block(S.Goto(target, S.Constant(7)), S.Label(target, S.Constant(0)))));
        Verify(t, S.Lambda<Func<int>>(S.Add(invocation, invocation)), 14);
    }

}
