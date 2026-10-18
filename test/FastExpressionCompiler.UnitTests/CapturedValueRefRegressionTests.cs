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

        Nullable_and_reference_captures_expose_writable_cells(t);

    }
    public static int Add(ref int value, int increment) => value += increment;
    public static int? AddNullable(ref int? value, int increment) => value = value + increment;
    public static string Replace(ref string value) => value = "changed";

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

}
