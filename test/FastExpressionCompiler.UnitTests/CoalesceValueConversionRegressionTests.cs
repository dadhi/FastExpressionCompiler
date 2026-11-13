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
            // .NET Framework's coalesce emitter rejects valid implicit decimal conversions.
            // An explicit conditional exercises the same semantics without that runtime defect.
            var system = SysExpr.Lambda(SysExpr.Condition(
                SysExpr.Property(left, "HasValue"),
                SysExpr.Convert(SysExpr.Property(left, "Value"), body.Type),
                SysExpr.Convert(right, body.Type)), left, right).Compile();
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

}
