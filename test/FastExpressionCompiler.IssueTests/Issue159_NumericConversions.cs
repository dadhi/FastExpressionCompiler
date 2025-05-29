﻿using System;
using System.Reflection;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue159_NumericConversions : ITest
    {
        public int Run()
        {
            FloatToDecimalNullableShouldWork();
            UnsignedLongComparisonsWithConversionsShouldWork();
            IntToNullableUlong();
            UnsignedNullableLongComparison();
            UnsignedNullableLongComparisonsWithConversionsShouldWork();
            FloatComparisonsWithConversionsShouldWork();
            FloatComparisonsWithConversionsShouldWork2();
            ComparisonsWithConversionsShouldWork3();
            ComparisonsWithConversionsShouldWork4();
            FloatComparisonsWithConversionsShouldWork3();
            FloatComparisonsWithConversionsShouldWork4();
            ConvertNullableFloatToDecimal();
            NullableFloatComparisonsWithConversionsShouldWork();
            DoubleComparisonsWithConversionsShouldWork();
            DoubleComparisonsWithConversionsShouldWork2();
            NullableIntToDoubleCastsShouldWork();
            NullableIntToDoubleCastsShouldWork_with_MemberInit();
            NullableDecimalToDoubleCastsShouldWork();
            DecimalToNullableDoubleCastsShouldWork();
            return 19;
        }


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

            var fs = ulongValueOrDefaultLambda.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual(default(int), fs(new ValueHolder<ulong> { Value = ulong.MaxValue }).Value);

            var fx = ulongValueOrDefaultLambda.CompileFast(true);
            fx.PrintIL();
            Asserts.AreEqual(default(int), fx(new ValueHolder<ulong> { Value = ulong.MaxValue }).Value);
        }


        public void IntToNullableUlong()
        {
            var outputHolderExpr = Variable(typeof(ValueHolder<ulong?>), "nullableUlongValue");
            var outputValPropExpr = Property(outputHolderExpr, "Value");

            var block = Block(
                new[] { outputHolderExpr },
                Assign(outputHolderExpr, New(outputHolderExpr.Type)),
                Assign(outputValPropExpr, Convert(Constant(int.MaxValue), outputValPropExpr.Type)),
                outputHolderExpr);

            var ulongValHolderExpr = Lambda<Func<ValueHolder<ulong?>>>(block);

            var expected = ulongValHolderExpr.CompileSys();
            Asserts.AreEqual((ulong)int.MaxValue, expected().Value);

            var actual = ulongValHolderExpr.CompileFast(true);
            Asserts.AreEqual((ulong)int.MaxValue, actual().Value);
        }


        public void UnsignedNullableLongComparison()
        {
            var ulongParameter = Parameter(typeof(ValueHolder<ulong?>), "nullableUlongValue");
            var ulongValueProperty = Property(ulongParameter, "Value");

            var lambdaExpr = Lambda<Func<ValueHolder<ulong?>, bool>>(
                LessThanOrEqual(ulongValueProperty, Convert(Constant(int.MaxValue), ulongValueProperty.Type)),
                ulongParameter);

            var expected = lambdaExpr.CompileSys();
            var expectedResult = expected(new ValueHolder<ulong?> { Value = ulong.MaxValue });
            Asserts.AreEqual(false, expectedResult);

            var actual = lambdaExpr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);

            actual.Method.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Call,   // get_Value getter
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,  // GetValueOrDefault
                OpCodes.Ldc_I4, // load `int.MaxValue`
                OpCodes.Conv_U8,// convert it to `ulong?`  
                OpCodes.Newobj,
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,
                OpCodes.Ldfld,  // Nullable value getter
                OpCodes.Cgt_Un,
                OpCodes.Ldc_I4_0,
                OpCodes.Ceq,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.Ldloca_S,
                OpCodes.Call,
                OpCodes.And,
                OpCodes.And,
                OpCodes.Ret);

            var actualResult = actual(new ValueHolder<ulong?> { Value = ulong.MaxValue });
            Asserts.AreEqual(false, actualResult);
        }


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
            Asserts.AreEqual(default(int?), expectedResult.Value);

            var actual = ulongValueOrDefaultLambda.CompileFast(true);
            var actualResult = actual(new ValueHolder<ulong?> { Value = ulong.MaxValue });
            Asserts.AreEqual(default(int?), actualResult.Value);
        }


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

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast(true);
            var result = floatValueOrDefaultFunc.Invoke(source);

            Asserts.AreEqual((short)532, result.Value);
        }


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

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast(true);
            var result = floatValueOrDefaultFunc.Invoke(source);

            Asserts.AreEqual((short)532, result.Value);
        }


        public void FloatToDecimalNullableShouldWork()
        {
            var floatParamExpr = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValuePropExpr = Property(floatParamExpr, "Value");

            var nullableDecimalVarExpr = Variable(typeof(ValueHolder<decimal?>), "nullableDecimal");
            var nullableDecimalValuePropExpr = Property(nullableDecimalVarExpr, "Value");

            var block = Block(
                new[] { nullableDecimalVarExpr },
                Assign(nullableDecimalVarExpr, New(nullableDecimalVarExpr.Type)),
                Assign(nullableDecimalValuePropExpr, Convert(floatValuePropExpr, nullableDecimalValuePropExpr.Type)),
                nullableDecimalVarExpr);

            var expr = Lambda<Func<ValueHolder<float>, ValueHolder<decimal?>>>(
                block,
                floatParamExpr);
            expr.PrintCSharp();

            var compiled = expr.CompileSys();
            compiled.PrintIL();

            var source = new ValueHolder<float> { Value = 3.14f };
            Asserts.AreEqual(3.14m, compiled(source).Value);

            var fastCompiled = expr.CompileFast(true);
            fastCompiled.PrintIL();
            var result = fastCompiled(source);
            Asserts.AreEqual(3.14m, result.Value);
        }


        public void ComparisonsWithConversionsShouldWork3()
        {
            var floatParamExpr = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValuePropExpr = Property(floatParamExpr, "Value");

            var condition = GreaterThanOrEqual(
                floatValuePropExpr,
                Convert(Constant(decimal.MinValue), floatValuePropExpr.Type));

            var expr = Lambda<Func<ValueHolder<float>, bool>>(condition, floatParamExpr);
            var source = new ValueHolder<float> { Value = float.MaxValue };

            var compiledFast = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);

            compiledFast.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Call,
                OpCodes.Ldsfld,
                OpCodes.Call,
                OpCodes.Clt_Un,
                OpCodes.Ldc_I4_0,
                OpCodes.Ceq,
                OpCodes.Ret);

            Asserts.AreEqual(true, compiledFast(source));
        }


        public void ComparisonsWithConversionsShouldWork4()
        {
            var floatParamExpr = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValuePropExpr = Property(floatParamExpr, "Value");

            var condition = AndAlso(
                    GreaterThanOrEqual(floatValuePropExpr, Convert(Constant(decimal.MinValue), floatValuePropExpr.Type)),
                    LessThanOrEqual(floatValuePropExpr, Convert(Constant(decimal.MaxValue), floatValuePropExpr.Type)));

            var expr = Lambda<Func<ValueHolder<float>, bool>>(condition, floatParamExpr);
            var source = new ValueHolder<float> { Value = float.MaxValue };

            var compiled = expr.CompileSys();
            compiled.PrintIL();

            var compiledFast = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            compiledFast.PrintIL();

            compiledFast.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Call,
                OpCodes.Ldsfld,
                OpCodes.Call,
                OpCodes.Clt_Un,
                OpCodes.Ldc_I4_0,
                OpCodes.Ceq,
                OpCodes.Brfalse,
                OpCodes.Ldarg_1,
                OpCodes.Call,
                OpCodes.Ldsfld,
                OpCodes.Call,
                OpCodes.Cgt_Un,
                OpCodes.Ldc_I4_0,
                OpCodes.Ceq,
                OpCodes.Br,
                OpCodes.Ldc_I4_0,
                OpCodes.Ret);

            Asserts.AreEqual(false, compiledFast(source));
        }


        public void FloatComparisonsWithConversionsShouldWork3()
        {
            var floatParamExpr = Parameter(typeof(ValueHolder<float>), "floatValue");
            var floatValuePropExpr = Property(floatParamExpr, "Value");

            var nullableDecimalVarExpr = Variable(typeof(ValueHolder<decimal?>), "nullableDecimal");
            var nullableDecimalValuePropExpr = Property(nullableDecimalVarExpr, "Value");

            var floatValueOrDefault = Condition(
                AndAlso(
                    GreaterThanOrEqual(floatValuePropExpr, Convert(Constant(decimal.MinValue), floatValuePropExpr.Type)),
                    LessThanOrEqual(floatValuePropExpr, Convert(Constant(decimal.MaxValue), floatValuePropExpr.Type))),
                Convert(floatValuePropExpr, nullableDecimalValuePropExpr.Type),
                Default(nullableDecimalValuePropExpr.Type));

            var block = Block(
                new[] { nullableDecimalVarExpr },
                Assign(nullableDecimalVarExpr, New(nullableDecimalVarExpr.Type)),
                Assign(nullableDecimalValuePropExpr, floatValueOrDefault),
                nullableDecimalVarExpr);

            var expr = Lambda<Func<ValueHolder<float>, ValueHolder<decimal?>>>(
                block,
                floatParamExpr);

            var source = new ValueHolder<float> { Value = float.MaxValue };

            var func = expr.CompileFast(true);
            var result = func(source);

            Asserts.IsNull(result.Value);
        }


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

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast(true);
            var result = floatValueOrDefaultFunc.Invoke(source);

            Asserts.AreEqual(8532.00m, result.Value);
        }


        public void ConvertNullableFloatToDecimal()
        {
            var p = Parameter(typeof(float?), "f");
            var e = Lambda<Func<float?, decimal?>>(Convert(p, typeof(decimal?)), p);
            e.PrintCSharp();
            var @cs = (Func<float?, Decimal?>)((float? f) =>
                (Decimal?)f);
            Asserts.AreEqual(42, cs(42));

            var fs = e.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual(42, fs(42));

            var ff = e.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual(42, ff(42));
        }


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

            var floatValueOrDefaultFunc = floatValueOrDefaultLambda.CompileFast(true);
            Asserts.IsNotNull(floatValueOrDefaultFunc);

            var result = floatValueOrDefaultFunc.Invoke(source);
            Asserts.AreEqual(73.62m, result.Value);
        }


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

            var doubleValueOrDefaultFunc = doubleValueOrDefaultLambda.CompileFast(true);
            var result = doubleValueOrDefaultFunc.Invoke(source);

            Asserts.IsNull(result.Value);
        }


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

            var doubleValueOrDefaultFunc = doubleValueOrDefaultLambda.CompileFast(true);
            var result = doubleValueOrDefaultFunc.Invoke(source);

            Asserts.AreEqual(default(decimal), result.Value);
        }

        // Expression used for the tests below
        //private ValueHolder<double> Adapt(ValueHolder<int?> nullableIntHolder)
        //{
        //    var vd = new ValueHolder<double>();
        //    vd.Value = (double)nullableIntHolder.Value;
        //    return vd;
        //}


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

            var adapt = adaptExpr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be replaced with dup #
                OpCodes.Ldloc_0, // 
                OpCodes.Ldarg_1,
                OpCodes.Call, // ValueHolder<int?>.get_Value
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,
                OpCodes.Call, // int?.get_Value
                OpCodes.Conv_R8,
                OpCodes.Call, // ValueHolder<double>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(new ValueHolder<int?> { Value = 321 });
            Asserts.AreEqual(321d, result.Value);
        }


        public void NullableIntToDoubleCastsShouldWork_with_MemberInit()
        {
            var nullableIntHolderParam = Parameter(typeof(ValueHolder<int?>), "nullableIntHolder");

            var memberInit = MemberInit(New(typeof(ValueHolder<double>)),
                Bind(typeof(ValueHolder<double>).GetTypeInfo().GetDeclaredProperty(nameof(ValueHolder<double>.Value)),
                    Convert(Property(nullableIntHolderParam, "Value"), typeof(double))));

            var adaptExpr = Lambda<Func<ValueHolder<int?>, ValueHolder<double>>>(
                memberInit, nullableIntHolderParam);

            var adapt = adaptExpr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);

            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Dup,
                OpCodes.Ldarg_1,
                OpCodes.Call, // ValueHolder<int?>.get_Value
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Call, // int?.get_Value
                OpCodes.Conv_R8,
                OpCodes.Call, // ValueHolder<double>.set_Value 
                OpCodes.Ret);

            var result = adapt(new ValueHolder<int?> { Value = 321 });
            Asserts.AreEqual(321d, result.Value);
        }


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

            var adapt = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be simplified with dup #173
                OpCodes.Ldloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Call, // ValueHolder<decimal?>.get_Value
                OpCodes.Stloc_1,
                OpCodes.Ldloca_S,
                OpCodes.Call, // decimal?.get_Value
                OpCodes.Call, // double Decimal.op_Explicit() 
                OpCodes.Call, // ValueHolder<double>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(source);
            Asserts.AreEqual(938378.637d, result.Value);
        }


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

            var adapt = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            adapt.Method.AssertOpCodes(
                OpCodes.Newobj,
                OpCodes.Stloc_0, // todo: can be simplified with dup #173
                OpCodes.Ldloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Call,    // ValueHolder<decimal>.get_Value
                OpCodes.Call,    // double Decimal.op_Explicit() 
                OpCodes.Newobj,  // new Nullable<double>()
                OpCodes.Call,    // ValueHolder<double?>.set_Value
                OpCodes.Ldloc_0,
                OpCodes.Ret);

            var result = adapt(source);
            Asserts.AreEqual(5332d, result.Value);
        }

        private class ValueHolder<T>
        {
            public T Value { get; set; }
        }
    }
}