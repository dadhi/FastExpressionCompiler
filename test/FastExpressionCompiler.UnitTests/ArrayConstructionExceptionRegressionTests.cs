using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ArrayConstructionExceptionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Array_elements_support_exception_regions(t);
        Element_effects_ignored_results_and_throws_keep_order(t);
        Multidimensional_bounds_support_exception_regions(t);
        Bound_failures_preserve_evaluation_order(t);
    }

    public void Array_elements_support_exception_regions(TestContext t)
    {
        CheckElements(t, new[] { 2, 3, 5 });
        CheckElements(t, new[] { "a", null, "c" });
        CheckElements(t, new[] { 1.25m, decimal.MinValue, decimal.MaxValue });
        CheckElements(t, new int?[] { null, 3, 5 });
    }

    private static void CheckElements<T>(TestContext t, T[] values)
    {
        var x = SysExpr.Parameter(typeof(T));
        var y = SysExpr.Parameter(typeof(T));
        var z = SysExpr.Parameter(typeof(T));
        for (var site = 0; site < 3; ++site)
        {
            System.Linq.Expressions.Expression[] elements = { x, y, z };
            elements[site] = SysExpr.TryFinally(elements[site], SysExpr.Empty());
            var expression = SysExpr.Lambda<Func<T, T, T, T[]>>(SysExpr.NewArrayInit(typeof(T), elements), x, y, z);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var actual = fast(values[0], values[1], values[2]);
            for (var i = 0; i < values.Length; ++i) t.AreEqual(values[i], actual[i]);
        }
    }

    public void Element_effects_ignored_results_and_throws_keep_order(TestContext t)
    {
        foreach (var fail in new[] { false, true })
        foreach (var ignored in new[] { false, true })
        {
            var events = new List<int>();
            Func<int> first = () => { events.Add(0); return 2; };
            Func<int> second = () => { events.Add(1); if (fail) throw new InvalidOperationException(); return 3; };
            Action finalizer = () => events.Add(2);
            Func<int> third = () => { events.Add(3); return 5; };
            var body = SysExpr.NewArrayInit(typeof(int), SysExpr.Invoke(SysExpr.Constant(first)),
                SysExpr.TryFinally(SysExpr.Invoke(SysExpr.Constant(second)), SysExpr.Invoke(SysExpr.Constant(finalizer))),
                SysExpr.Invoke(SysExpr.Constant(third)));
            var fast = ignored
                ? (Delegate)SysExpr.Lambda<Action>(SysExpr.Block(body, SysExpr.Empty())).FromSysExpression().CompileFast(true)
                : SysExpr.Lambda<Func<int[]>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Action invoke = () => { if (ignored) ((Action)fast)(); else ((Func<int[]>)fast)(); };
            if (fail) t.Throws<InvalidOperationException>(invoke); else invoke();
            t.AreEqual(fail ? "0,1,2" : "0,1,2,3", string.Join(",", events));
        }
    }

    public void Multidimensional_bounds_support_exception_regions(TestContext t)
    {
        var events = new List<int>();
        Func<int> x = () => { events.Add(0); return 2; };
        Func<int> y = () => { events.Add(1); return 3; };
        Func<int> z = () => { events.Add(3); return 5; };
        Action finalizer = () => events.Add(2);
        var body = SysExpr.NewArrayBounds(typeof(int), SysExpr.Invoke(SysExpr.Constant(x)),
            SysExpr.TryFinally(SysExpr.Invoke(SysExpr.Constant(y)), SysExpr.Invoke(SysExpr.Constant(finalizer))),
            SysExpr.Invoke(SysExpr.Constant(z)));
        var fast = SysExpr.Lambda<Func<int[,,]>>(body).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var array = fast();
        t.AreEqual(2, array.GetLength(0));
        t.AreEqual(3, array.GetLength(1));
        t.AreEqual(5, array.GetLength(2));
        t.AreEqual("0,1,2,3", string.Join(",", events));
    }

    public void Bound_failures_preserve_evaluation_order(TestContext t)
    {
        foreach (var firstThrows in new[] { false, true })
        {
            var events = new List<int>();
            Func<int> first = () => { events.Add(0); if (firstThrows) throw new InvalidOperationException(); return -1; };
            Func<int> second = () => { events.Add(1); return 3; };
            Action finalizer = () => events.Add(2);
            var body = SysExpr.NewArrayBounds(typeof(int), SysExpr.Invoke(SysExpr.Constant(first)),
                SysExpr.TryFinally(SysExpr.Invoke(SysExpr.Constant(second)), SysExpr.Invoke(SysExpr.Constant(finalizer))));
            var fast = SysExpr.Lambda<Func<int[,]>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            if (firstThrows) t.Throws<InvalidOperationException>(() => fast());
            else t.Throws<OverflowException>(() => fast());
            t.AreEqual(firstThrows ? "0" : "0,1,2", string.Join(",", events));
        }
    }
}
