using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NullableCompoundWritebackRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Null_compound_results_are_written_to_property_and_indexer(t);
        Nullable_callback_receives_null_and_writes_converted_result(t);
        Nullable_increment_preserves_postfix_result_and_calls_setter(t);
    }

    public sealed class Box
    {
        public int? Stored;
        public int Gets, Sets;
        public int? Value { get { ++Gets; return Stored; } set { ++Sets; Stored = value; } }
        public int? this[int index] { get { ++Gets; return Stored; } set { ++Sets; Stored = value; } }
    }
    public static int? AddNullable(int? left, int? right) => left + right;

    private static System.Linq.Expressions.Expression Target(Box box, bool index) => index
        ? SysExpr.MakeIndex(SysExpr.Constant(box), typeof(Box).GetProperty("Item"), new[] { SysExpr.Constant(0) })
        : SysExpr.Property(SysExpr.Constant(box), nameof(Box.Value));

    public void Null_compound_results_are_written_to_property_and_indexer(TestContext t)
    {
        foreach (var index in new[] { false, true })
        foreach (var left in new int?[] { null, 5 })
        foreach (var right in new int?[] { null, 3 })
        {
            var box = new Box { Stored = left };
            var body = SysExpr.AddAssign(Target(box, index), SysExpr.Constant(right, typeof(int?)));
            var fast = SysExpr.Lambda<Func<int?>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(left + right, fast());
            t.AreEqual(left + right, box.Stored);
            t.AreEqual(1, box.Gets);
            t.AreEqual(1, box.Sets);
        }
    }

    public void Nullable_callback_receives_null_and_writes_converted_result(TestContext t)
    {
        foreach (var index in new[] { false, true })
        foreach (var ignored in new[] { false, true })
        foreach (var left in new int?[] { null, 5 })
        foreach (var right in new int?[] { null, 3 })
        {
            var box = new Box { Stored = left };
            var calls = 0;
            int? observed = -1;
            Func<int?, int?> convert = number => { ++calls; observed = number; return number ?? 42; };
            var value = SysExpr.Parameter(typeof(int?));
            var conversion = SysExpr.Lambda<Func<int?, int?>>(SysExpr.Invoke(SysExpr.Constant(convert), value), value);
            var body = SysExpr.AddAssign(Target(box, index), SysExpr.Constant(right, typeof(int?)),
                typeof(NullableCompoundWritebackRegressionTests).GetMethod(nameof(AddNullable)), conversion);
            if (ignored)
            {
                var fast = SysExpr.Lambda<Action>(SysExpr.Block(body, SysExpr.Empty())).FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                fast();
            }
            else
            {
                var fast = SysExpr.Lambda<Func<int?>>(body).FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                t.AreEqual((left + right) ?? 42, fast());
            }
            t.AreEqual(left + right, observed);
            t.AreEqual((left + right) ?? 42, box.Stored);
            t.AreEqual(1, calls);
            t.AreEqual(1, box.Gets);
            t.AreEqual(1, box.Sets);
        }
    }

    public void Nullable_increment_preserves_postfix_result_and_calls_setter(TestContext t)
    {
        foreach (var index in new[] { false, true })
        foreach (var post in new[] { false, true })
        foreach (var value in new int?[] { null, 5 })
        {
            var box = new Box { Stored = value };
            var body = post ? SysExpr.PostIncrementAssign(Target(box, index)) : SysExpr.PreIncrementAssign(Target(box, index));
            var fast = SysExpr.Lambda<Func<int?>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(post ? value : value + 1, fast());
            t.AreEqual(value + 1, box.Stored);
            t.AreEqual(1, box.Gets);
            t.AreEqual(1, box.Sets);
        }
    }
}
