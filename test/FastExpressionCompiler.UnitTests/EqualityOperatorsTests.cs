using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class EqualityOperatorsTests : ITest
    {
        public int Run()
        {
            Greater_or_equal_than_DateTime_constant();
            Less_or_equal_than_DateTime_constant();

#if !LIGHT_EXPRESSION
            Greater_or_equal_than_DateTime_parameter();
            Less_or_equal_than_DateTime_parameter();
            Complex_expression_with_DateTime_Strings_and_Int();
            return 5;
#else
            return 2;
#endif        
}

#if !LIGHT_EXPRESSION
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
        public void Complex_expression_with_DateTime_Strings_and_Int()
        {
            var dtNow = DateTime.Now;
            var version = new Entity { DateType = 1, AvailableDate = dtNow };
            var startTime = "b"; // a <= b
            var endTime = "x"; // y >= x
            var timeSliceId = "42";

            Expression<Func<Entity, bool>> e = w =>
                w.DateType == version.DateType &&
                w.AvailableDate == version.AvailableDate &&
                string.Compare(w.StartTime, startTime) <= 0 &&
                string.Compare(w.EndTime, endTime) >= 0 &&
                w.Id != timeSliceId;

            var f = e.CompileFast(true);


            var tested = new Entity
            {
                DateType = 1, AvailableDate = dtNow,
                StartTime = "a", EndTime = "y"
            };
            Assert.IsTrue(f(tested));
        }
#endif

        [Test]
        public void Greater_or_equal_than_DateTime_constant()
        {
            var dtNow = Constant(DateTime.Now);
            var e = Lambda<Func<bool>>(
                GreaterThanOrEqual(Constant(DateTime.Now), dtNow));

            var f = e.CompileFast(true);

            Assert.IsTrue(f());
        }

        [Test]
        public void Less_or_equal_than_DateTime_constant()
        {
            var dtNow = Constant(DateTime.Now);
            var e = Lambda<Func<bool>>(
                LessThanOrEqual(dtNow, Constant(DateTime.Now)));

            var f = e.CompileFast(true);

            Assert.IsTrue(f());
        }

        public class Entity
        {
            public string Id { get; set; }
            public int  DateType { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public DateTime AvailableDate { get; set; }
        }
    }
}
