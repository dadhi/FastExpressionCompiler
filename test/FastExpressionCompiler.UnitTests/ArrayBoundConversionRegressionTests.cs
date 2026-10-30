using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ArrayBoundConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Vector_bounds_use_checked_integer_conversion(t);
        Multidimensional_bounds_use_checked_integer_conversion(t);
        Ignored_array_results_evaluate_bounds_once(t);
    }

    public void Vector_bounds_use_checked_integer_conversion(TestContext t) => Check(t, false);
    public void Multidimensional_bounds_use_checked_integer_conversion(TestContext t) => Check(t, true);

    private static void Check(TestContext t, bool multidimensional)
    {
        Type[] types = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };
        object[][] values = {
            new object[] { (byte)0, (byte)3 }, new object[] { (sbyte)-1, (sbyte)0, (sbyte)3 },
            new object[] { (short)-1, (short)0, (short)3 }, new object[] { (ushort)0, (ushort)3 },
            new object[] { -1, 0, 3 }, new object[] { 0u, 3u, uint.MaxValue },
            new object[] { -1L, 0L, 3L, 1L << 32, long.MaxValue }, new object[] { 0UL, 3UL, 1UL << 32, ulong.MaxValue }
        };
        for (var i = 0; i < types.Length; ++i)
        foreach (var nullable in new[] { false, true })
        {
            var type = nullable ? typeof(Nullable<>).MakeGenericType(types[i]) : types[i];
            var sysParameter = SysExpr.Parameter(type);
            var sysBounds = multidimensional ? new SysExpr[] { sysParameter, SysExpr.Constant(2) } : new SysExpr[] { sysParameter };
            var system = SysExpr.Lambda(SysExpr.NewArrayBounds(typeof(int), sysBounds), sysParameter).Compile();
            var parameter = Expr.Parameter(type);
            var bounds = multidimensional ? new Expr[] { parameter, Expr.Constant(2) } : new Expr[] { parameter };
            var fast = Expr.Lambda(Expr.NewArrayBounds(typeof(int), bounds), parameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in values[i]) t.AreEqual(Invoke(system, value), Invoke(fast, value));
            if (nullable) t.AreEqual(Invoke(system, null), Invoke(fast, null));
        }
    }

    public void Ignored_array_results_evaluate_bounds_once(TestContext t)
    {
        var getLength = Expr.Parameter(typeof(Func<int?>));
        var fast = Expr.Lambda<Action<Func<int?>>>(Expr.Block(
            Expr.NewArrayBounds(typeof(int), Expr.Invoke(getLength)), Expr.Empty()), getLength).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        fast(() => { ++calls; return 3; });
        t.AreEqual(1, calls);
        t.Throws<InvalidOperationException>(() => fast(() => { ++calls; return null; }));
        t.AreEqual(2, calls);
    }

    private static object Invoke(Delegate function, object length)
    {
        try
        {
            var array = (Array)function.DynamicInvoke(new[] { length });
            return array.Rank + ":" + array.Length + ":" + array.GetLength(0) + ":" + array.GetLowerBound(0);
        }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
