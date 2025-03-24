using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class EqualityOperatorsTests : ITest
    {
        public int Run()
        {
            Greater_or_equal_than_DateTime_constant();
            Less_or_equal_than_DateTime_constant();

            Greater_or_equal_than_DateTime_parameter();
            Less_or_equal_than_DateTime_parameter();
            Complex_expression_with_DateTime_Strings_and_Int();
            Implicit_equal();

            return 6;
        }


        public void Greater_or_equal_than_DateTime_parameter()
        {
            System.Linq.Expressions.Expression<Func<DateTime, bool>> se =
                dt => dt >= DateTime.Now;
            var e = se.FromSysExpression();

            var f = e.CompileFast(true);

            Asserts.IsFalse(f(DateTime.Now - TimeSpan.FromDays(1)));
        }


        public void Less_or_equal_than_DateTime_parameter()
        {
            System.Linq.Expressions.Expression<Func<DateTime, bool>> se =
                dt => dt <= DateTime.Now;
            var e = se.FromSysExpression();

            var f = e.CompileFast(true);

            Asserts.IsFalse(f(DateTime.Now + TimeSpan.FromDays(1)));
        }


        public void Implicit_equal()
        {
            System.Linq.Expressions.Expression<Func<EntityWithImplicitEquality, Entity, bool>> se =
                (ewe, entity) => ewe == entity;
            var e = se.FromSysExpression();

            var f = e.CompileFast(true);
            var entityWithEquals = new EntityWithImplicitEquality { AvailableDate = DateTime.Now };
            var entity = new Entity { AvailableDate = entityWithEquals.AvailableDate };
            var value = f(entityWithEquals, entity);
            Asserts.IsTrue(value);
        }


        public void Complex_expression_with_DateTime_Strings_and_Int()
        {
            var dtNow = DateTime.Now;
            var version = new Entity { DateType = 1, AvailableDate = dtNow };
            var startTime = "b"; // a <= b
            var endTime = "x"; // y >= x
            var timeSliceId = "42";

            System.Linq.Expressions.Expression<Func<Entity, bool>> se = w =>
                w.DateType == version.DateType &&
                w.AvailableDate == version.AvailableDate &&
                string.Compare(w.StartTime, startTime) <= 0 &&
                string.Compare(w.EndTime, endTime) >= 0 &&
                w.Id != timeSliceId;
            var e = se.FromSysExpression();

            var f = e.CompileFast(true);

            var tested = new Entity
            {
                DateType = 1,
                AvailableDate = dtNow,
                StartTime = "a",
                EndTime = "y"
            };
            Asserts.IsTrue(f(tested));
        }


        public void Greater_or_equal_than_DateTime_constant()
        {
            var dtNow = Constant(DateTime.Now);
            var e = Lambda<Func<bool>>(
                GreaterThanOrEqual(Constant(DateTime.Now), dtNow));

            var f = e.CompileFast(true);

            Asserts.IsTrue(f());
        }


        public void Less_or_equal_than_DateTime_constant()
        {
            var dtNow = Constant(DateTime.Now);
            var e = Lambda<Func<bool>>(
                LessThanOrEqual(dtNow, Constant(DateTime.Now)));

            var f = e.CompileFast(true);

            Asserts.IsTrue(f());
        }

        public class Entity
        {
            public string Id { get; set; }
            public int DateType { get; set; }
            public string StartTime { get; set; }
            public string EndTime { get; set; }
            public DateTime AvailableDate { get; set; }
        }


        public class EntityWithImplicitEquality
        {
            public DateTime AvailableDate { get; set; }

            public static bool operator ==(EntityWithImplicitEquality ewe, Entity entity) => ewe?.AvailableDate == entity?.AvailableDate;
            public static bool operator !=(EntityWithImplicitEquality ewe, Entity entity) => ewe?.AvailableDate.Equals(entity?.AvailableDate) != true;

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (ReferenceEquals(obj, null))
                {
                    return false;
                }

                if (obj is EntityWithImplicitEquality other)
                    return AvailableDate == other.AvailableDate;
                return false;
            }

            public override int GetHashCode()
            {
                return AvailableDate.GetHashCode();
            }
        }
    }
}
