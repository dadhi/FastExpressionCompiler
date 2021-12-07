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
            //LiftToNull();
            //CustomMethod();
            DateTimeConstant();
            NullableDateTimeConstant();
            return 4;
        }

        [Test, Ignore("todo: fix liftToNull: true")]
        public void LiftToNull()
        {
            var p = Parameter(typeof(int), "tmp0");
            var expr = Expression.MakeBinary(
                ExpressionType.Add,
                Expression.Constant(1, typeof(int?)),
                Expression.Constant(null, typeof(int?)),
                liftToNull: true,
                null
            );

            var str = expr.ToExpressionString(out _, out _, out _, true);
            Console.WriteLine(str);
            Assert.AreEqual(
                "var e = new Expression[2]; // the unique expressions" + Environment.NewLine +
                "var expr = MakeBinary(ExpressionType.Add," + Environment.NewLine +
                "  e[0]=Constant(1, typeof(int?))," + Environment.NewLine +
                "  e[1]=Constant(null, typeof(int?))," + Environment.NewLine +
                "  liftToNull: true," + Environment.NewLine +
                "  null", str);
        }

        [Test, Ignore("todo: fix liftToNull: true")]
        public void CustomMethod()
        {
            var x = Parameter(typeof(A), "x");
            var expr = Expression.Add(x, x);
            var str = expr.ToExpressionString(out _, out _, out _, true);
            Console.WriteLine(str);
            Assert.AreEqual(
                @"var p = new ParameterExpression[1]; // the parameter expressions" + Environment.NewLine +
                @"var expr = MakeBinary(ExpressionType.Add," + Environment.NewLine +
                @"  p[0]=Parameter(typeof(Issue314_LiftToNull_ToExpressionString.A), ""x"")," + Environment.NewLine +
                @"  p[0 // (Issue314_LiftToNull_ToExpressionString.A x)"+ Environment.NewLine +
                @"    ],"+ Environment.NewLine +
                @"  liftToNull: false," + Environment.NewLine +
                @"  typeof(Issue314_LiftToNull_ToExpressionString.A).GetMethods().Single(x => !x.IsGenericMethod && x.Name == ""op_Addition"" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Issue314_LiftToNull_ToExpressionString.A), typeof(Issue314_LiftToNull_ToExpressionString.A) })));" + Environment.NewLine
                , str);
        }
        [Test]
        public void DateTimeConstant()
        {
            var expr = Expression.Constant(new DateTime(2020, 3, 13));
            var str = expr.ToExpressionString();
            Console.WriteLine(str);
            Assert.AreEqual(@"var expr = Constant(DateTime.Parse(""3/13/2020 12:00:00 AM""));", str);
        }
        [Test]
        public void NullableDateTimeConstant()
        {
            var expr = Expression.Constant(new DateTime(2020, 3, 13), typeof(DateTime?));
            var str = expr.ToExpressionString();
            Console.WriteLine(str);
            Assert.AreEqual(@"var expr = Constant(DateTime.Parse(""3/13/2020 12:00:00 AM""), typeof(System.DateTime?));", str);
        }

        class A {
            public static A operator +(A x, A y) { return new A(); }
        }
    }
}
