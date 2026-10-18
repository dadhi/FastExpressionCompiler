using System;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct DecimalConstantBitsRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Decimal_constant_retains_sign_and_scale(t);
        Nullable_decimal_constant_retains_sign_and_scale(t);
        Reference_typed_decimal_constant_retains_sign_and_scale(t);
    }
    public void Decimal_constant_retains_sign_and_scale(TestContext t) => Check(t, typeof(decimal));
    public void Nullable_decimal_constant_retains_sign_and_scale(TestContext t) => Check(t, typeof(decimal?));
    public void Reference_typed_decimal_constant_retains_sign_and_scale(TestContext t) => Check(t, typeof(object));
    private static void Check(TestContext t, Type type)
    {
        foreach (var negative in new[] { false, true })
        foreach (var scale in new byte[] { 0, 1, 3, 28 })
        foreach (var low in new[] { 0, 1, 1000, int.MaxValue })
        {
            var value = new decimal(low, 0, 0, negative, scale);
            var body = Expr.Convert(Expr.Constant(value, type), typeof(decimal));
            var fast = Expr.Lambda<Func<decimal>>(body).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = decimal.GetBits(value);
            var actual = decimal.GetBits(fast());
            for (var i = 0; i != 4; ++i) t.AreEqual(expected[i], actual[i]);
        }
        foreach (var value in new[] { decimal.MinValue, decimal.MaxValue, (decimal)long.MinValue, (decimal)long.MaxValue })
        {
            var fast = Expr.Lambda<Func<decimal>>(Expr.Convert(Expr.Constant(value, type), typeof(decimal))).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = decimal.GetBits(value);
            var actual = decimal.GetBits(fast());
            for (var i = 0; i != 4; ++i) t.AreEqual(expected[i], actual[i]);
        }
    }
}
