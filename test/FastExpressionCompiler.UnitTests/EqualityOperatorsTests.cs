using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.UnitTests
{
    [TestFixture]
    public class EqualityOperatorsTests
    {
        [Test]
        public void Greater_or_equal_than_DateTime_parameter()
        {
            Expression<Func<DateTime, bool>> e =
                dt => dt >= DateTime.Now;

            var f = e.CompileFast(true);

            Assert.IsFalse(f(DateTime.Now - TimeSpan.FromDays(1)));
        }

        [Test]
        public void Less_or_equal_than_DateTime_parameter()
        {
            Expression<Func<DateTime, bool>> e =
                dt => dt <= DateTime.Now;

            var f = e.CompileFast(true);

            Assert.IsFalse(f(DateTime.Now + TimeSpan.FromDays(1)));
        }

        [Test]
        public void Greater_or_equal_than_DateTime_constant()
        {
            var dtNow = Expression.Constant(DateTime.Now);
            var e = Expression.Lambda<Func<bool>>(
                Expression.GreaterThanOrEqual(Expression.Constant(DateTime.Now), dtNow));

            var f = e.CompileFast(true);

            Assert.IsTrue(f());
        }

        [Test]
        public void Less_or_equal_than_DateTime_constant()
        {
            var dtNow = Expression.Constant(DateTime.Now);
            var e = Expression.Lambda<Func<bool>>(
                Expression.LessThanOrEqual(dtNow, Expression.Constant(DateTime.Now)));

            var f = e.CompileFast(true);

            Assert.IsTrue(f());
        }
    }
}
