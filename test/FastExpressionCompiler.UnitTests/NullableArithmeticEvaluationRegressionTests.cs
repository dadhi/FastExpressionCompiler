using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NullableArithmeticEvaluationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {

        Null_ref_assignment_evaluates_right_before_writeback(t);

    }

    public void Null_ref_assignment_evaluates_right_before_writeback(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?).MakeByRefType());
        var right = SysExpr.Parameter(typeof(Func<int?>));
        var rhs = SysExpr.TryCatch(SysExpr.Invoke(right), SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(int?))));
        var expression = SysExpr.Lambda<CompoundAssignmentExceptionRegressionTests.RefNullableInt>(SysExpr.AddAssign(left, rhs), left, right);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        int? value = null;
        var calls = 0;
        t.AreEqual((int?)null, fast(ref value, () => { ++calls; value = 100; return 3; }));
        t.AreEqual(1, calls);
        t.AreEqual((int?)null, value);
    }

}
