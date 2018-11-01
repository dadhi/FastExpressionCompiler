using System;
using System.Reflection;
using System.Reflection.Emit;
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

        [Test]//, Ignore("Todo: fix me")]
        public void UnsignedNullableLongComparisonsWithConversionsShouldWork()
        {
            var ulongParameter = Parameter(typeof(ValueHolder<ulong?>), "nullableUlongValue");
            var ulongValueProperty = Property(ulongParameter, "Value");

            var intVariable = Variable(typeof(ValueHolder<int?>), "nullableIntValue");
            var intValueProperty = Property(intVariable, "Value");

            var ulongValueOrDefault = Condition(
                LessThanOrEqual(ulongValueProperty, Convert(Constant(int.MaxValue), ulongValueProperty.Type)),
                Convert(ulongValueProperty, typeof(int?)),
                Default(intValueProperty.Type));

            var block = Block(
                new[] { intVariable },
                Assign(intVariable, New(intVariable.Type)),
                Assign(intValueProperty, ulongValueOrDefault),
                intVariable);

            var ulongValueOrDefaultLambda = Lambda<Func<ValueHolder<ulong?>, ValueHolder<int?>>>(
                block,
                ulongParameter);

            var expected = ulongValueOrDefaultLambda.CompileSys();
            var expectedResult = expected(new ValueHolder<ulong?> { Value = ulong.MaxValue });
            Assert.AreEqual(default(int?), expectedResult.Value);

            var actual = ulongValueOrDefaultLambda.CompileFast();
            var actualResult = actual(new ValueHolder<ulong?> { Value = ulong.MaxValue });
            Assert.AreEqual(default(int?), actualResult.Value);
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

        [Test, Ignore("Common Language Runtime detected an invalid program")]
        public void FloatComparisonsWithConversionsShouldWork3()
        {
            var floatParameter = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValueProperty = Property(floatParameter, "Value");

            var nullableDecimalVariable = Variable(typeof(ValueHolder<decimal?>), "nullableDecimal");
            var nullableDecimalValueProperty = Property(nullableDecimalVariable, "Value");

            var newDecimalHolder = Assign(nullableDecimalVariable, New(nullableDecimalVariable.Type));

            var floatGtOrEqualToDecimalMinValue = GreaterThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MinValue), floatValueProperty.Type));

            var floatLtOrEqualToDecimalMaxValue = LessThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MaxValue), floatValueProperty.Type));

            var floatIsInDecimalRange = AndAlso(floatGtOrEqualToDecimalMinValue, floatLtOrEqualToDecimalMaxValue);

            var floatAsNullableDecimal = Convert(floatValueProperty, nullableDecimalValueProperty.Type);
            var defaultNullableDecimal = Default(nullableDecimalValueProperty.Type);
            var floatValueOrDefault = Condition(floatIsInDecimalRange, floatAsNullableDecimal, defaultNullableDecimal);

            var decimalValueAssignment = Assign(nullableDecimalValueProperty, floatValueOrDefault);

            var block = Block(
                new[] { nullableDecimalVariable },
                newDecimalHolder,
                decimalValueAssignment,
                nullableDecimalVariable);

            var floatValueOrDefaultLambda = Lambda<Func<ValueHolder<float>, ValueHolder<decimal?>>>(
                block,
                floatParameter);

            var source = new ValueHolder<float> { Value = float.MaxValue };

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast();
            var result = floatValueOrDefaultFunc.Invoke(source);

            Assert.IsNull(result.Value);
        }

        [Test, Ignore("Common Language Runtime detected an invalid program")]
        public void FloatComparisonsWithConversionsShouldWork4()
        {
            var floatParameter = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValueProperty = Property(floatParameter, "Value");

            var decimalVariable = Variable(typeof(ValueHolder<decimal>), "decimal");
            var decimalValueProperty = Property(decimalVariable, "Value");

            var newDecimalHolder = Assign(decimalVariable, New(decimalVariable.Type));

            var floatGtOrEqualToDecimalMinValue = GreaterThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MinValue), floatValueProperty.Type));

            var floatLtOrEqualToDecimalMaxValue = LessThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MaxValue), floatValueProperty.Type));

            var floatIsInDecimalRange = AndAlso(floatGtOrEqualToDecimalMinValue, floatLtOrEqualToDecimalMaxValue);

            var floatAsDecimal = Convert(floatValueProperty, decimalValueProperty.Type);
            var defaultDecimal = Default(decimalValueProperty.Type);
            var floatValueOrDefault = Condition(floatIsInDecimalRange, floatAsDecimal, defaultDecimal);

            var decimalValueAssignment = Assign(decimalValueProperty, floatValueOrDefault);

            var block = Block(
                new[] { decimalVariable },
                newDecimalHolder,
                decimalValueAssignment,
                decimalVariable);

            var floatValueOrDefaultLambda = Lambda<Func<ValueHolder<float>, ValueHolder<decimal>>>(
                block,
                floatParameter);

            var source = new ValueHolder<float> { Value = 8532.00f };

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast();
            var result = floatValueOrDefaultFunc.Invoke(source);

            Assert.AreEqual(8532.00m, result.Value);
        }

        [Test, Ignore("Common Language Runtime detected an invalid program")]
        public void NullableFloatComparisonsWithConversionsShouldWork()
        {
            var floatParameter = Parameter(typeof(ValueHolder<float?>), "nullableFloatValue");
            var floatValueProperty = Property(floatParameter, "Value");

            var nullableDecimalVariable = Variable(typeof(ValueHolder<decimal?>), "nullableDecimal");
            var nullableDecimalValueProperty = Property(nullableDecimalVariable, "Value");

            var newDecimalHolder = Assign(nullableDecimalVariable, New(nullableDecimalVariable.Type));

            var floatGtOrEqualToDecimalMinValue = GreaterThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MinValue), floatValueProperty.Type));

            var floatLtOrEqualToDecimalMaxValue = LessThanOrEqual(
                floatValueProperty,
                Convert(Constant(decimal.MaxValue), floatValueProperty.Type));

            var floatIsInDecimalRange = AndAlso(floatGtOrEqualToDecimalMinValue, floatLtOrEqualToDecimalMaxValue);

            var floatAsNullableDecimal = Convert(floatValueProperty, nullableDecimalValueProperty.Type);
            var defaultNullableDecimal = Default(nullableDecimalValueProperty.Type);
            var floatValueOrDefault = Condition(floatIsInDecimalRange, floatAsNullableDecimal, defaultNullableDecimal);

            var decimalValueAssignment = Assign(nullableDecimalValueProperty, floatValueOrDefault);

            var block = Block(
                new[] { nullableDecimalVariable },
                newDecimalHolder,
                decimalValueAssignment,
                nullableDecimalVariable);

            var floatValueOrDefaultLambda = Lambda<Func<ValueHolder<float?>, ValueHolder<decimal?>>>(
                block,
                floatParameter);

            var source = new ValueHolder<float?> { Value = 73.62f };

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast();
            var result = floatValueOrDefaultFunc.Invoke(source);

            Assert.AreEqual(73.62m, result.Value);
        }

        [Test, Ignore("Common Language Runtime detected an invalid program")]
        public void DoubleComparisonsWithConversionsShouldWork()
        {
            var doubleParameter = Parameter(typeof(ValueHolder<double>), "doubleValue");
            var doubleValueProperty = Property(doubleParameter, "Value");

            var nullableDecimalVariable = Variable(typeof(ValueHolder<decimal?>), "nullableDecimal");
            var nullableDecimalValueProperty = Property(nullableDecimalVariable, "Value");

            var newDecimalHolder = Assign(nullableDecimalVariable, New(nullableDecimalVariable.Type));

            var doubleGtOrEqualToDecimalMinValue = GreaterThanOrEqual(
                doubleValueProperty,
                Convert(Constant(decimal.MinValue), doubleValueProperty.Type));

            var doubleLtOrEqualToDecimalMaxValue = LessThanOrEqual(
                doubleValueProperty,
                Convert(Constant(decimal.MaxValue), doubleValueProperty.Type));

            var doubleIsInDecimalRange = AndAlso(doubleGtOrEqualToDecimalMinValue, doubleLtOrEqualToDecimalMaxValue);

            var doubleAsNullableDecimal = Convert(doubleValueProperty, nullableDecimalValueProperty.Type);
            var defaultNullableDecimal = Default(nullableDecimalValueProperty.Type);
            var doubleValueOrDefault = Condition(doubleIsInDecimalRange, doubleAsNullableDecimal, defaultNullableDecimal);

            var decimalValueAssignment = Assign(nullableDecimalValueProperty, doubleValueOrDefault);

            var block = Block(
                new[] { nullableDecimalVariable },
                newDecimalHolder,
                decimalValueAssignment,
                nullableDecimalVariable);

            var doubleValueOrDefaultLambda = Lambda<Func<ValueHolder<double>, ValueHolder<decimal?>>>(
                block,
                doubleParameter);

            var source = new ValueHolder<double> { Value = double.MaxValue };

            var doubleValueOrDefaultFunc = doubleValueOrDefaultLambda.CompileFast();
            var result = doubleValueOrDefaultFunc.Invoke(source);

            Assert.IsNull(result.Value);
        }

        [Test, Ignore("Common Language Runtime detected an invalid program")]
        public void DoubleComparisonsWithConversionsShouldWork2()
        {
            var doubleParameter = Parameter(typeof(ValueHolder<double>), "doubleValue");
            var doubleValueProperty = Property(doubleParameter, "Value");

            var decimalVariable = Variable(typeof(ValueHolder<decimal>), "decimal");
            var decimalValueProperty = Property(decimalVariable, "Value");

            var newDecimalHolder = Assign(decimalVariable, New(decimalVariable.Type));

            var doubleGtOrEqualToDecimalMinValue = GreaterThanOrEqual(
                doubleValueProperty,
                Convert(Constant(decimal.MinValue), doubleValueProperty.Type));

            var doubleLtOrEqualToDecimalMaxValue = LessThanOrEqual(
                doubleValueProperty,
                Convert(Constant(decimal.MaxValue), doubleValueProperty.Type));

            var doubleIsInDecimalRange = AndAlso(doubleGtOrEqualToDecimalMinValue, doubleLtOrEqualToDecimalMaxValue);

            var doubleAsDecimal = Convert(doubleValueProperty, decimalValueProperty.Type);
            var defaultDecimal = Default(decimalValueProperty.Type);
            var doubleValueOrDefault = Condition(doubleIsInDecimalRange, doubleAsDecimal, defaultDecimal);

            var decimalValueAssignment = Assign(decimalValueProperty, doubleValueOrDefault);

            var block = Block(
                new[] { decimalVariable },
                newDecimalHolder,
                decimalValueAssignment,
                decimalVariable);

            var doubleValueOrDefaultLambda = Lambda<Func<ValueHolder<double>, ValueHolder<decimal>>>(
                block,
                doubleParameter);

            var source = new ValueHolder<double> { Value = double.MinValue };

            var doubleValueOrDefaultFunc = doubleValueOrDefaultLambda.CompileFast();
            var result = doubleValueOrDefaultFunc.Invoke(source);

            Assert.AreEqual(default(decimal), result.Value);
        }

        // Expression used for the tests below
        //private ValueHolder<double> Adapt(ValueHolder<int?> nullableIntHolder)
        //{
        //    var vd = new ValueHolder<double>();
        //    vd.Value = (double)nullableIntHolder.Value;
        //    return vd;
        //}

        [Test]
        public void NullableIntToDoubleCastsShouldWork()
        {
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
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be replaced with dup #
                OpCodes.Ldloc_0, // 
                OpCodes.Ldarg_0,
                OpCodes.Call, // ValueHolder<int?>.get_Value
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,
                OpCodes.Call, // int?.get_Value
                OpCodes.Conv_R8,
                OpCodes.Call, // ValueHolder<double>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(new ValueHolder<int?> { Value = 321 });
            Assert.AreEqual(321d, result.Value);
        }

        [Test]
        public void NullableIntToDoubleCastsShouldWork_with_MemberInit()
        {
            var nullableIntHolderParam = Parameter(typeof(ValueHolder<int?>), "nullableIntHolder");

            var memberInit = MemberInit(New(typeof(ValueHolder<double>)),
                Bind(typeof(ValueHolder<double>).GetTypeInfo().GetDeclaredProperty(nameof(ValueHolder<double>.Value)),
                    Convert(Property(nullableIntHolderParam, "Value"), typeof(double))));

            var adaptExpr = Lambda<Func<ValueHolder<int?>, ValueHolder<double>>>(
                memberInit, nullableIntHolderParam);

            var adapt = adaptExpr.CompileFast();

            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Dup,
                OpCodes.Ldarg_0,
                OpCodes.Call, // ValueHolder<int?>.get_Value
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call, // int?.get_Value
                OpCodes.Conv_R8,
                OpCodes.Call, // ValueHolder<double>.set_Value 
                OpCodes.Ret);

            var result = adapt(new ValueHolder<int?> { Value = 321 });
            Assert.AreEqual(321d, result.Value);
        }

        [Test]
        public void NullableDecimalToDoubleCastsShouldWork()
        {
            var decimalParameter = Parameter(typeof(ValueHolder<decimal?>), "nullableDecimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");

            var doubleVariable = Variable(typeof(ValueHolder<double>), "double");
            var doubleValueProperty = Property(doubleVariable, "Value");

            var block = Block(
                new[] { doubleVariable },
                Assign(doubleVariable, New(doubleVariable.Type)),
                Assign(doubleValueProperty, Convert(decimalValueProperty, doubleValueProperty.Type)),
                doubleVariable);

            var expr = Lambda<Func<ValueHolder<decimal?>, ValueHolder<double>>>(
                block,
                decimalParameter);

            var source = new ValueHolder<decimal?> { Value = 938378.637m };

            var adapt = expr.CompileFast();
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be simplified with dup #173
                OpCodes.Ldloc_0,
                OpCodes.Ldarg_0,
                OpCodes.Call, // ValueHolder<decimal?>.get_Value
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,
                OpCodes.Call, // decimal?.get_Value
                OpCodes.Call, // double Decimal.op_Explicit() 
                OpCodes.Call, // ValueHolder<double>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(source);
            Assert.AreEqual(938378.637d, result.Value);
        }

        [Test]
        public void DecimalToNullableDoubleCastsShouldWork()
        {
            var decimalParameter = Parameter(typeof(ValueHolder<decimal>), "decimalValue");
            var decimalValueProperty = Property(decimalParameter, "Value");

            var nullableDoubleVariable = Variable(typeof(ValueHolder<double?>), "nullableDouble");
            var doubleValueProperty = Property(nullableDoubleVariable, "Value");

            var block = Block(
                new[] { nullableDoubleVariable },
                Assign(nullableDoubleVariable, New(nullableDoubleVariable.Type)),
                Assign(doubleValueProperty, Convert(decimalValueProperty, doubleValueProperty.Type)),
                nullableDoubleVariable);

            var expr = Lambda<Func<ValueHolder<decimal>, ValueHolder<double?>>>(
                block,
                decimalParameter);

            var source = new ValueHolder<decimal> { Value = 5332.00m };

            var adapt = expr.CompileFast();
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be simplified with dup #173
                OpCodes.Ldloc_0,
                OpCodes.Ldarg_0,
                OpCodes.Call,    // ValueHolder<decimal>.get_Value
                OpCodes.Call,    // double Decimal.op_Explicit() 
                OpCodes.Newobj,  // new Nullable<double>()
                OpCodes.Call,    // ValueHolder<double?>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(source);
            Assert.AreEqual(5332d, result.Value);
        }

        private class ValueHolder<T>
        {
            public T Value { get; set; }
        }
    }
}