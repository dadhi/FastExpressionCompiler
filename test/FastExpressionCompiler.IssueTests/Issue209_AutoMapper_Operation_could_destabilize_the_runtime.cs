using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue209_AutoMapper_Operation_could_destabilize_the_runtime  : ITest
    {
        public int Run()
        {
            ShouldAlsoWork();
            return 1;
        }

        [Test]
        public void ShouldAlsoWork()
        {
            var srcParam = Parameter(typeof(OrderWithNullableStatus), "src");
            var destParam = Parameter(typeof(OrderDtoWithNullableStatus), "dest");
            var resolvedValueParam = Parameter(typeof(Status?), "resolvedValue");
            var propertyValueParam = Parameter(typeof(Status?), "propertyValue");

            var expression = Lambda<Func<OrderWithNullableStatus, OrderDtoWithNullableStatus>>(
                Block(typeof(OrderDtoWithNullableStatus), new[] { destParam, resolvedValueParam, propertyValueParam },
                    Assign(destParam, New(typeof(OrderDtoWithNullableStatus).GetConstructors()[0])),
                    Assign(resolvedValueParam, Property(srcParam, "Status")),
                    Assign(propertyValueParam, Condition(
                        Equal(resolvedValueParam, Constant(null)),
                        Default(typeof(Status?)),
                        Convert(Property(resolvedValueParam, "Value"), typeof(Status?)))),
                    Assign(Property(destParam, "Status"), propertyValueParam),
                    destParam
                ),
                srcParam
            );

            var compiled = expression.CompileFast(true);

            var src = new OrderWithNullableStatus
            {
                Status = Status.InProgress
            };

            var dest = compiled(src);

            Assert.IsNotNull(dest);
            Assert.AreEqual(Status.InProgress, dest.Status);
        }

        public enum Status
        {
            InProgress = 1,
            Complete = 2
        }

        public class OrderWithNullableStatus
        {
            public Status? Status { get; set; }
        }

        public class OrderDtoWithNullableStatus
        {
            public Status? Status { get; set; }
        }
    }
}
