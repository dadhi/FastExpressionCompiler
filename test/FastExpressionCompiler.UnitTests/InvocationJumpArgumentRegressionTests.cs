using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InvocationJumpArgumentRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Argument_jump_discards_pending_receiver_and_arguments(t);
        Inline_lambda_argument_jump_stays_in_the_caller(t);
        Runtime_expression_compilation_follows_argument_jumps(t);
    }

    private static string Trace;
    public static Func<int, int, int> Receiver() { Trace += "receiver;"; return Target; }
    public static int First() { Trace += "first;"; return 2; }
    public static int Target(int x, int y) { Trace += "target;"; return x + y; }

    public static System.Linq.Expressions.Expression<Func<int, int>> InvalidRuntimeExpression()
    {
        Trace += "receiver;";
        var unbound = S.Parameter(typeof(int), "unbound");
        return S.Lambda<Func<int, int>>(unbound, S.Parameter(typeof(int)));
    }

    public void Runtime_expression_compilation_follows_argument_jumps(TestContext t)
    {
        var leave = S.Parameter(typeof(bool));
        var target = S.Label(typeof(int));
        var receiver = S.Call(typeof(InvocationJumpArgumentRegressionTests).GetMethod(nameof(InvalidRuntimeExpression)));
        var argument = S.Block(S.Call(typeof(InvocationJumpArgumentRegressionTests).GetMethod(nameof(First))),
            S.Condition(leave, S.Goto(target, S.Constant(7), typeof(int)), S.Constant(3)));
        var tree = S.Lambda<Func<bool, int>>(S.Block(S.Invoke(receiver, argument), S.Label(target, S.Constant(8))), leave);
        CheckRuntimeJump(t, tree.Compile());
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) CheckRuntimeJump(t, fast);
        }
    }

    private static void CheckRuntimeJump(TestContext t, Func<bool, int> compiled)
    {
        Trace = ""; t.AreEqual(7, compiled(true)); t.AreEqual("receiver;first;", Trace);
        Trace = ""; t.Throws<InvalidOperationException>(() => compiled(false)); t.AreEqual("receiver;first;", Trace);
    }

    public void Argument_jump_discards_pending_receiver_and_arguments(TestContext t)
    {
        var leave = S.Parameter(typeof(bool));
        var target = S.Label(typeof(int));
        var receiver = S.Call(typeof(InvocationJumpArgumentRegressionTests).GetMethod(nameof(Receiver)));
        var first = S.Call(typeof(InvocationJumpArgumentRegressionTests).GetMethod(nameof(First)));
        var argument = S.Condition(leave, S.Goto(target, S.Constant(7), typeof(int)), S.Constant(3));
        var tree = S.Lambda<Func<bool, int>>(S.Block(S.Invoke(receiver, first, argument), S.Label(target, S.Constant(8))), leave);
        Verify(t, tree, "receiver;first;", "receiver;first;target;");
    }

    public void Inline_lambda_argument_jump_stays_in_the_caller(TestContext t)
    {
        var leave = S.Parameter(typeof(bool));
        var p = S.Parameter(typeof(int));
        var target = S.Label(typeof(int));
        var argument = S.Condition(leave, S.Goto(target, S.Constant(7), typeof(int)), S.Constant(3));
        var tree = S.Lambda<Func<bool, int>>(S.Block(S.Invoke(S.Lambda<Func<int, int>>(p, p), argument), S.Label(target, S.Constant(8))), leave);
        Verify(t, tree, "", "");
    }

    private static void Verify(TestContext t, System.Linq.Expressions.Expression<Func<bool, int>> tree, string jumped, string normal)
    {
        Trace = ""; var system = tree.Compile();
        t.AreEqual(7, system(true)); t.AreEqual(jumped, Trace);
        Trace = ""; t.AreEqual(8, system(false)); t.AreEqual(normal, Trace);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Trace = ""; t.AreEqual(7, fast(true)); t.AreEqual(jumped, Trace);
            Trace = ""; t.AreEqual(8, fast(false)); t.AreEqual(normal, Trace);
        }
    }
}
