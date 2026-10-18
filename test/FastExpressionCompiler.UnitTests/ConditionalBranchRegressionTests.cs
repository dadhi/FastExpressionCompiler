using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ConditionalBranchRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Equality_branches_match_system_for_primitive_and_reference_inputs(t);
        Operand_side_effects_keep_order_and_short_circuit_on_exceptions(t);
        Exception_region_operands_keep_stack_safe_emission(t);
    }

    public enum Number : uint { Zero, High = uint.MaxValue }
    public void Equality_branches_match_system_for_primitive_and_reference_inputs(TestContext t)
    {
        Check(t, new[] { int.MinValue, -1, 0, 1, 17, int.MaxValue });
        Check(t, new[] { 0u, 1u, 17u, uint.MaxValue });
        Check(t, new[] { long.MinValue, -1L, 0L, 1L, 17L, long.MaxValue });
        Check(t, new[] { 0UL, 17UL, ulong.MaxValue });
        Check(t, new[] { float.NaN, float.NegativeInfinity, -0f, 0f, 17f, float.PositiveInfinity });
        Check(t, new[] { double.NaN, double.NegativeInfinity, -0d, 0d, 17d, double.PositiveInfinity });
        Check(t, new[] { true, false });
        Check(t, new[] { Number.Zero, Number.High });
        Check(t, new[] { (object)null, new object(), new object() });
        Check(t, new[] { (int?)null, 0, 17 });
        Check(t, new[] { 0m, 17m });
    }
    private static void Check<T>(TestContext t, T[] values)
    {
        var a = S.Parameter(typeof(T));
        var b = S.Parameter(typeof(T));
        foreach (var unequal in new[] { false, true })
        {
            var test = unequal ? S.NotEqual(a, b) : S.Equal(a, b);
            var tree = S.Lambda<Func<T, T, int>>(S.Condition(test, S.Constant(7), S.Constant(19)), a, b);
            var system = tree.Compile();
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var x in values) foreach (var y in values) t.AreEqual(system(x, y), fast(x, y));
        }
    }
    public static int Record(List<int> trace, int value, bool fail)
    {
        trace.Add(value);
        if (fail) throw new InvalidOperationException();
        return value;
    }
    public void Operand_side_effects_keep_order_and_short_circuit_on_exceptions(TestContext t)
    {
        var trace = S.Parameter(typeof(List<int>));
        var fail = S.Parameter(typeof(bool));
        var method = typeof(ConditionalBranchRegressionTests).GetMethod(nameof(Record));
        var tree = S.Lambda<Func<List<int>, bool, int>>(S.Condition(
            S.Equal(S.Call(method, trace, S.Constant(17), fail), S.Call(method, trace, S.Constant(19), S.Constant(false))),
            S.Constant(7), S.Constant(11)), trace, fail);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var log = new List<int>();
        t.AreEqual(11, fast(log, false)); t.AreEqual("17,19", string.Join(",", log));
        log.Clear(); t.Throws<InvalidOperationException>(() => fast(log, true));
        t.AreEqual("17", string.Join(",", log));
    }
    public void Exception_region_operands_keep_stack_safe_emission(TestContext t)
    {
        var p = S.Parameter(typeof(int));
        var right = S.TryFinally(S.Constant(17), S.Empty());
        var tree = S.Lambda<Func<int, int>>(S.Condition(S.Equal(p, right), S.Constant(7), S.Constant(19)), p);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(7, fast(17)); t.AreEqual(19, fast(18));
    }
}
