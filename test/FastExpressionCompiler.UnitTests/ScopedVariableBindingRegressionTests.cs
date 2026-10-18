using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ScopedVariableBindingRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Leaving_a_nested_binding_restores_the_outer_variable(t);
        Captured_shadowed_variables_keep_distinct_storage(t);
    }

    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default,
        CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };

    public void Leaving_a_nested_binding_restores_the_outer_variable(TestContext t)
    {
        var value = S.Variable(typeof(int), "value");
        var middle = S.Variable(typeof(int), "middle");
        var source = S.Lambda<Func<int>>(S.Block(new[] { value },
            S.Assign(value, S.Constant(1)),
            S.Block(new[] { middle },
                S.Assign(middle, S.Constant(0)),
                S.Block(new[] { value }, S.Assign(value, S.Constant(2))),
                value)));
        t.AreEqual(1, source.Compile()());
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(1, fast());
        }
    }

    public void Captured_shadowed_variables_keep_distinct_storage(TestContext t)
    {
        var value = S.Variable(typeof(int), "value");
        var middle = S.Variable(typeof(int), "middle");
        var outer = S.Variable(typeof(Func<int>), "outer");
        var inner = S.Variable(typeof(Func<int>), "inner");
        var source = S.Lambda<Func<Func<int>[]>>(S.Block(new[] { value, outer, inner },
            S.Assign(value, S.Constant(1)),
            S.Assign(outer, S.Lambda<Func<int>>(value)),
            S.Block(new[] { middle },
                S.Assign(middle, S.Constant(0)),
                S.Block(new[] { value },
                    S.Assign(value, S.Constant(2)),
                    S.Assign(inner, S.Lambda<Func<int>>(value))),
                S.Assign(value, S.Constant(3))),
            S.NewArrayInit(typeof(Func<int>), outer, inner)));
        var expected = source.Compile()();
        t.AreEqual(3, expected[0]());
        t.AreEqual(2, expected[1]());
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            var first = fast();
            var second = fast();
            t.AreEqual(3, first[0]());
            t.AreEqual(2, first[1]());
            t.AreEqual(3, second[0]());
            t.AreEqual(2, second[1]());
        }
    }
}
