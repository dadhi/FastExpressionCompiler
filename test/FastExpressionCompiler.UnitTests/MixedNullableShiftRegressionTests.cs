using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct MixedNullableShiftRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nonnullable_values_shift_by_nullable_counts(t);
        Nullable_count_evaluation_preserves_order_and_exception_blocks(t);
        Count_exception_propagates_after_left_evaluation(t);
    }

    public void Nonnullable_values_shift_by_nullable_counts(TestContext t)
    {
        Check(t, new[] { 0, 1, -1, int.MinValue, int.MaxValue });
        Check(t, new[] { 0U, 1U, uint.MaxValue });
        Check(t, new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue });
        Check(t, new[] { 0UL, 1UL, ulong.MaxValue });
    }

    private static void Check<T>(TestContext t, T[] values) where T : struct
    {
        var left = SysExpr.Parameter(typeof(T));
        var right = SysExpr.Parameter(typeof(int?));
        foreach (var operation in new[] { ExpressionType.LeftShift, ExpressionType.RightShift })
        {
#if LIGHT_EXPRESSION
            var directLeft = FastExpressionCompiler.LightExpression.Expression.Parameter(typeof(T));
            var directRight = FastExpressionCompiler.LightExpression.Expression.Parameter(typeof(int?));
            var direct = operation == ExpressionType.LeftShift
                ? FastExpressionCompiler.LightExpression.Expression.LeftShift(directLeft, directRight)
                : FastExpressionCompiler.LightExpression.Expression.RightShift(directLeft, directRight);
            t.AreEqual(typeof(T?), direct.Type);
            t.AreEqual(true, direct.IsLifted);
            t.AreEqual(true, direct.IsLiftedToNull);
#endif
            var body = SysExpr.MakeBinary(operation, left, right);
            var expression = SysExpr.Lambda<Func<T, int?, T?>>(body, left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            var ignored = SysExpr.Lambda<Action<T, int?>>(SysExpr.Block(body, SysExpr.Empty()), left, right)
                .FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
            foreach (var value in values)
            foreach (var count in new int?[] { null, int.MinValue, -1, 0, 1, 2, 31, 32, 63, 64, int.MaxValue })
            {
                t.AreEqual(system(value, count), fast(value, count));
                ignored(value, count);
            }
        }
    }

    public void Nullable_count_evaluation_preserves_order_and_exception_blocks(TestContext t)
    {
        foreach (var nullableCount in new int?[] { null, 1 })
        foreach (var nested in new[] { false, true })
        {
            var events = new List<string>();
            Func<uint> left = () => { events.Add("left"); return uint.MaxValue; };
            Func<int?> count = () => { events.Add("right"); return nullableCount; };
            System.Linq.Expressions.Expression right = SysExpr.TryCatch(SysExpr.Invoke(SysExpr.Constant(count)),
                SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(int?))));
            if (nested) right = SysExpr.Add(right, SysExpr.Constant((int?)1, typeof(int?)));
            var body = SysExpr.RightShift(SysExpr.Invoke(SysExpr.Constant(left)), right);
            var fast = SysExpr.Lambda<Func<uint?>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expectedCount = nested ? nullableCount + 1 : nullableCount;
            t.AreEqual(expectedCount.HasValue ? (uint?)(uint.MaxValue >> expectedCount.Value) : null, fast());
            t.AreEqual("left,right", string.Join(",", events));
        }
    }

    public void Count_exception_propagates_after_left_evaluation(TestContext t)
    {
        var events = new List<string>();
        Func<uint> left = () => { events.Add("left"); return 1; };
        Func<int?> count = () => { events.Add("right"); throw new InvalidOperationException(); };
        var body = SysExpr.LeftShift(SysExpr.Invoke(SysExpr.Constant(left)), SysExpr.Invoke(SysExpr.Constant(count)));
        var fast = SysExpr.Lambda<Func<uint?>>(body).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var thrown = false;
        try { fast(); }
        catch (InvalidOperationException) { thrown = true; }
        t.AreEqual(true, thrown);
        t.AreEqual("left,right", string.Join(",", events));
    }
}
