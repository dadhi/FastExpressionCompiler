using System;
using System.Linq;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeCompilerLayoutBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        VerifyIL_Simple_behavior(t);
        VerifyIL_Exceptions_behavior(t);
        VerifyIL_Closure1_behavior(t);
        VerifyIL_Closure2_behavior(t);
        VerifyIL_Closure3_behavior(t);
        AboveByteMaxArityArgIL_behavior(t);
        AboveByteMaxArityArgAddressIL_behavior(t);
    }

    public void VerifyIL_Simple_behavior(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int>> expression = () => Math.Abs(42);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(42, fast());
        t.AreEqual(expression.Compile()(), fast());
    }

    public void VerifyIL_Exceptions_behavior(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(int), "x");
        var expression = SysExpr.Lambda<Func<int, int>>(SysExpr.TryCatchFinally(
            SysExpr.Call(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }),
                SysExpr.Divide(SysExpr.Constant(42), parameter)),
            SysExpr.Empty(), SysExpr.Catch(typeof(DivideByZeroException), SysExpr.Constant(-1))), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var expected = expression.Compile();
        foreach (var pair in new[] { new[] { 0, -1 }, new[] { 1, 42 }, new[] { -1, 42 }, new[] { 2, 21 }, new[] { -2, 21 }, new[] { 100, 0 } })
        {
            t.AreEqual(pair[1], fast(pair[0]));
            t.AreEqual(expected(pair[0]), fast(pair[0]));
        }
    }

    public void VerifyIL_Closure1_behavior(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<Func<int>>> expression = () => () => 42;
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(42, fast()());
        t.AreEqual(expression.Compile()()(), fast()());
    }

    public void VerifyIL_Closure2_behavior(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>> expression = x => () => x;
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var first = fast(42);
        var second = fast(-7);
        t.AreEqual(42, first());
        t.AreEqual(-7, second());
        t.AreEqual(42, first());
        foreach (var value in new[] { int.MinValue, 0, int.MaxValue })
            t.AreEqual(expression.Compile()(value)(), fast(value)());
    }

    public void VerifyIL_Closure3_behavior(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int, int>>> expression = x => y => unchecked(x + y);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var first = fast(42);
        var second = fast(-7);
        t.AreEqual(45, first(3));
        t.AreEqual(-4, second(3));
        t.AreEqual(46, first(4));
        foreach (var left in new[] { int.MinValue, -1, 0, int.MaxValue })
        foreach (var right in new[] { int.MinValue, -1, 0, int.MaxValue })
        {
            t.AreEqual(unchecked(left + right), fast(left)(right));
            t.AreEqual(expression.Compile()(left)(right), fast(left)(right));
        }
    }

    public void AboveByteMaxArityArgIL_behavior(TestContext t)
    {
        var parameters = Enumerable.Range(0, 300).Select(_ => SysExpr.Parameter(typeof(int))).ToArray();
        var expression = SysExpr.Lambda(parameters.Last(), parameters);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var expected = expression.Compile();
        var arguments = Enumerable.Range(0, 300).Select(i => (object)(i * 17 - 300)).ToArray();
        foreach (var last in new[] { int.MinValue, 42, int.MaxValue })
        {
            arguments[299] = last;
            t.AreEqual(last, fast.DynamicInvoke(arguments));
            t.AreEqual(expected.DynamicInvoke(arguments), fast.DynamicInvoke(arguments));
        }
    }

    public struct Mutable
    {
        public bool Mutated;
        public Mutable Mutate() { Mutated = true; return this; }
    }

    public void AboveByteMaxArityArgAddressIL_behavior(TestContext t)
    {
        var mutableParameter = SysExpr.Parameter(typeof(Mutable));
        var parameters = Enumerable.Range(0, 299).Select(_ => SysExpr.Parameter(typeof(int))).Concat(new[] { mutableParameter }).ToArray();
        var expression = SysExpr.Lambda(SysExpr.Call(mutableParameter, typeof(Mutable).GetMethod(nameof(Mutable.Mutate))), parameters);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var arguments = Enumerable.Range(0, 299).Select(i => (object)(i * 17 - 300)).Concat(new object[] { new Mutable() }).ToArray();
        var expected = (Mutable)expression.Compile().DynamicInvoke(arguments);
        var actual = (Mutable)fast.DynamicInvoke(arguments);
        t.IsTrue(expected.Mutated);
        t.IsTrue(actual.Mutated);
        t.IsFalse(((Mutable)arguments[299]).Mutated);
    }
}
