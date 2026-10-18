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


        Inlined_jumps_continue_at_the_actual_target(t);

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
