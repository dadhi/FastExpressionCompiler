#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;

namespace FastExpressionCompiler.UnitTests;

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
        var parameter = Expression.Parameter(typeof(bool));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        foreach (var value in new[] { false, true })
        {
            var constant = Expression.Lambda<Func<bool>>(Expression.Not(Expression.Constant(value), method));
            var fastConstant = constant.CompileFast(true, flags);
            if (!t.IsNotNull(fastConstant)) return;
            t.AreEqual(value, fastConstant());
            var expression = Expression.Lambda<Func<bool, bool>>(Expression.Not(parameter, method), parameter);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(value, fast(value));
        }
    }

    public void Conditional_preserves_custom_not_semantics(TestContext t)
    {
        var parameter = Expression.Parameter(typeof(bool));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        foreach (var value in new[] { false, true })
        foreach (var shape in new[] { 0, 1, 2 })
        {
            Expression operand = shape == 0 ? Expression.Constant(value)
                : shape == 1 ? parameter : Expression.Equal(parameter, Expression.Constant(true));
            var body = Expression.Condition(Expression.Not(operand, method), Expression.Constant(42), Expression.Constant(7));
            var expression = Expression.Lambda<Func<bool, int>>(body, parameter);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(expression.Compile()(value), fast(value));
            t.AreEqual(2, Calls);
        }
    }

    public void Ignored_result_preserves_custom_not_effects(TestContext t)
    {
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(Identity));
        var expression = Expression.Lambda<Func<int>>(Expression.Block(
            Expression.Not(Expression.Constant(true), method), Expression.Constant(7)));
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.DisableInterpreter })
        {
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(7, fast());
            t.AreEqual(1, Calls);
        }
    }

    public void Nullable_method_receives_null(TestContext t)
    {
        var parameter = Expression.Parameter(typeof(bool?));
        var method = typeof(MethodBasedNotRegressionTests).GetMethod(nameof(NullableIdentity));
        var expression = Expression.Lambda<Func<bool?, bool?>>(Expression.Not(parameter, method), parameter);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new bool?[] { null, false, true })
            t.AreEqual(expression.Compile()(value), fast(value));
    }

    public static int Calls;
    public static bool Identity(bool value) { ++Calls; return value; }
    public static bool? NullableIdentity(bool? value) { ++Calls; return value ?? true; }
}
#endif
