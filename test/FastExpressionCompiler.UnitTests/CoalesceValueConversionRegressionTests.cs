using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CoalesceValueConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nullable_numeric_coalesce_matches_system(t);
        Nullable_coalesce_boxes_reference_results(t);
        Coalesce_evaluates_operands_with_short_circuiting(t);
    }

    public void Nullable_numeric_coalesce_matches_system(TestContext t)
    {
        Type[] types = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(char), typeof(int),
            typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) };
        foreach (var source in types)
        foreach (var target in types)
        foreach (var nullableTarget in new[] { false, true })
        {
            var left = SysExpr.Parameter(typeof(Nullable<>).MakeGenericType(source));
            var right = SysExpr.Parameter(nullableTarget ? typeof(Nullable<>).MakeGenericType(target) : target);
            System.Linq.Expressions.BinaryExpression body;
            try { body = SysExpr.Coalesce(left, right); }
            catch (ArgumentException) { continue; }
            var expression = SysExpr.Lambda(body, left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new[] { null, Convert.ChangeType(1, source), source.GetField("MaxValue").GetValue(null) })
            {
                var fallback = Convert.ChangeType(2, target);
                t.AreEqual(system.DynamicInvoke(value, fallback), fast.DynamicInvoke(value, fallback));
                if (nullableTarget) t.AreEqual(system.DynamicInvoke(value, null), fast.DynamicInvoke(value, null));
            }
        }
    }

    public void Nullable_coalesce_boxes_reference_results(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?));
        foreach (var type in new[] { typeof(object), typeof(ValueType), typeof(IComparable) })
        {
            var right = SysExpr.Parameter(type);
            var expression = SysExpr.Lambda(SysExpr.Coalesce(left, right), left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new object[] { null, 42 })
                t.AreEqual(expression.Compile().DynamicInvoke(value, 10), fast.DynamicInvoke(value, 10));
        }
    }

    public void Coalesce_evaluates_operands_with_short_circuiting(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(Func<int?>));
        var right = SysExpr.Parameter(typeof(Func<long>));
        var body = SysExpr.Coalesce(SysExpr.Invoke(left), SysExpr.Invoke(right));
        var fast = SysExpr.Lambda<Func<Func<int?>, Func<long>, long>>(body, left, right).FromSysExpression().CompileFast(true);
        var ignored = SysExpr.Lambda<Action<Func<int?>, Func<long>>>(SysExpr.Block(body, SysExpr.Empty()), left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
        var leftCalls = 0;
        var rightCalls = 0;
        Func<long> fallback = () => { ++rightCalls; return 55; };
        t.AreEqual(3L, fast(() => { ++leftCalls; return 3; }, fallback));
        t.AreEqual(55L, fast(() => { ++leftCalls; return null; }, fallback));
        ignored(() => { ++leftCalls; return 3; }, fallback);
        t.AreEqual(3, leftCalls);
        t.AreEqual(1, rightCalls);
    }
}
