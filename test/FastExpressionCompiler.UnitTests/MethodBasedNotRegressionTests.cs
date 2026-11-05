using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

[RunTestX]
public struct MethodBasedNotRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Explicit_not_method_controls_result(t);
        Conditional_preserves_custom_not_semantics(t);
        Ignored_result_preserves_custom_not_effects(t);
        Nullable_method_receives_null(t);
    }

    public void Explicit_not_method_controls_result(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(bool));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        foreach (var value in new[] { false, true })
        {
            var constant = SysExpr.Lambda<Func<bool>>(SysExpr.Not(SysExpr.Constant(value), method));
            var fastConstant = constant.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fastConstant)) return;
            t.AreEqual(value, fastConstant());
            var expression = SysExpr.Lambda<Func<bool, bool>>(SysExpr.Not(parameter, method), parameter);
            var fast = expression.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(value, fast(value));
        }
    }

    public void Conditional_preserves_custom_not_semantics(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(bool));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        foreach (var value in new[] { false, true })
        foreach (var shape in new[] { 0, 1, 2 })
        {
            System.Linq.Expressions.Expression operand = shape == 0 ? SysExpr.Constant(value)
                : shape == 1 ? parameter : SysExpr.Equal(parameter, SysExpr.Constant(true));
            var body = SysExpr.Condition(SysExpr.Not(operand, method), SysExpr.Constant(42), SysExpr.Constant(7));
            var expression = SysExpr.Lambda<Func<bool, int>>(body, parameter);
            var fast = expression.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(expression.Compile()(value), fast(value));
            t.AreEqual(2, Calls);
        }
    }

    public void Ignored_result_preserves_custom_not_effects(TestContext t)
    {
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.Block(
            SysExpr.Not(SysExpr.Constant(true), method), SysExpr.Constant(7)));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        {
            var fast = expression.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(7, fast());
            t.AreEqual(1, Calls);
        }
    }

    public void Nullable_method_receives_null(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(bool?));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(NullableIdentity));
        var expression = SysExpr.Lambda<Func<bool?, bool?>>(SysExpr.Not(parameter, method), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new bool?[] { null, false, true })
            t.AreEqual(expression.Compile()(value), fast(value));
    }

    public static int Calls;
    public static bool Identity(bool value) { ++Calls; return value; }
    public static bool? NullableIdentity(bool? value) { ++Calls; return value ?? true; }
}
