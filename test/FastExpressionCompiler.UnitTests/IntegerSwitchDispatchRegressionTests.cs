using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IntegerSwitchDispatchRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Duplicate_switch_tests_choose_the_first_case(t);
        Wide_switch_values_do_not_alias_low_bits(t);
        Negative_and_unsigned_table_origins_match_system(t);
        Wide_switches_evaluate_the_operand_once(t);
    }

    public enum SignedKind : long { One = 1, Two = 2, Three = 3, Max = long.MaxValue }
    public enum UnsignedKind : ulong { One = 1, Two = 2, Three = 3, Max = ulong.MaxValue }
    public enum SmallKind { One = 1, Two = 2, Three = 3, Max = int.MaxValue }

    private static object AsValue(Type type, ulong value) => type.IsEnum
        ? Enum.ToObject(type, value)
        : type == typeof(long) ? (object)unchecked((long)value)
        : type == typeof(ulong) ? value
        : type == typeof(uint) ? (object)unchecked((uint)value)
        : unchecked((int)value);

    public void Duplicate_switch_tests_choose_the_first_case(TestContext t)
    {
        foreach (var type in new[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(SignedKind), typeof(UnsignedKind), typeof(SmallKind) })
        {
            var parameter = SysExpr.Parameter(type);
            var maximum = type == typeof(long) || type == typeof(SignedKind) ? (ulong)long.MaxValue
                : type == typeof(ulong) || type == typeof(UnsignedKind) ? ulong.MaxValue
                : type == typeof(uint) ? uint.MaxValue : (ulong)int.MaxValue;
            var expression = SysExpr.Lambda(SysExpr.Switch(parameter, SysExpr.Constant("default"),
                SysExpr.SwitchCase(SysExpr.Constant("first"), SysExpr.Constant(AsValue(type, 1), type)),
                SysExpr.SwitchCase(SysExpr.Constant("two"), SysExpr.Constant(AsValue(type, 2), type)),
                SysExpr.SwitchCase(SysExpr.Constant("duplicate"), SysExpr.Constant(AsValue(type, 1), type)),
                SysExpr.SwitchCase(SysExpr.Constant("max"), SysExpr.Constant(AsValue(type, maximum), type))), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = expression.Compile();
            t.AreEqual("first", fast.DynamicInvoke(AsValue(type, 1)));
            t.AreEqual("two", fast.DynamicInvoke(AsValue(type, 2)));
            t.AreEqual("max", fast.DynamicInvoke(AsValue(type, maximum)));
            foreach (var value in new[] { 0UL, 1UL, 2UL, 3UL, maximum })
                t.AreEqual(expected.DynamicInvoke(AsValue(type, value)), fast.DynamicInvoke(AsValue(type, value)));
        }
    }

    public void Wide_switch_values_do_not_alias_low_bits(TestContext t)
    {
        foreach (var type in new[] { typeof(long), typeof(ulong), typeof(SignedKind), typeof(UnsignedKind) })
        {
            var parameter = SysExpr.Parameter(type);
            var expression = SysExpr.Lambda(SysExpr.Switch(parameter, SysExpr.Constant(-1),
                SysExpr.SwitchCase(SysExpr.Constant(10), SysExpr.Constant(AsValue(type, 1), type)),
                SysExpr.SwitchCase(SysExpr.Constant(20), SysExpr.Constant(AsValue(type, 2), type)),
                SysExpr.SwitchCase(SysExpr.Constant(30), SysExpr.Constant(AsValue(type, 3), type))), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = expression.Compile();
            foreach (var value in new[] { 0UL, 1UL, 2UL, 3UL, 4UL, (1UL << 32) + 1, (1UL << 32) + 2, ulong.MaxValue, (ulong)long.MaxValue })
                t.AreEqual(expected.DynamicInvoke(AsValue(type, value)), fast.DynamicInvoke(AsValue(type, value)));
        }
    }

    public void Negative_and_unsigned_table_origins_match_system(TestContext t)
    {
        foreach (var type in new[] { typeof(int), typeof(uint) })
        foreach (var origin in type == typeof(int) ? new[] { int.MinValue, -3, -2, 0, 1, int.MaxValue - 3 } : new[] { 0, 1, int.MinValue, -4 })
        {
            var parameter = SysExpr.Parameter(type);
            object Value(int offset) => type == typeof(int) ? (object)unchecked(origin + offset) : unchecked((uint)(origin + offset));
            var expression = SysExpr.Lambda(SysExpr.Switch(parameter, SysExpr.Constant(-1),
                SysExpr.SwitchCase(SysExpr.Constant(10), SysExpr.Constant(Value(0), type)),
                SysExpr.SwitchCase(SysExpr.Constant(20), SysExpr.Constant(Value(1), type)),
                SysExpr.SwitchCase(SysExpr.Constant(30), SysExpr.Constant(Value(2), type))), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = expression.Compile();
            foreach (var offset in new[] { -1, 0, 1, 2, 3, 5 })
                t.AreEqual(expected.DynamicInvoke(Value(offset)), fast.DynamicInvoke(Value(offset)));
        }
    }

    public void Wide_switches_evaluate_the_operand_once(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Func<long>));
        var expression = SysExpr.Lambda<Func<Func<long>, int>>(SysExpr.Switch(SysExpr.Invoke(parameter), SysExpr.Constant(-1),
            SysExpr.SwitchCase(SysExpr.Constant(10), SysExpr.Constant(1L)),
            SysExpr.SwitchCase(SysExpr.Constant(20), SysExpr.Constant(2L)),
            SysExpr.SwitchCase(SysExpr.Constant(30), SysExpr.Constant(3L))), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        t.AreEqual(20, fast(() => { ++calls; return 2; }));
        t.AreEqual(1, calls);
    }
}
