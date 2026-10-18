using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedValueRefRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Curried_ref_calls_preserve_reads_and_mutations(t);
        Repeated_invocations_observe_mutated_capture(t);
        Separate_closures_keep_ref_mutations_isolated(t);
        Nullable_and_reference_captures_expose_writable_cells(t);
        Hoisted_local_ref_calls_update_the_shared_box(t);
    }
    public static int Add(ref int value, int increment) => value += increment;
    public static int? AddNullable(ref int? value, int increment) => value = value + increment;
    public static string Replace(ref string value) => value = "changed";

    public void Curried_ref_calls_preserve_reads_and_mutations(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>>[] expressions = {
            x => () => x * Add(ref x, 1),
            x => () => x * Add(ref x, 1) * Add(ref x, 2),
            x => () => x * Add(ref x, 1) * Add(ref x, 2) * x
        };
        var expectedValues = new[] { 12, 72, 432 };
        for (var i = 0; i < expressions.Length; ++i)
        {
            var fast = expressions[i].FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(expectedValues[i], fast(3)());
            t.AreEqual(expressions[i].Compile()(3)(), fast(3)());
        }
    }

    public void Repeated_invocations_observe_mutated_capture(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>> expression = x => () => Add(ref x, 1);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var actual = fast(3);
        var expected = expression.Compile()(3);
        for (var i = 4; i < 8; ++i)
        {
            t.AreEqual(i, actual());
            t.AreEqual(i, expected());
        }
    }

    public void Separate_closures_keep_ref_mutations_isolated(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>> expression = x => () => Add(ref x, 1);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var first = fast(3);
        var second = fast(30);
        t.AreEqual(4, first());
        t.AreEqual(31, second());
        t.AreEqual(5, first());
    }

    public void Nullable_and_reference_captures_expose_writable_cells(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int?, Func<int?>>> nullable = x => () => AddNullable(ref x, 1);
        System.Linq.Expressions.Expression<Func<string, Func<string>>> reference = x => () => Replace(ref x);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var numbers = nullable.FromSysExpression().CompileFast(true, flags);
            var strings = reference.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(numbers) || !t.IsNotNull(strings)) return;
            var expected = nullable.Compile()(3);
            var first = numbers(3);
            var second = numbers(30);
            t.AreEqual(expected(), first()); t.AreEqual(expected(), first());
            t.AreEqual((int?)31, second()); t.AreEqual((int?)6, first());
            t.AreEqual(null, (object)numbers(null)());
            var text = strings("original");
            t.AreEqual(reference.Compile()("original")(), text());
            t.AreEqual("changed", text());
        }
    }

    public void Hoisted_local_ref_calls_update_the_shared_box(TestContext t)
    {
        var value = SysExpr.Variable(typeof(int));
        var getter = SysExpr.Variable(typeof(Func<int>));
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.Block(new[] { value, getter },
            SysExpr.Assign(value, SysExpr.Constant(3)),
            SysExpr.Assign(getter, SysExpr.Lambda<Func<int>>(value)),
            SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(Add)), value, SysExpr.Constant(1)),
            SysExpr.Invoke(getter)));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(4, fast());
        t.AreEqual(expression.Compile()(), fast());
    }
}
