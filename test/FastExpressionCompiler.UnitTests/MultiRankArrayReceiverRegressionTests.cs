using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct MultiRankArrayReceiverRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Struct_array_element_calls_write_back(t);
        Nested_struct_array_members_write_back(t);
        Array_and_indices_are_evaluated_once_in_order(t);
        Ignored_multi_rank_array_values_balance_the_stack(t);
    }

    public struct Counter
    {
        public int Value;
        public int Next() => Value++;
    }
    public struct Nested { public Counter Counter; }

    public void Struct_array_element_calls_write_back(TestContext t)
    {
        foreach (var rank in new[] { 2, 3 })
        {
            var arrayType = typeof(Counter).MakeArrayType(rank);
            var parameter = SysExpr.Parameter(arrayType);
            var indices = new System.Linq.Expressions.Expression[rank];
            for (var i = 0; i < rank; ++i) indices[i] = SysExpr.Constant(0);
            var expression = SysExpr.Lambda(SysExpr.Call(SysExpr.ArrayAccess(parameter, indices), typeof(Counter).GetMethod(nameof(Counter.Next))), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var lengths = new int[rank];
            for (var i = 0; i < rank; ++i) lengths[i] = 1;
            var actualArray = Array.CreateInstance(typeof(Counter), lengths);
            var expectedArray = Array.CreateInstance(typeof(Counter), lengths);
            var expected = expression.Compile();
            for (var i = 0; i < 3; ++i)
            {
                t.AreEqual(i, fast.DynamicInvoke(actualArray));
                t.AreEqual(i, expected.DynamicInvoke(expectedArray));
            }
        }
    }

    public void Nested_struct_array_members_write_back(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Nested[,]));
        var member = SysExpr.Field(SysExpr.ArrayAccess(parameter, SysExpr.Constant(0), SysExpr.Constant(0)), nameof(Nested.Counter));
        var expression = SysExpr.Lambda<Func<Nested[,], int>>(SysExpr.Call(member, typeof(Counter).GetMethod(nameof(Counter.Next))), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var array = new Nested[1, 1];
        t.AreEqual(0, fast(array));
        t.AreEqual(1, fast(array));
        t.AreEqual(2, array[0, 0].Counter.Value);
    }

    public void Array_and_indices_are_evaluated_once_in_order(TestContext t)
    {
        var arrayFactory = SysExpr.Parameter(typeof(Func<Counter[,]>));
        var firstIndex = SysExpr.Parameter(typeof(Func<int>));
        var secondIndex = SysExpr.Parameter(typeof(Func<int>));
        var indexed = SysExpr.ArrayAccess(SysExpr.Invoke(arrayFactory), SysExpr.Invoke(firstIndex), SysExpr.Invoke(secondIndex));
        var expression = SysExpr.Lambda<Func<Func<Counter[,]>, Func<int>, Func<int>, int>>(
            SysExpr.Call(indexed, typeof(Counter).GetMethod(nameof(Counter.Next))), arrayFactory, firstIndex, secondIndex);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var array = new Counter[1, 1];
        var order = 0;
        t.AreEqual(0, fast(() => { t.AreEqual(0, order++); return array; },
            () => { t.AreEqual(1, order++); return 0; }, () => { t.AreEqual(2, order++); return 0; }));
        t.AreEqual(3, order);
        t.AreEqual(1, array[0, 0].Value);
    }

    public void Ignored_multi_rank_array_values_balance_the_stack(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Counter[,]));
        var indexed = SysExpr.ArrayAccess(parameter, SysExpr.Constant(0), SysExpr.Constant(0));
        var expression = SysExpr.Lambda<Func<Counter[,], int>>(SysExpr.Block(indexed, SysExpr.Constant(42)), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(42, fast(new Counter[1, 1]));
        t.Throws<IndexOutOfRangeException>(() => fast(new Counter[0, 0]));
    }
}
