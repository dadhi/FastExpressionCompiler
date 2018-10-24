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
    public class Issue159_NumericConversions
    {
        [Test]
        public void UnsignedLongComparisonsWithConversionsShouldWork()
        {
            var ulongParameter = Parameter(typeof(ValueHolder<ulong>), "ulongValue");
            var ulongValueProperty = Property(ulongParameter, "Value");

            var intVariable = Variable(typeof(ValueHolder<int>), "intValue");
            var intValueProperty = Property(intVariable, "Value");

            var newIntHolder = Assign(intVariable, New(intVariable.Type));

            var ulongGtOrEqualToIntMinValue = GreaterThanOrEqual(
                Convert(ulongValueProperty, intValueProperty.Type),
                Constant(int.MinValue));

            var ulongLtOrEqualToIntMaxValue = LessThanOrEqual(
                ulongValueProperty,
                Convert(Constant(int.MaxValue), ulongValueProperty.Type));

            var ulongIsInIntRange = AndAlso(ulongGtOrEqualToIntMinValue, ulongLtOrEqualToIntMaxValue);

            var ulongAsInt = Convert(ulongValueProperty, typeof(int));
            var defaultInt = Default(intValueProperty.Type);
            var ulongValueOrDefault = Condition(ulongIsInIntRange, ulongAsInt, defaultInt);

            var intValueAssignment = Assign(intValueProperty, ulongValueOrDefault);

            var block = Block(
                new[] { intVariable },
                newIntHolder,
                intValueAssignment,
                intVariable);

            var ulongValueOrDefaultLambda = Lambda<Func<ValueHolder<ulong>, ValueHolder<int>>>(
                block,
                ulongParameter);

            var ulongValueOrDefaultFunc = ulongValueOrDefaultLambda.CompileFast();
            var result = ulongValueOrDefaultFunc.Invoke(new ValueHolder<ulong> { Value = ulong.MaxValue });

            Assert.AreEqual(default(int), result.Value);
        }

        [Test]
        public void FloatComparisonsWithConversionsShouldWork()
        {
            var floatParameter = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValueProperty = Property(floatParameter, "Value");

            var nullableShortVariable = Variable(typeof(ValueHolder<short?>), "nullableShort");
            var nullableShortValueProperty = Property(nullableShortVariable, "Value");

            var newShortHolder = Assign(nullableShortVariable, New(nullableShortVariable.Type));

            var floatGtOrEqualToShortMinValue = GreaterThanOrEqual(
                floatValueProperty,
                Convert(Constant(short.MinValue), floatValueProperty.Type));

            var floatLtOrEqualToShortMaxValue = LessThanOrEqual(
                floatValueProperty,
                Convert(Constant(short.MaxValue), floatValueProperty.Type));

            var floatIsInShortRange = AndAlso(floatGtOrEqualToShortMinValue, floatLtOrEqualToShortMaxValue);

            var floatAsNullableShort = Convert(floatValueProperty, nullableShortValueProperty.Type);
            var defaultNullableShort = Default(nullableShortValueProperty.Type);
            var floatValueOrDefault = Condition(floatIsInShortRange, floatAsNullableShort, defaultNullableShort);

            var shortValueAssignment = Assign(nullableShortValueProperty, floatValueOrDefault);

            var block = Block(
                new[] { nullableShortVariable },
                newShortHolder,
                shortValueAssignment,
                nullableShortVariable);

            var floatValueOrDefaultLambda = Lambda<Func<ValueHolder<float>, ValueHolder<short?>>>(
                block,
                floatParameter);

            var source = new ValueHolder<float> { Value = 532.00f };

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast();
            var result = floatValueOrDefaultFunc.Invoke(source);

            Assert.AreEqual((short)532, result.Value);
        }

        [Test]
        public void FloatComparisonsWithConversionsShouldWork2()
        {
            var floatParameter = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValueProperty = Property(floatParameter, "Value");

            var nullableShortVariable = Variable(typeof(ValueHolder<short>), "short");
            var nullableShortValueProperty = Property(nullableShortVariable, "Value");

            var newShortHolder = Assign(nullableShortVariable, New(nullableShortVariable.Type));

            var floatGtOrEqualToShortMinValue = GreaterThanOrEqual(
                floatValueProperty,
                Convert(Constant(short.MinValue), floatValueProperty.Type));

            var floatLtOrEqualToShortMaxValue = LessThanOrEqual(
                floatValueProperty,
                Convert(Constant(short.MaxValue), floatValueProperty.Type));

            var floatIsInShortRange = AndAlso(floatGtOrEqualToShortMinValue, floatLtOrEqualToShortMaxValue);

            var floatAsNullableShort = Convert(floatValueProperty, nullableShortValueProperty.Type);
            var defaultNullableShort = Default(nullableShortValueProperty.Type);
            var floatValueOrDefault = Condition(floatIsInShortRange, floatAsNullableShort, defaultNullableShort);

            var shortValueAssignment = Assign(nullableShortValueProperty, floatValueOrDefault);

            var block = Block(
                new[] { nullableShortVariable },
                newShortHolder,
                shortValueAssignment,
                nullableShortVariable);

            var floatValueOrDefaultLambda = Lambda<Func<ValueHolder<float>, ValueHolder<short>>>(
                block,
                floatParameter);

            var source = new ValueHolder<float> { Value = 532.00f };

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast();
            var result = floatValueOrDefaultFunc.Invoke(source);

            Assert.AreEqual((short)532, result.Value);
        }

        // todo: problem is not in conversion but rather in no dup newed value
        [Test]//, Ignore("Fails")]
        public void NullableIntToDoubleCastsShouldWork()
        {
            // constructs this function
            ValueHolder<double> Adapt(ValueHolder<int?> nullableIntHolder)
            {
                var vd = new ValueHolder<double>();
                vd.Value = (double)nullableIntHolder.Value;
                return vd;
            }

            var nullableIntHolderParam = Parameter(typeof(ValueHolder<int?>), "nullableIntHolder");
            var doubleHolderVar = Variable(typeof(ValueHolder<double>), "doubleHolder");
            var nullableIntHolderValueProp = Property(nullableIntHolderParam, "Value");
            var doubleHolderValueProp = Property(doubleHolderVar, "Value");

            var block = Block(
                new[] { doubleHolderVar },
                Assign(doubleHolderVar, New(doubleHolderVar.Type)),
                Assign(doubleHolderValueProp, Convert(nullableIntHolderValueProp, doubleHolderValueProp.Type)),
                doubleHolderVar);

            var adaptExpr = Lambda<Func<ValueHolder<int?>, ValueHolder<double>>>(
                block,
                nullableIntHolderParam);

            var adapt = adaptExpr.CompileFast();

            var result = adapt(new ValueHolder<int?> { Value = 321 });
            Assert.AreEqual(321d, result.Value);
        }

        [Test, Ignore("Fails")]
        public void NullableDecimalToDoubleCastsShouldWork()
        {
            var decimalParameter = Parameter(typeof(ValueHolder<decimal?>), "nullableDecimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");

            var doubleVariable = Variable(typeof(ValueHolder<double>), "double");
            var doubleValueProperty = Property(doubleVariable, "Value");

            var newDoubleHolder = Assign(doubleVariable, New(doubleVariable.Type));

            var nullableDecimalAsDouble = Convert(decimalValueProperty, doubleValueProperty.Type);
            var doubleValueAssignment = Assign(doubleValueProperty, nullableDecimalAsDouble);

            var block = Block(
                new[] { doubleVariable },
                newDoubleHolder,
                doubleValueAssignment,
                doubleVariable);

            var convertedDecimalValueLambda = Lambda<Func<ValueHolder<decimal?>, ValueHolder<double>>>(
                block,
                decimalParameter);

            var source = new ValueHolder<decimal?> { Value = 938378.637m };

            var convertedDecimalValueFunc = convertedDecimalValueLambda.CompileFast();
            var result = convertedDecimalValueFunc.Invoke(source);

            Assert.AreEqual(938378.637d, result.Value);
        }

        [Test, Ignore("Fails")]
        public void DecimalToNullableDoubleCastsShouldWork()
        {
            var decimalParameter = Parameter(typeof(ValueHolder<decimal>), "decimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");

            var nullableDoubleVariable = Variable(typeof(ValueHolder<double?>), "nullableDouble");
            var doubleValueProperty = Property(nullableDoubleVariable, "Value");

            var newDoubleHolder = Assign(nullableDoubleVariable, New(nullableDoubleVariable.Type));

            var decimalAsNullableDouble = Convert(decimalValueProperty, doubleValueProperty.Type);
            var doubleValueAssignment = Assign(doubleValueProperty, decimalAsNullableDouble);

            var block = Block(
                new[] { nullableDoubleVariable },
                newDoubleHolder,
                doubleValueAssignment,
                nullableDoubleVariable);

            var convertedDecimalValueLambda = Lambda<Func<ValueHolder<decimal>, ValueHolder<double?>>>(
                block,
                decimalParameter);

            var source = new ValueHolder<decimal> { Value = 5332.00m };

            var convertedDecimalValueFunc = convertedDecimalValueLambda.CompileFast();
            var result = convertedDecimalValueFunc.Invoke(source);

            Assert.AreEqual(5332d, result.Value);
        }

        private class ValueHolder<T>
        {
            public T Value { get; set; }
        }
    }
}