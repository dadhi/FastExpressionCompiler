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
    public class Issue174_NullReferenceInSetter : ITest
    {
        public int Run()
        {
            PropertyAssignmentFromEqualityComparisonShouldWork();
            return 1;
        }

        [Test]
        public void PropertyAssignmentFromEqualityComparisonShouldWork()
        {
            var boolParameter = Parameter(typeof(ValueHolder<bool>), "boolValue");
            var boolValueProperty = Property(boolParameter, "Value");

            var decimalParameter = Parameter(typeof(ValueHolder<decimal>), "decimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");

            var expr = Lambda<Func<ValueHolder<decimal>, bool>>(
                Block(new[] { boolParameter },
                Assign(boolParameter, New(boolParameter.Type)),
                Assign(boolValueProperty, Equal(decimalValueProperty, Constant(decimal.One)))),
                decimalParameter);

            var source = new ValueHolder<decimal> { Value = 1 };

            var compiled = expr.CompileSys();
            Assert.IsTrue(compiled(source));

            var compiledFast = expr.CompileFast(true);
            Assert.IsTrue(compiledFast(source));
        }

        private class ValueHolder<T>
        {
            public T Value { get; set; }
        }
    }
}
