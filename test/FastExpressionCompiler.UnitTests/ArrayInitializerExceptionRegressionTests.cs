using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ArrayInitializerExceptionRegressionTests : ITestX
{
    public void Run(TestRun t) { Array_elements_support_exception_regions(t); Element_effects_ignored_results_and_throws_keep_order(t); }

    public void Array_elements_support_exception_regions(TestContext t)
    {
        CheckElements(t, new[] { 2, 3, 5 });
        CheckElements(t, new[] { "a", null, "c" });
        CheckElements(t, new[] { 1.25m, decimal.MinValue, decimal.MaxValue });
        CheckElements(t, new int?[] { null, 3, 5 });
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
            var body = S.NewArrayInit(typeof(int), S.Invoke(S.Constant(first)),
                S.TryFinally(S.Invoke(S.Constant(second)), S.Invoke(S.Constant(finalizer))),
                S.Invoke(S.Constant(third)));
            var fast = ignored
                ? (Delegate)S.Lambda<Action>(S.Block(body, S.Empty())).FromSysExpression().CompileFast(true)
                : S.Lambda<Func<int[]>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Action invoke = () => { if (ignored) ((Action)fast)(); else ((Func<int[]>)fast)(); };
            if (fail) t.Throws<InvalidOperationException>(invoke); else invoke();
            t.AreEqual(fail ? "0,1,2" : "0,1,2,3", string.Join(",", events));
        }
    }


    private static void CheckElements<T>(TestContext t, T[] values)
    {
        var x = S.Parameter(typeof(T));
        var y = S.Parameter(typeof(T));
        var z = S.Parameter(typeof(T));
        for (var site = 0; site < 3; ++site)
        {
            System.Linq.Expressions.Expression[] elements = { x, y, z };
            elements[site] = S.TryFinally(elements[site], S.Empty());
            var expression = S.Lambda<Func<T, T, T, T[]>>(S.NewArrayInit(typeof(T), elements), x, y, z);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var actual = fast(values[0], values[1], values[2]);
            for (var i = 0; i < values.Length; ++i) t.AreEqual(values[i], actual[i]);
        }
    }

}
