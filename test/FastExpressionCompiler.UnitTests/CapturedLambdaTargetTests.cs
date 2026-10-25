using System;
using SysExpr = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedLambdaTargetTests : ITestX
{
    public void Run(TestRun t)
    {
        All_supported_arities_preserve_state_and_isolation(t);
        Custom_delegates_preserve_state(t);
    }

    public static int Observed;
    public static void Observe(int value) => Observed = value;

    public void All_supported_arities_preserve_state_and_isolation(TestContext t)
    {
        for (var arity = 0; arity <= 10; ++arity)
        foreach (var action in new[] { false, true })
        {
            var value = SysExpr.Parameter(typeof(int), "value");
            var parameters = new System.Linq.Expressions.ParameterExpression[arity];
            var arguments = new object[arity];
            System.Linq.Expressions.Expression sum = SysExpr.Constant(1);
            for (var i = 0; i < arity; ++i)
            {
                parameters[i] = SysExpr.Parameter(typeof(int));
                arguments[i] = i + 1;
                sum = SysExpr.Add(sum, parameters[i]);
            }
            var update = SysExpr.AddAssign(value, sum);
            var child = SysExpr.Lambda(action ? SysExpr.Block(update, SysExpr.Call(typeof(CapturedLambdaTargetTests).GetMethod(nameof(Observe)), value)) : update, parameters);
            var factory = SysExpr.Lambda(child, value);
            var system = factory.Compile();
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
            {
                var compiled = factory.FromSysExpression().CompileFast(true, flags);
                if (!t.IsNotNull(compiled)) return;
                var actual = (Delegate)compiled.DynamicInvoke(3);
                var expected = (Delegate)system.DynamicInvoke(3);
                var independent = (Delegate)compiled.DynamicInvoke(100);
                for (var i = 0; i < 3; ++i)
                {
                    var expectedResult = expected.DynamicInvoke(arguments);
                    var expectedObserved = Observed;
                    t.AreEqual(expectedResult, actual.DynamicInvoke(arguments));
                    if (action) t.AreEqual(expectedObserved, Observed);
                }
                t.IsTrue(actual.Target is ExpressionCompiler.ArrayClosureWithNonPassedParams);
                t.IsTrue(!ReferenceEquals(actual.Target, independent.Target));
                var expectedIndependent = (Delegate)system.DynamicInvoke(100);
                var expectedIndependentResult = expectedIndependent.DynamicInvoke(arguments);
                var expectedIndependentObserved = Observed;
                t.AreEqual(expectedIndependentResult, independent.DynamicInvoke(arguments));
                if (action) t.AreEqual(expectedIndependentObserved, Observed);
            }
        }
    }

    public delegate int Custom(int amount);
    public void Custom_delegates_preserve_state(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(int));
        var amount = SysExpr.Parameter(typeof(int));
        var tree = SysExpr.Lambda<Func<int, Custom>>(SysExpr.Lambda<Custom>(SysExpr.AddAssign(value, amount), amount), value);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.EnableDelegateDebugInfo })
        {
            var expected = tree.Compile()(3);
            var actual = tree.FromSysExpression().CompileFast(true, flags)(3);
            t.AreEqual(expected(4), actual(4));
            t.AreEqual(expected(-2), actual(-2));
        }
    }
}
