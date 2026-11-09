using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeSwitchCaseRegressionTests : ITestX
{
    public void Run(TestRun t) { Numeric_runtime_case_values_are_supported(t); Constant_input_does_not_skip_runtime_tests(t); }
    public void Numeric_runtime_case_values_are_supported(TestContext t)
    {
        var value = S.Parameter(typeof(int)); var test = S.Parameter(typeof(int));
        var tree = S.Lambda<Func<int, int, int>>(S.Switch(value, S.Constant(-1), S.SwitchCase(S.Constant(7), S.Add(test, S.Constant(1)))), value, test);
        var system = tree.Compile(); var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var x in new[] { int.MinValue, -1, 0, 42, int.MaxValue })
            foreach (var y in new[] { -1, 0, 41, int.MaxValue }) t.AreEqual(system(x, y), fast(x, y));
    }
    public static int Calls;
    public static int Test() { ++Calls; return 42; }
    public void Constant_input_does_not_skip_runtime_tests(TestContext t)
    {
        var tree = S.Lambda<Func<int>>(S.Switch(S.Constant(42), S.Constant(-1),
            S.SwitchCase(S.Constant(7), S.Call(typeof(RuntimeSwitchCaseRegressionTests).GetMethod(nameof(Test)))),
            S.SwitchCase(S.Constant(9), S.Constant(42))));
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Calls = 0; t.AreEqual(7, fast()); t.AreEqual(1, Calls);
    }
}
