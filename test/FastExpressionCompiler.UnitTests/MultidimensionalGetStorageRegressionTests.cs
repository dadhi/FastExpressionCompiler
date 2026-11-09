using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct MultidimensionalGetStorageRegressionTests : ITestX
{
    public struct Counter { public int Value; public int Next() => Value++; public int Add(int value) => Value += value; }
    public static void Bump(ref Counter value) => ++value.Value;
    public void Run(TestRun t) { Get_receivers_preserve_element_storage(t); Ref_arguments_preserve_element_storage(t); Receiver_addresses_survive_exception_arguments(t); Static_dynamic_method_calls_remain_supported(t); }
    public void Get_receivers_preserve_element_storage(TestContext t)
    {
        var array = S.Parameter(typeof(Counter[,])); var get = S.ArrayIndex(array, S.Constant(0), S.Constant(1));
        var call = S.Call(get, typeof(Counter).GetMethod(nameof(Counter.Next)));
        var fast = S.Lambda<Func<Counter[,], int>>(call, array).FromSysExpression().CompileFast(true);
        var ignored = S.Lambda<Action<Counter[,]>>(S.Block(call, S.Empty()), array).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
        var values = new Counter[1, 2]; t.AreEqual(0, fast(values)); t.AreEqual(1, fast(values));
        ignored(values); t.AreEqual(3, values[0, 1].Value);
        t.Throws<NullReferenceException>(() => ignored(null)); t.Throws<IndexOutOfRangeException>(() => ignored(new Counter[0, 0]));
        var copy = S.Lambda<Func<Counter[,], Counter>>(get, array).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(copy)) { var value = copy(values); ++value.Value; t.AreEqual(3, values[0, 1].Value); }
    }
    public void Ref_arguments_preserve_element_storage(TestContext t)
    {
        var array = S.Parameter(typeof(Counter[,])); var get = S.ArrayIndex(array, S.Constant(0), S.Constant(1));
        var fast = S.Lambda<Action<Counter[,]>>(S.Call(typeof(MultidimensionalGetStorageRegressionTests).GetMethod(nameof(Bump)), get), array).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var values = new Counter[1, 2]; fast(values); t.AreEqual(1, values[0, 1].Value);
    }
    public void Receiver_addresses_survive_exception_arguments(TestContext t)
    {
        var array = S.Parameter(typeof(Counter[,,]));
        var get = S.ArrayIndex(array, S.Constant(0), S.Constant(1), S.Constant(0));
        var rhs = S.TryFinally(S.Constant(3), S.Empty());
        var fast = S.Lambda<Func<Counter[,,], int>>(S.Call(get, typeof(Counter).GetMethod(nameof(Counter.Add)), rhs), array).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var values = new Counter[1, 2, 1]; t.AreEqual(3, fast(values)); t.AreEqual(6, fast(values)); t.AreEqual(6, values[0, 1, 0].Value);
    }
    public void Static_dynamic_method_calls_remain_supported(TestContext t)
    {
        var method = new System.Reflection.Emit.DynamicMethod("ArrayAddressControl", typeof(int), Type.EmptyTypes);
        var il = method.GetILGenerator(); il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4, 37); il.Emit(System.Reflection.Emit.OpCodes.Ret);
        var fast = S.Lambda<Func<int>>(S.Call(method)).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(37, fast());
    }
}
