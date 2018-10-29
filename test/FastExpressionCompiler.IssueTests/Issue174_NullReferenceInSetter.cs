using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    public class Issue174_NullReferenceInSetter
    {
        [Test, Ignore("TODO")]
        public void PropertyAssignmentFromEqualityComparisonShouldWork()
        {
            var boolParameter = Parameter(typeof(ValueHolder<bool>), "boolValue");
            var boolValueProperty = Property(boolParameter, "Value");

            var decimalParameter = Parameter(typeof(ValueHolder<decimal>), "decimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");
            var decimalOne = Constant(decimal.One);
            var decimalPropertyEqualsOne = Equal(decimalValueProperty, decimalOne);

            var assignBoolProperty = Assign(boolValueProperty, decimalPropertyEqualsOne);

            var expr = Lambda<Func<ValueHolder<decimal>, bool>>(
                assignBoolProperty,
                decimalParameter);

            var source = new ValueHolder<decimal> { Value = 1 };

            var adapt = expr.CompileFast();

            // Throws NullReferenceException in ValueHolder<T> - '<Value>k__BackingField was null'
            var result = adapt(source);

            Assert.IsTrue(result);
        }

        private class ValueHolder<T>
        {
            public T Value { get; set; }
        }
    }
}