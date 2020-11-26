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
    public class Issue204_Operation_could_destabilize_the_runtime__AutoMapper : ITest
    {
        public int Run()
        {
            Default_with_struct();
            ShouldAlsoWork();
            return 2;
        }

        public enum Status
        {
            InProgress = 0,
            Complete = 1
        }

        public class OrderWithNullableStatus
        {
            public Status? Status { get; set; }
        }

        public class OrderDtoWithNullableStatus
        {
            public Status? Status { get; set; }
        }

        [Test]
        public void Default_with_struct()
        {
            var expression = Lambda<Func<Status?>>(Default(typeof(Status?)));

            var fastCompiled = expression.CompileFast(true);
            var status = fastCompiled();
            Assert.AreEqual(default(Status?), status);
        }

        /* Test expression:
            src =>
            {
                var dest = new AnotherFastExpressionCompilerBug.OrderDtoWithNullableStatus();
                var resolvedValue = src.Status;
                var propertyValue = (resolvedValue == null) ? null : (AnotherFastExpressionCompilerBug.Status?)resolvedValue.Value;
                dest.Status = propertyValue;

                return dest;
            }
         */
        [Test]
        public void ShouldAlsoWork()
        {
            var srcParam = Parameter(typeof(OrderWithNullableStatus), "src");
            var destParam = Parameter(typeof(OrderDtoWithNullableStatus), "dest");
            var resolvedValueParam = Parameter(typeof(Status?), "resolvedValue");
            var propertyValueParam = Parameter(typeof(Status?), "propertyValue");

            var e = Lambda<Func<OrderWithNullableStatus, OrderDtoWithNullableStatus>>(
                Block(typeof(OrderDtoWithNullableStatus), new[] { destParam, resolvedValueParam, propertyValueParam },
                    Assign(destParam, New(typeof(OrderDtoWithNullableStatus).GetConstructors()[0])),
                    Assign(resolvedValueParam, Property(srcParam, "Status")),
                    Assign(propertyValueParam, 
                        Condition(
                            Equal(resolvedValueParam, Constant(null)),
                            Default(typeof(Status?)),
                            Convert(Property(resolvedValueParam, "Value"), typeof(Status?)))),
                    Assign(Property(destParam, "Status"), propertyValueParam),
                    destParam
                ),
                srcParam
            );

            e.PrintCSharp();

            var inProgress = new OrderWithNullableStatus { Status = Status.InProgress };
            var complete = new OrderWithNullableStatus { Status = Status.Complete };

            var fs = e.CompileSys();
            fs.PrintIL();
            
            var dest = fs(inProgress);
            Assert.AreEqual(Status.InProgress, dest.Status);
            dest = fs(complete);
            Assert.AreEqual(Status.Complete, dest.Status);

            var f = e.CompileFast(true);
            f.PrintIL();
            dest = f(inProgress);
            Assert.AreEqual(Status.InProgress, dest.Status);
            dest = f(complete);
            Assert.AreEqual(Status.Complete, dest.Status);
        }
    }
}
