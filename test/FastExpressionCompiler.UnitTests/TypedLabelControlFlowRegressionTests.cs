using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct TypedLabelControlFlowRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Jumps_continue_after_typed_labels(t);
        Jumps_preserve_surrounding_operand_stack(t);
        Inlined_jumps_continue_at_the_actual_target(t);
        Backward_typed_jumps_use_an_existing_value_label(t);
    }

    public void Jumps_continue_after_typed_labels(TestContext t)
    {
        foreach (var kind in new[] { GotoExpressionKind.Return, GotoExpressionKind.Goto, GotoExpressionKind.Break })
        {
            var label = SysExpr.Label(typeof(int));
            var body = SysExpr.Block(SysExpr.MakeGoto(kind, label, SysExpr.Constant(1), typeof(void)),
                SysExpr.Label(label, SysExpr.Constant(2)), SysExpr.Constant(3));
            var fast = SysExpr.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(3, fast());
        }
    }

    public void Jumps_preserve_surrounding_operand_stack(TestContext t)
    {
        foreach (var kind in new[] { GotoExpressionKind.Return, GotoExpressionKind.Goto, GotoExpressionKind.Break })
        {
            var label = SysExpr.Label(typeof(int));
            var block = SysExpr.Block(SysExpr.MakeGoto(kind, label, SysExpr.Constant(7), typeof(void)),
                SysExpr.Throw(SysExpr.New(typeof(Exception))), SysExpr.Label(label, SysExpr.Constant(2)));
            var expression = SysExpr.Lambda<Func<int>>(SysExpr.Add(SysExpr.Constant(10), block));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(17, fast());
        }
    }

    public void Backward_typed_jumps_use_an_existing_value_label(TestContext t)
    {
        foreach (var kind in new[] { GotoExpressionKind.Return, GotoExpressionKind.Goto, GotoExpressionKind.Break })
        {
            var count = SysExpr.Variable(typeof(int));
            var label = SysExpr.Label(typeof(int));
            var body = SysExpr.Block(new[] { count }, SysExpr.Label(label, SysExpr.Constant(0)),
                SysExpr.PostIncrementAssign(count), SysExpr.IfThen(SysExpr.LessThan(count, SysExpr.Constant(3)),
                    SysExpr.MakeGoto(kind, label, count, typeof(void))), count);
            var fast = SysExpr.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(3, fast());
        }
    }

    public void Inlined_jumps_continue_at_the_actual_target(TestContext t)
    {
        var label = SysExpr.Label(typeof(int));
        var body = SysExpr.Block(SysExpr.Return(label, SysExpr.Constant(1)),
            SysExpr.Label(label, SysExpr.Constant(2)), SysExpr.Constant(3));
        var inner = SysExpr.Lambda<Func<int>>(body);
        var fast = SysExpr.Lambda<Func<int>>(SysExpr.Add(SysExpr.Constant(10), SysExpr.Invoke(inner)))
            .FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(13, fast());
    }
}
