using System;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unassigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue183_NullableDecimal : ITest
    {
        public int Run()
        {
            ConvertDecimalParamToNullableDecimal();
            ConvertNullNullableParamToNullableDecimal_CheckAgainstTheSystemExprCompile();
            ConvertDecimalPropertyToNullableDecimal();
            ConvertNullableBytePropertyToNullableDecimal();
            NullableDecimalIssue();
            return 5;
        }

        [Test]
        public void ConvertDecimalParamToNullableDecimal()
        {
            var param = Parameter(typeof(decimal), "d");

            var f = Lambda<Func<decimal, decimal?>>(Convert(param, typeof(decimal?)), param).CompileFast(true);
            var x = f(42);

            Assert.IsNotNull(x);
            Assert.AreEqual(42, x.Value);
        }

        [Test]
        public void ConvertNullNullableParamToNullableDecimal_CheckAgainstTheSystemExprCompile()
        {
            var ps = Parameter(typeof(byte?), "b");
            var fs = Lambda<Func<byte?, decimal?>>(
                Convert(ps, typeof(decimal?)), ps)
                .CompileSys();
            var xs = fs(null);
            Assert.IsNull(xs);

            var param = Parameter(typeof(byte?), "b");
            var ff = Lambda<Func<byte?, decimal?>>(Convert(param, typeof(decimal?)), param)
                .CompileFast(true);
            var xf = ff(null);
            Assert.IsNull(xf);
        }

        [Test]
        public void ConvertDecimalPropertyToNullableDecimal()
        {
            var param = Parameter(typeof(DecimalContainer), "d");

            var f = Lambda<Func<DecimalContainer, decimal?>>(
                Convert(Property(param, nameof(DecimalContainer.Decimal)), typeof(decimal?)), 
                param
                ).CompileFast(true);

            var x = f(new DecimalContainer { Decimal = 42 });

            Assert.IsNotNull(x);
            Assert.AreEqual(42, x.Value);
        }

        [Test]
        public void ConvertNullableBytePropertyToNullableDecimal()
        {
            var param = Parameter(typeof(DecimalContainer), "d");

            var f = Lambda<Func<DecimalContainer, decimal?>>(
                Convert(Property(param, nameof(DecimalContainer.NullableByte)), typeof(decimal?)),
                param
            ).CompileFast(true);

            var x = f(new DecimalContainer { NullableByte = 42 });

            Assert.IsNotNull(x);
            Assert.AreEqual(42, x.Value);
        }

        [Test]
        public void NullableDecimalIssue()
        {
            var param = Parameter(typeof(DecimalContainer));

            var body = Equal(
                Convert(Property(param, nameof(DecimalContainer.NullableByte)), typeof(decimal?)), 
                Convert(Property(param, nameof(DecimalContainer.Decimal)), typeof(decimal?)));

            var f = Lambda<Func<DecimalContainer, bool>>(body, param).CompileFast(true);

            var x = f(new DecimalContainer { Decimal = 1 });
            Assert.IsFalse(x); // cause byte? to decimal? would be `null`
        }
    }

    public class DecimalContainer
    {
        public byte? NullableByte { get; set; }
        public decimal Decimal { get; set; }
    }
}
