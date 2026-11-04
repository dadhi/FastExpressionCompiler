using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CompilerLayoutBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Constant_index_assignment_spill_preserves_behavior(t);
        Default_index_assignment_spill_preserves_behavior(t);
        Literal_field_spill_preserves_behavior(t);
        Static_readonly_field_spill_preserves_behavior(t);
        Arithmetic_operands_after_spill_preserve_behavior(t);
    }

    public void Constant_index_assignment_spill_preserves_behavior(TestContext t) => CheckArray(t, SysExpr.Constant(0));
    public void Default_index_assignment_spill_preserves_behavior(TestContext t) => CheckArray(t, SysExpr.Default(typeof(int)));

    private static void CheckArray(TestContext t, SysExpr index)
    {
        var xs = SysExpr.Parameter(typeof(int[]));
        foreach (var effects in new[] { false, true })
        {
            var calls = 0;
            var body = SysExpr.Assign(SysExpr.ArrayAccess(xs, index), Spill(SysExpr.Constant(1), effects ? (Action)(() => ++calls) : null));
            var expression = SysExpr.Lambda<Action<int[]>>(body, xs);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var array = new[] { 42 };
            fast(array);
            t.AreEqual(1, array[0]);
            t.AreEqual(effects ? 1 : 0, calls);
            foreach (var value in new int[][] { null, Array.Empty<int>() })
            {
                calls = 0;
                var expected = Invoke(expression.Compile(), new object[] { value });
                var expectedCalls = calls;
                calls = 0;
                var actual = Invoke(fast, new object[] { value });
                t.AreEqual(expected, actual);
                t.AreEqual(expectedCalls, calls);
            }
        }
    }

    public void Literal_field_spill_preserves_behavior(TestContext t)
    {
        foreach (var effects in new[] { false, true })
        {
            var calls = 0;
            var expression = SysExpr.Lambda<Func<double>>(SysExpr.Add(
                SysExpr.Field(null, typeof(Math).GetField(nameof(Math.PI))),
                Spill(SysExpr.Constant(0.0), effects ? (Action)(() => ++calls) : null)));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(Math.PI, fast());
            t.AreEqual(effects ? 1 : 0, calls);
        }
    }

    public void Static_readonly_field_spill_preserves_behavior(TestContext t)
    {
        foreach (var effects in new[] { false, true })
        {
            var calls = 0;
            var expression = SysExpr.Lambda<Func<string>>(SysExpr.Call(
                typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) }),
                SysExpr.Field(null, typeof(bool).GetField(nameof(bool.TrueString))),
                Spill(SysExpr.Constant("!"), effects ? (Action)(() => ++calls) : null)));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual("True!", fast());
            t.AreEqual(effects ? 1 : 0, calls);
        }
    }

    public void Arithmetic_operands_after_spill_preserve_behavior(TestContext t)
    {
        var x1 = SysExpr.Parameter(typeof(int));
        var x2 = SysExpr.Parameter(typeof(int));
        var x3 = SysExpr.Parameter(typeof(int));
        var x4 = SysExpr.Parameter(typeof(int));
        foreach (var effects in new[] { false, true })
        {
            var calls = 0;
            var expression = SysExpr.Lambda<Func<int, int, int, int, int>>(SysExpr.Add(SysExpr.Add(SysExpr.Add(
                x1, Spill(x2, effects ? (Action)(() => ++calls) : null)), x3), x4), x1, x2, x3, x4);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(10, fast(1, 2, 3, 4));
            t.AreEqual(effects ? 1 : 0, calls);
        }
    }

    private static SysExpr Spill(SysExpr value, Action finallyAction) =>
        SysExpr.TryFinally(value, finallyAction == null ? SysExpr.Empty() : (SysExpr)SysExpr.Invoke(SysExpr.Constant(finallyAction)));

    private static object Invoke(Delegate function, object[] arguments)
    {
        try { return function.DynamicInvoke(arguments); }
        catch (System.Reflection.TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
