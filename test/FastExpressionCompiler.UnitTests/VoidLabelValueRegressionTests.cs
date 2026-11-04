using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct VoidLabelValueRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Void_targets_evaluate_and_discard_values(t);
        Void_returns_run_finally_blocks(t);
        Inlined_void_returns_skip_following_effects(t);
        Void_label_returns_continue_at_the_target(t);
    }

    public void Void_targets_evaluate_and_discard_values(TestContext t)
    {
        Check(t, new Func<int>(() => { ++Calls; return 42; }));
        Check(t, new Func<string>(() => { ++Calls; return "value"; }));
        Check(t, new Func<DateTime>(() => { ++Calls; return DateTime.MinValue; }));
    }

    private static void Check<T>(TestContext t, Func<T> getValue)
    {
        foreach (var kind in new[] { GotoExpressionKind.Return, GotoExpressionKind.Goto, GotoExpressionKind.Break })
        {
            var getter = SysExpr.Parameter(typeof(Func<T>));
            var target = SysExpr.Label();
            var expression = SysExpr.Lambda<Action<Func<T>>>(SysExpr.Block(
                SysExpr.MakeGoto(kind, target, SysExpr.Invoke(getter), typeof(void)), SysExpr.Label(target)), getter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            fast(getValue);
            t.AreEqual(1, Calls);
        }
    }

    public void Void_returns_run_finally_blocks(TestContext t)
    {
        var getter = SysExpr.Parameter(typeof(Func<int>));
        var cleanup = SysExpr.Parameter(typeof(Action));
        var target = SysExpr.Label();
        var body = SysExpr.Block(SysExpr.TryFinally(SysExpr.Return(target, SysExpr.Invoke(getter)),
            SysExpr.Invoke(cleanup)), SysExpr.Label(target));
        var fast = SysExpr.Lambda<Action<Func<int>, Action>>(body, getter, cleanup).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var values = 0;
        var cleanups = 0;
        fast(() => { ++values; return 42; }, () => ++cleanups);
        t.AreEqual(1, values);
        t.AreEqual(1, cleanups);
    }

    public void Inlined_void_returns_skip_following_effects(TestContext t)
    {
        var getter = SysExpr.Parameter(typeof(Func<int>));
        var skipped = SysExpr.Parameter(typeof(Action));
        var target = SysExpr.Label();
        var inner = SysExpr.Lambda<Action>(SysExpr.Block(SysExpr.Return(target, SysExpr.Invoke(getter)),
            SysExpr.Invoke(skipped), SysExpr.Label(target)));
        var fast = SysExpr.Lambda<Action<Func<int>, Action>>(SysExpr.Invoke(inner), getter, skipped)
            .FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var values = 0;
        var skippedCalls = 0;
        fast(() => { ++values; return 42; }, () => ++skippedCalls);
        t.AreEqual(1, values);
        t.AreEqual(0, skippedCalls);
    }

    public void Void_label_returns_continue_at_the_target(TestContext t)
    {
        var target = SysExpr.Label();
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.Block(SysExpr.Return(target, SysExpr.Constant(42)),
            SysExpr.Label(target), SysExpr.Constant(7)));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(expression.Compile()(), fast());
    }

    private static int Calls;
}
