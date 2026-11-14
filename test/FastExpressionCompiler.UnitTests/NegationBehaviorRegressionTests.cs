using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NegationBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Short_checked_negation_matches_runtime(t);
        Nullable_short_checked_negation_matches_runtime(t);
    }

    public void Short_checked_negation_matches_runtime(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(short));
        var expression = SysExpr.Lambda<Func<short, short>>(SysExpr.NegateChecked(parameter), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new short[] { 0, 1, -1, short.MaxValue }) t.AreEqual((short)-value, fast(value));
        t.Throws<OverflowException>(() => fast(short.MinValue));
    }

    public void Nullable_short_checked_negation_matches_runtime(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(short?));
        var expression = SysExpr.Lambda<Func<short?, short?>>(SysExpr.NegateChecked(parameter), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new short?[] { null, 0, 1, -1, short.MaxValue }) t.AreEqual((short?)-value, fast(value));
        t.Throws<OverflowException>(() => fast(short.MinValue));
    }

}
