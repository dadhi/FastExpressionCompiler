using NUnit.Framework;
using System;
using System.Linq.Expressions;
#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue314_LiftToNull_ToExpressionString
    {
        public int Run()
        {
            LiftToNull();
            CustomMethod();
            DateTimeConstant();
            NullableDateTimeConstant();
            return 4;
        }

        [Test]
        public void LiftToNull()
        {
            var p = Parameter(typeof(int), "tmp0");
            var expr = MakeBinary(
                ExpressionType.Add,
                Constant(1, typeof(int?)),
                Constant(null, typeof(int?)),
                liftToNull: true,
                null
            );

            var str = expr.ToExpressionString(out _, out _, out _, true);

            // for Add and other arithmetics operations the liftToNull is ignored so we won't repro it in the final expression.
            Assert.AreEqual(
                "var e = new Expression[2]; // the unique expressions" + Environment.NewLine +
                "var expr = MakeBinary(ExpressionType.Add," + Environment.NewLine +
                "  e[0]=Constant(1, typeof(int?))," + Environment.NewLine +
                "  e[1]=Constant(null, typeof(int?))," + Environment.NewLine +
                "  liftToNull: true," + Environment.NewLine +
                "  null);"
                , str);
        }

        [Test]
        public void CustomMethod()
        {
            var x = Parameter(typeof(A), "x");
            var expr = Add(x, x);

            var str = expr.ToExpressionString(out _, out _, out _, true);

            Assert.AreEqual(
                @"var p = new ParameterExpression[1]; // the parameter expressions" + Environment.NewLine +
                @"var expr = MakeBinary(ExpressionType.Add," + Environment.NewLine +
                @"  p[0]=Parameter(typeof(Issue314_LiftToNull_ToExpressionString.A), ""x"")," + Environment.NewLine +
                @"  p[0 // (Issue314_LiftToNull_ToExpressionString.A x)" + Environment.NewLine +
                @"    ]," + Environment.NewLine +
                @"  liftToNull: false," + Environment.NewLine +
                @"  typeof(Issue314_LiftToNull_ToExpressionString.A).GetMethods().Single(x => !x.IsGenericMethod && x.Name == ""op_Addition"" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Issue314_LiftToNull_ToExpressionString.A), typeof(Issue314_LiftToNull_ToExpressionString.A) })));"
                , str);
        }

        [Test]
        public void DateTimeConstant()
        {
            var dt = new DateTime(2020, 3, 13);
            var expr = Constant(dt);

            var str = expr.ToExpressionString();

            Assert.AreEqual(@$"var expr = Constant(DateTime.Parse(""{dt}""));", str);
        }

        [Test]
        public void NullableDateTimeConstant()
        {
            var dt = new DateTime(2020, 3, 13);
            var expr = Constant(dt, typeof(DateTime?));

            var str = expr.ToExpressionString();

            Assert.AreEqual(@$"var expr = Constant(DateTime.Parse(""{dt}""), typeof(System.DateTime?));", str);
        }

        class A
        {
            public static A operator +(A x, A y) { return new A(); }
        }
    }
}
