using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

[RunTest]
public struct BlockVariableBindingRegressionTests : ITestX
{
    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };

    public void Run(TestRun t)
    {
        Leaving_nested_binding_restores_outer_value(t);
        Sibling_redeclarations_restore_outer_value(t);
        Direct_nested_and_distinct_bindings_keep_their_values(t);
    }

    public void Leaving_nested_binding_restores_outer_value(TestContext t)
    {
        var value = S.Variable(typeof(int), "value");
        var middle = S.Variable(typeof(int), "middle");
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value },
            S.Assign(value, S.Constant(1)),
            S.Block(new[] { middle }, S.Assign(middle, S.Constant(0)),
                S.Block(new[] { value }, S.Assign(value, S.Constant(2))), value)));
        var expected = tree.Compile()();
        t.AreEqual(1, expected);
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
        }
    }

    public void Sibling_redeclarations_restore_outer_value(TestContext t)
    {
        var value = S.Variable(typeof(int), "value");
        var sum = S.Variable(typeof(int), "sum");
        var statements = new List<System.Linq.Expressions.Expression>
        {
            S.Assign(value, S.Constant(1)), S.Assign(sum, S.Constant(0))
        };
        for (var i = 0; i < 3; ++i)
        {
            var middle = S.Variable(typeof(int), "middle");
            statements.Add(S.Block(new[] { middle }, S.Assign(middle, S.Constant(0)),
                S.Block(new[] { value }, S.Assign(value, S.Constant(i + 10))), S.AddAssign(sum, value)));
        }
        statements.Add(sum);
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value, sum }, statements));
        var expected = tree.Compile()();
        t.AreEqual(3, expected);
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
        }
    }

    public void Direct_nested_and_distinct_bindings_keep_their_values(TestContext t)
    {
        var value = S.Variable(typeof(int), "value");
        var other = S.Variable(typeof(int), "other");
        var tree = S.Lambda<Func<int>>(S.Block(new[] { value }, S.Assign(value, S.Constant(1)),
            S.Block(new[] { value }, S.Assign(value, S.Constant(2))),
            S.Block(new[] { other }, S.Assign(other, S.Constant(3))), value));
        var expected = tree.Compile()();
        t.AreEqual(1, expected);
        foreach (var flags in Flags)
        {
            var fast = tree.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
        }
    }
}
