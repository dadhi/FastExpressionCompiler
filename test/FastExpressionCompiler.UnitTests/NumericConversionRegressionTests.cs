using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NumericConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Numeric_conversion_boundaries(t);
        Nullable_numeric_conversion_boundaries(t);
        Nullable_source_conversion_boundaries(t);
    }

    public void Numeric_conversion_boundaries(TestContext t) => CheckConversions(t, false, false);
    public void Nullable_numeric_conversion_boundaries(TestContext t) => CheckConversions(t, true, true);

    public void Nullable_source_conversion_boundaries(TestContext t) => CheckConversions(t, true, false);

    private static void CheckConversions(TestContext t, bool nullable, bool nullableTarget)
    {
        Type[] types = { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char),
            typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double),
            typeof(UnsignedEnum), typeof(SignedEnum) };
        object[][] values = {
            new object[] { sbyte.MinValue, (sbyte)-1, (sbyte)0, sbyte.MaxValue },
            new object[] { (byte)0, (byte)1, byte.MaxValue },
            new object[] { short.MinValue, (short)-1, (short)0, short.MaxValue },
            new object[] { (ushort)0, (ushort)1, ushort.MaxValue },
            new object[] { char.MinValue, (char)1, char.MaxValue },
            new object[] { int.MinValue, -1, 0, int.MaxValue },
            new object[] { 0u, 1u, (uint)int.MaxValue + 1, uint.MaxValue },
            new object[] { long.MinValue, -1L, 0L, long.MaxValue },
            new object[] { 0UL, 1UL, (ulong)long.MaxValue + 1, ulong.MaxValue },
            new object[] { float.NegativeInfinity, -1f, 0f, 1f, float.MaxValue, float.PositiveInfinity, float.NaN },
            new object[] { double.NegativeInfinity, -1d, 0d, 1d, double.MaxValue, double.PositiveInfinity, double.NaN },
            new object[] { (UnsignedEnum)0, (UnsignedEnum)uint.MaxValue },
            new object[] { (SignedEnum)long.MinValue, (SignedEnum)(-1), (SignedEnum)long.MaxValue }
        };
        for (var sourceIndex = 0; sourceIndex < types.Length; ++sourceIndex)
        foreach (var target in types)
        for (var isChecked = 0; isChecked != 2; ++isChecked)
        for (var sourceNullable = 0; sourceNullable <= (nullable ? 1 : 0); ++sourceNullable)
        {
            var source = sourceNullable == 0 ? types[sourceIndex] : typeof(Nullable<>).MakeGenericType(types[sourceIndex]);
            var targetType = nullableTarget ? typeof(Nullable<>).MakeGenericType(target) : target;
            var parameter = SysExpr.Parameter(source);
            var body = isChecked == 0 ? SysExpr.Convert(parameter, targetType) : SysExpr.ConvertChecked(parameter, targetType);
            var expression = SysExpr.Lambda(body, parameter);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in values[sourceIndex])
            {
                var expected = Invoke(system, value);
                var actual = Invoke(fast, value);
                if (!t.AreEqual(expected, actual)) return;
            }
            if (sourceNullable != 0 && !t.AreEqual(Invoke(system, null), Invoke(fast, null))) return;
        }
    }

    // Compare both the value and the exception contract at conversion boundaries.
    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { value }); }
        catch (TargetInvocationException error) { return error.InnerException.GetType(); }
    }

    public enum UnsignedEnum : uint { Zero }
    public enum SignedEnum : long { Zero }
}
