using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ArrayBoundsExceptionRegressionTests : ITestX
{
    public void Run(TestRun t) { Multidimensional_bounds_support_exception_regions(t); Bound_failures_preserve_evaluation_order(t); }

    public void Multidimensional_bounds_support_exception_regions(TestContext t)
    {
        var events = new List<int>();
        Func<int> x = () => { events.Add(0); return 2; };
        Func<int> y = () => { events.Add(1); return 3; };
        Func<int> z = () => { events.Add(3); return 5; };
        Action finalizer = () => events.Add(2);
        var body = S.NewArrayBounds(typeof(int), S.Invoke(S.Constant(x)),
            S.TryFinally(S.Invoke(S.Constant(y)), S.Invoke(S.Constant(finalizer))),
            S.Invoke(S.Constant(z)));
        var fast = S.Lambda<Func<int[,,]>>(body).FromSysExpression().CompileFast(true);
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
            var body = S.NewArrayBounds(typeof(int), S.Invoke(S.Constant(first)),
                S.TryFinally(S.Invoke(S.Constant(second)), S.Invoke(S.Constant(finalizer))));
            var fast = S.Lambda<Func<int[,]>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            if (firstThrows) t.Throws<InvalidOperationException>(() => fast());
            else t.Throws<OverflowException>(() => fast());
            t.AreEqual(firstThrows ? "0" : "0,1,2", string.Join(",", events));
        }
    }

}
