using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct FrameworkFilterRejectionTests : ITestX
{
    public void Run(TestRun t)
    {
#if NETFRAMEWORK
        Framework_dynamic_method_filters_are_rejected_cleanly(t);
#endif
    }

#if NETFRAMEWORK
    public void Framework_dynamic_method_filters_are_rejected_cleanly(TestContext t)
    {
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.TryCatch(
            SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int)),
            SysExpr.Catch(typeof(Exception), SysExpr.Constant(7), SysExpr.Constant(true))));
        t.AreEqual(null, expression.FromSysExpression().CompileFast(true));
        var error = t.Throws<NotSupportedExpressionException>(() =>
            expression.FromSysExpression().CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        if (error != null)
            t.AreEqual(ExpressionCompiler.Result.NotSupported_ExceptionCatchFilter, error.Reason);
    }
#endif

}
