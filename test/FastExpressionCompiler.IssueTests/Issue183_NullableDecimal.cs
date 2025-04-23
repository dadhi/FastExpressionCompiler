using System;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unassigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue183_NullableDecimal : ITestX
{
    public void Run(TestRun t)
    {
        ConvertNullNullableParamToNullableDecimal_CheckAgainstTheSystemExprCompile(t);
        ConvertDecimalParamToNullableDecimal(t);
        ConvertDecimalPropertyToNullableDecimal(t);
        ConvertNullableBytePropertyToNullableDecimal(t);
        NullableDecimalIssue(t);
    }

    public void ConvertDecimalParamToNullableDecimal(TestContext t)
    {
        var param = Parameter(typeof(decimal), "d");

        var f = Lambda<Func<decimal, decimal?>>(Convert(param, typeof(decimal?)), param).CompileFast(true);
        var x = f(42);

        t.IsNotNull(x);
        t.AreEqual(42, x.Value);
    }

    public void ConvertNullNullableParamToNullableDecimal_CheckAgainstTheSystemExprCompile(TestContext t)
    {
        var ps = Parameter(typeof(byte?), "b");
        var e = Lambda<Func<byte?, decimal?>>(Convert(ps, typeof(decimal?)), ps);
        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();
        var xs = fs(null);
        t.IsNull(xs);

        var ff = e.CompileFast(true);
        ff.PrintIL();
        var xf = ff(null);
        t.IsNull(xf);
    }

    public void ConvertDecimalPropertyToNullableDecimal(TestContext t)
    {
        var param = Parameter(typeof(DecimalContainer), "d");

        var f = Lambda<Func<DecimalContainer, decimal?>>(
            Convert(Property(param, nameof(DecimalContainer.Decimal)), typeof(decimal?)),
            param
            ).CompileFast(true);

        var x = f(new DecimalContainer { Decimal = 42 });

        t.IsNotNull(x);
        t.AreEqual(42, x.Value);
    }

    public void ConvertNullableBytePropertyToNullableDecimal(TestContext t)
    {
        var param = Parameter(typeof(DecimalContainer), "d");

        var f = Lambda<Func<DecimalContainer, decimal?>>(
            Convert(Property(param, nameof(DecimalContainer.NullableByte)), typeof(decimal?)),
            param
        ).CompileFast(true);

        var x = f(new DecimalContainer { NullableByte = 42 });

        t.IsNotNull(x);
        t.AreEqual(42, x.Value);
    }

    public void NullableDecimalIssue(TestContext t)
    {
        var param = Parameter(typeof(DecimalContainer));

        var body = Equal(
            Convert(Property(param, nameof(DecimalContainer.NullableByte)), typeof(decimal?)),
            Convert(Property(param, nameof(DecimalContainer.Decimal)), typeof(decimal?)));

        var f = Lambda<Func<DecimalContainer, bool>>(body, param).CompileFast(true);

        var x = f(new DecimalContainer { Decimal = 1 });
        t.IsFalse(x); // cause byte? to decimal? would be `null`
    }
}

public class DecimalContainer
{
    public byte? NullableByte { get; set; }
    public decimal Decimal { get; set; }
}
