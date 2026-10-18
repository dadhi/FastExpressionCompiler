using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InvocationExceptionSpillRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Try_arguments_preserve_delegate_invocation_behavior(t);
        Invocation_target_and_arguments_are_evaluated_in_order(t);
        By_reference_arguments_remain_references_across_try_arguments(t);
    }

    public void Try_arguments_preserve_delegate_invocation_behavior(TestContext t)
    {
        var function = SysExpr.Parameter(typeof(Func<int, int, int, int>));
        var x = SysExpr.Parameter(typeof(int));
        var y = SysExpr.Parameter(typeof(int));
        var z = SysExpr.Parameter(typeof(int));
        for (var tryIndex = 0; tryIndex < 3; ++tryIndex)
        {
            System.Linq.Expressions.Expression[] arguments = { x, y, z };
            arguments[tryIndex] = SysExpr.TryFinally(arguments[tryIndex], SysExpr.Empty());
            var expression = SysExpr.Lambda<Func<Func<int, int, int, int>, int, int, int, int>>(
                SysExpr.Invoke(function, arguments), function, x, y, z);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Func<int, int, int, int> calculate = (a, b, c) => a + b * c;
            t.AreEqual(7, fast(calculate, 1, 2, 3));
            t.AreEqual(expression.Compile()(calculate, -3, 4, 5), fast(calculate, -3, 4, 5));
        }
    }

    public void Invocation_target_and_arguments_are_evaluated_in_order(TestContext t)
    {
        var target = SysExpr.Parameter(typeof(Func<Func<int, int, int, int>>));
        var first = SysExpr.Parameter(typeof(Func<int>));
        var second = SysExpr.Parameter(typeof(Func<int>));
        var third = SysExpr.Parameter(typeof(Func<int>));
        var finallyAction = SysExpr.Parameter(typeof(Action));
        var expression = SysExpr.Lambda<Func<Func<Func<int, int, int, int>>, Func<int>, Func<int>, Func<int>, Action, int>>(
            SysExpr.Invoke(SysExpr.Invoke(target), SysExpr.TryFinally(SysExpr.Invoke(first), SysExpr.Invoke(finallyAction)),
                SysExpr.Invoke(second), SysExpr.Invoke(third)), target, first, second, third, finallyAction);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var order = new List<int>();
        Func<Func<int, int, int, int>> factory = () => { order.Add(0); return (a, b, c) => { order.Add(5); return a + b * c; }; };
        Func<int> firstValue = () => { order.Add(1); return 1; };
        Func<int> secondValue = () => { order.Add(3); return 2; };
        Func<int> thirdValue = () => { order.Add(4); return 3; };
        Action finalizer = () => order.Add(2);
        t.AreEqual(7, fast(factory, firstValue, secondValue, thirdValue, finalizer));
        t.AreEqual("0,1,2,3,4,5", string.Join(",", order));
        order.Clear();
        t.AreEqual(7, expression.Compile()(factory, firstValue, secondValue, thirdValue, finalizer));
        t.AreEqual("0,1,2,3,4,5", string.Join(",", order));
    }

    public delegate int RefAdder(ref int value, int increment);
    public delegate int RefCaller(ref int value, RefAdder add);

    public void By_reference_arguments_remain_references_across_try_arguments(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(int).MakeByRefType());
        var function = SysExpr.Parameter(typeof(RefAdder));
        var expression = SysExpr.Lambda<RefCaller>(SysExpr.Invoke(function, value,
            SysExpr.TryFinally(SysExpr.Constant(2), SysExpr.Empty())), value, function);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        RefAdder add = (ref int item, int increment) => item += increment;
        var actualValue = 40;
        var expectedValue = 40;
        t.AreEqual(42, fast(ref actualValue, add));
        t.AreEqual(42, actualValue);
        t.AreEqual(42, expression.Compile()(ref expectedValue, add));
        t.AreEqual(expectedValue, actualValue);
    }
}
