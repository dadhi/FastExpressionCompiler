using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InvalidFilterControlFlowRegressionTests : ITestX
{
    public void Run(TestRun t) => Unsupported_filter_control_flow_is_rejected(t);

    public void Unsupported_filter_control_flow_is_rejected(TestContext t)
    {
        var nested = SysExpr.TryCatch(SysExpr.Constant(true), SysExpr.Catch(typeof(Exception), SysExpr.Constant(false)));
        var target = SysExpr.Label(typeof(bool));
        var jump = SysExpr.Block(SysExpr.Goto(target, SysExpr.Constant(true)), SysExpr.Constant(false));
        foreach (var filter in new System.Linq.Expressions.Expression[] { nested, jump, SysExpr.Rethrow(typeof(bool)) })
        {
            var guarded = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(Exception)), typeof(bool)),
                SysExpr.Catch(typeof(Exception), SysExpr.Constant(true), filter));
            var expression = SysExpr.Lambda<Func<bool>>(SysExpr.Block(guarded, SysExpr.Label(target, SysExpr.Constant(false))));
            if (ReferenceEquals(filter, nested) || ReferenceEquals(filter, jump))
                t.Throws<InvalidOperationException>(() => expression.FromSysExpression().CompileFast(true));
            else
                t.AreEqual(null, expression.FromSysExpression().CompileFast(true));
        }
    }
}
