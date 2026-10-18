using System;
using S = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct LabelDefinitionValidationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Duplicate_labels_report_the_target(t);
        Duplicate_loop_labels_report_the_target(t);
        Undefined_labels_report_the_target(t);
        Valid_forward_backward_and_loop_labels_are_preserved(t);
    }

    public void Duplicate_labels_report_the_target(TestContext t)
    {
        const string name = "duplicate-label-contract";
        var label = S.Label(name);
        var expression = S.Lambda<Action>(S.Block(S.Label(label), S.Label(label))).FromSysExpression();
        var error = t.Throws<InvalidOperationException>(() => expression.CompileFast(true));
        if (t.IsNotNull(error)) t.IsTrue(error.Message.Contains(name));
    }

    public void Duplicate_loop_labels_report_the_target(TestContext t)
    {
        foreach (var kind in new[] { 0, 1, 2 })
        {
            var name = "duplicate-loop-contract-" + kind;
            var label = S.Label(name);
            var body = kind == 0 ? S.Loop(S.Empty(), label, label)
                : kind == 1 ? S.Loop(S.Label(label), label)
                : S.Loop(S.Label(label), null, label);
            var expression = S.Lambda<Action>(body).FromSysExpression();
            var error = t.Throws<InvalidOperationException>(() => expression.CompileFast(true));
            if (t.IsNotNull(error)) t.IsTrue(error.Message.Contains(name));
        }
    }

    public void Undefined_labels_report_the_target(TestContext t)
    {
        const string name = "undefined-label-contract";
        var expression = S.Lambda<Action>(S.Goto(S.Label(name))).FromSysExpression();
        var error = t.Throws<InvalidOperationException>(() => expression.CompileFast(true));
        if (t.IsNotNull(error)) t.IsTrue(error.Message.Contains(name));
    }

    public void Valid_forward_backward_and_loop_labels_are_preserved(TestContext t)
    {
        var value = S.Variable(typeof(int));
        var back = S.Label("back");
        var finish = S.Label(typeof(int), "finish");
        var body = S.Block(new[] { value }, S.Assign(value, S.Constant(0)), S.Label(back),
            S.PreIncrementAssign(value), S.IfThen(S.LessThan(value, S.Constant(3)), S.Goto(back)),
            S.Goto(finish, value), S.Label(finish, S.Constant(-1)));
        var expression = S.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(expression)) t.AreEqual(3, expression());
        var stop = S.Label(typeof(int), "stop");
        var loop = S.Lambda<Func<int>>(S.Loop(S.Break(stop, S.Constant(7)), stop)).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(loop)) t.AreEqual(7, loop());
    }
}
