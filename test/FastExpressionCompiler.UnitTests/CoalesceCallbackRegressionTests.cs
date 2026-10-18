using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CoalesceCallbackRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_conversion_runs_only_for_nonnull_left(t);
        Nullable_conversion_accepts_underlying_or_nullable_parameter(t);
        Conversion_can_capture_outer_parameters(t);
        Ignored_result_preserves_conversion_effects(t);
    }

    public void Reference_conversion_runs_only_for_nonnull_left(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(string));
        var argument = SysExpr.Parameter(typeof(string));
        var conversion = SysExpr.Lambda<Func<string, int>>(SysExpr.Property(argument, "Length"), argument);
        var expression = SysExpr.Lambda<Func<string, int>>(SysExpr.Coalesce(left, SysExpr.Constant(42), conversion), left);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(3, fast("abc"));
        t.AreEqual(42, fast(null));
    }

    public void Nullable_conversion_accepts_underlying_or_nullable_parameter(TestContext t)
    {
        foreach (var nullable in new[] { false, true })
        {
            var left = SysExpr.Parameter(typeof(int?));
            var argument = SysExpr.Parameter(nullable ? typeof(int?) : typeof(int));
            var value = nullable ? SysExpr.Property(argument, "Value") : (System.Linq.Expressions.Expression)argument;
            var conversion = SysExpr.Lambda(SysExpr.Multiply(value, SysExpr.Constant(2)), argument);
            var expression = SysExpr.Lambda<Func<int?, int>>(SysExpr.Coalesce(left, SysExpr.Constant(23), conversion), left);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(10, fast(5));
            t.AreEqual(23, fast(null));
        }
    }

    public void Conversion_can_capture_outer_parameters(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?));
        var extra = SysExpr.Parameter(typeof(int));
        var argument = SysExpr.Parameter(typeof(int));
        var conversion = SysExpr.Lambda<Func<int, int>>(SysExpr.Add(argument, extra), argument);
        var expression = SysExpr.Lambda<Func<int?, int, int>>(SysExpr.Coalesce(left, extra, conversion), left, extra);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(12, fast(5, 7));
        t.AreEqual(7, fast(null, 7));
        t.AreEqual(15, fast(5, 10));
    }

    public void Ignored_result_preserves_conversion_effects(TestContext t)
    {
        var leftCalls = 0;
        var conversionCalls = 0;
        var fallbackCalls = 0;
        Func<int?, int?> left = x => { ++leftCalls; return x; };
        Func<int, int> convert = x => { ++conversionCalls; return x * 2; };
        Func<int> fallback = () => { ++fallbackCalls; return 23; };
        var input = SysExpr.Parameter(typeof(int?));
        var argument = SysExpr.Parameter(typeof(int));
        var conversion = SysExpr.Lambda<Func<int, int>>(SysExpr.Invoke(SysExpr.Constant(convert), argument), argument);
        var body = SysExpr.Coalesce(SysExpr.Invoke(SysExpr.Constant(left), input), SysExpr.Invoke(SysExpr.Constant(fallback)), conversion);
        var expression = SysExpr.Lambda<Action<int?>>(SysExpr.Block(body, SysExpr.Empty()), input);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        fast(5);
        fast(null);
        t.AreEqual(2, leftCalls);
        t.AreEqual(1, conversionCalls);
        t.AreEqual(1, fallbackCalls);
    }
}
