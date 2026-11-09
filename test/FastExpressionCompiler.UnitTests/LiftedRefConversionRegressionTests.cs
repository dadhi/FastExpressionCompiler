#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests;
public struct LiftedRefConversionRegressionTests : ITestX
{
    public delegate long? ReadNullable(ref int? value);
    public static int Calls;
    public static long ConvertRef(ref int value) { ++Calls; return ++value; }
    public static long ConvertValue(int value) => value + 1L;
    public sealed class Cell { public int? Value; public int Reads; public int? Property { get { ++Reads; return Value; } } }
    public void Run(TestRun t) { Lifted_ref_results_preserve_nulls_and_copy_semantics(t); Nonnullable_results_throw_before_calling_for_null(t); Fields_and_properties_are_evaluated_once(t); Ordinary_method_conversions_remain_supported(t); }
    public void Lifted_ref_results_preserve_nulls_and_copy_semantics(TestContext t)
    {
        var value = Expression.Parameter(typeof(int?).MakeByRefType());
        var tree = Expression.Lambda<ReadNullable>(Expression.Convert(value, typeof(long?), typeof(LiftedRefConversionRegressionTests).GetMethod(nameof(ConvertRef))), value);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        {
            var fast = tree.CompileFast(true, flags); if (!t.IsNotNull(fast)) continue;
            int? actual = 7; Calls = 0; t.AreEqual((long?)8, fast(ref actual)); t.AreEqual((int?)7, actual); t.AreEqual(1, Calls);
            actual = null; Calls = 0; t.AreEqual((long?)null, fast(ref actual)); t.AreEqual((int?)null, actual); t.AreEqual(0, Calls);
#if !NETFRAMEWORK
            var system = tree.Compile(); actual = 7; t.AreEqual((long?)8, system(ref actual)); t.AreEqual((int?)7, actual);
            actual = null; t.AreEqual((long?)null, system(ref actual));
#endif
        }
    }
    public void Nonnullable_results_throw_before_calling_for_null(TestContext t)
    {
        var value = Expression.Parameter(typeof(int?));
        var tree = Expression.Lambda<Func<int?, long>>(Expression.Convert(value, typeof(long), typeof(LiftedRefConversionRegressionTests).GetMethod(nameof(ConvertRef))), value);
        var fast = tree.CompileFast(true); if (!t.IsNotNull(fast)) return;
        Calls = 0; t.AreEqual(8L, fast(7)); t.AreEqual(1, Calls);
        Calls = 0; t.Throws<InvalidOperationException>(() => fast(null)); t.AreEqual(0, Calls);
#if !NETFRAMEWORK
        var system = tree.Compile(); t.AreEqual(system(7), fast(7)); t.Throws<InvalidOperationException>(() => system(null));
#endif
    }
    public void Fields_and_properties_are_evaluated_once(TestContext t)
    {
        foreach (var property in new[] { false, true })
        {
            var cell = new Cell { Value = 7 };
            Expression operand = property ? Expression.Property(Expression.Constant(cell), nameof(Cell.Property)) : Expression.Field(Expression.Constant(cell), nameof(Cell.Value));
            var tree = Expression.Lambda<Func<long?>>(Expression.Convert(operand, typeof(long?), typeof(LiftedRefConversionRegressionTests).GetMethod(nameof(ConvertRef))));
            var fast = tree.CompileFast(true); if (!t.IsNotNull(fast)) return;
            Calls = 0; cell.Reads = 0; t.AreEqual((long?)8, fast()); t.AreEqual((int?)7, cell.Value); t.AreEqual(property ? 1 : 0, cell.Reads); t.AreEqual(1, Calls);
            cell.Value = null; cell.Reads = 0; Calls = 0; t.AreEqual((long?)null, fast()); t.AreEqual(property ? 1 : 0, cell.Reads); t.AreEqual(0, Calls);
        }
    }
    public void Ordinary_method_conversions_remain_supported(TestContext t)
    {
        var value = Expression.Parameter(typeof(int?));
        var tree = Expression.Lambda<Func<int?, long?>>(Expression.Convert(value, typeof(long?), typeof(LiftedRefConversionRegressionTests).GetMethod(nameof(ConvertValue))), value);
        var fast = tree.CompileFast(true); if (!t.IsNotNull(fast)) return;
        t.AreEqual((long?)8, fast(7)); t.AreEqual((long?)null, fast(null));
    }
}
#endif
