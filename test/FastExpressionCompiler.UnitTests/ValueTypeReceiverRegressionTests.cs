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

public struct ValueTypeReceiverRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Object_methods_on_value_receivers_match_system(t);
        Interface_methods_properties_and_indexers_match_system(t);
        Ignored_getters_preserve_side_effects(t);
        By_ref_nullable_members_use_the_original_address(t);
        Interface_setters_mutate_struct_receivers(t);
        By_ref_nullable_null_comparisons_keep_the_address(t);
    }

    public void Object_methods_on_value_receivers_match_system(TestContext t)
    {
        Type[] types = { typeof(int), typeof(DayOfWeek), typeof(Indexed), typeof(int?), typeof(Indexed?) };
        object[] values = { 42, DayOfWeek.Friday, new Indexed { Seed = 40 }, 42, new Indexed { Seed = 40 } };
        foreach (var name in new[] { "GetType", "ToString", "GetHashCode" })
        for (var i = 0; i < types.Length; ++i)
        for (var shape = 0; shape != 4; ++shape)
        foreach (var value in types[i].IsNullable() ? new[] { null, values[i] } : new[] { values[i] })
        {
            var method = typeof(object).GetMethod(name, Type.EmptyTypes);
            var parameter = SysExpr.Parameter(shape == 3 ? types[i].MakeByRefType() : types[i]);
            var local = SysExpr.Variable(types[i]);
            var instance = shape == 1 ? (SysExpr)SysExpr.Constant(value, types[i]) : shape == 2 ? local : parameter;
            var call = SysExpr.Convert(SysExpr.Call(instance, method), typeof(object));
            var body = shape == 2 ? (SysExpr)SysExpr.Block(new[] { local }, SysExpr.Assign(local, parameter), call) : call;
            var expression = shape == 3 ? SysExpr.Lambda(typeof(Read<>).MakeGenericType(types[i]), body, parameter) : SysExpr.Lambda(body, parameter);
            var fastParameter = Expr.Parameter(shape == 3 ? types[i].MakeByRefType() : types[i]);
            var fastLocal = Expr.Variable(types[i]);
            var fastInstance = shape == 1 ? (Expr)Expr.Constant(value, types[i]) : shape == 2 ? fastLocal : fastParameter;
            var fastCall = Expr.Convert(Expr.Call(fastInstance, method), typeof(object));
            var fastBody = shape == 2 ? (Expr)Expr.Block(new[] { fastLocal }, Expr.Assign(fastLocal, fastParameter), fastCall) : fastCall;
            var fastExpression = shape == 3 ? Expr.Lambda(typeof(Read<>).MakeGenericType(types[i]), fastBody, fastParameter) : Expr.Lambda(fastBody, fastParameter);
            var fast = fastExpression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(Invoke(expression.Compile(), value), Invoke(fast, value));
        }
    }

    public void Interface_methods_properties_and_indexers_match_system(TestContext t)
    {
        var value = new Indexed { Seed = 40 };
        for (var shape = 0; shape != 3; ++shape)
        {
            var parameter = Expr.Parameter(shape == 2 ? typeof(Indexed).MakeByRefType() : typeof(Indexed));
            var instance = shape == 0 ? (Expr)Expr.Constant(value) : parameter;
            Expr[] calls = { Expr.Call(instance, typeof(IIndexed).GetMethod("Read")), Expr.Property(instance, typeof(IIndexed).GetProperty("Value")),
                Expr.MakeIndex(instance, typeof(IIndexed).GetProperty("Item"), new[] { Expr.Constant(2) }) };
            var body = Expr.NewArrayInit(typeof(object), Expr.Convert(calls[0], typeof(object)), Expr.Convert(calls[1], typeof(object)), Expr.Convert(calls[2], typeof(object)));
            var expression = shape == 2 ? Expr.Lambda(typeof(Read<>).MakeGenericType(typeof(Indexed)), body, parameter) : Expr.Lambda(body, parameter);
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var results = (object[])fast.DynamicInvoke(value);
            t.AreEqual((object)41, results[0]);
            t.AreEqual((object)40, results[1]);
            t.AreEqual((object)42, results[2]);
        }
    }

    public void Ignored_getters_preserve_side_effects(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(Indexed));
        var body = Expr.Block(Expr.Property(parameter, typeof(IIndexed).GetProperty("Value")),
            Expr.MakeIndex(parameter, typeof(IIndexed).GetProperty("Item"), new[] { Expr.Constant(2) }), Expr.Empty());
        var fast = Expr.Lambda<Action<Indexed>>(body, parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Calls = 0;
        fast(new Indexed { Seed = 40 });
        t.AreEqual(2, Calls);
    }

    public void By_ref_nullable_members_use_the_original_address(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(int?).MakeByRefType());
        var fast = Expr.Lambda<HasValue>(Expr.Property(parameter, "HasValue"), parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        int? value = null;
        t.IsFalse(fast(ref value));
        value = 42;
        t.IsTrue(fast(ref value));
    }

    public void By_ref_nullable_null_comparisons_keep_the_address(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(int?).MakeByRefType());
        foreach (var body in new[] { Expr.Equal(parameter, Expr.Constant(null, typeof(int?))),
            Expr.Equal(Expr.Constant(null, typeof(int?)), parameter) })
        {
            var fast = Expr.Lambda<HasValue>(body, parameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            int? value = null;
            t.IsTrue(fast(ref value));
            value = 42;
            t.IsFalse(fast(ref value));
        }
    }

    public void Interface_setters_mutate_struct_receivers(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(Indexed));
        var property = typeof(IIndexed).GetProperty("Value");
        var indexer = typeof(IIndexed).GetProperty("Item");
        var body = Expr.Block(
            Expr.Assign(Expr.Property(parameter, property), Expr.Constant(50)),
            Expr.Assign(Expr.MakeIndex(parameter, indexer, new[] { Expr.Constant(2) }), Expr.Constant(62)),
            Expr.Property(parameter, property));
        var fast = Expr.Lambda<Func<Indexed, int>>(body, parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(60, fast(new Indexed { Seed = 40 }));
        var constant = Expr.Constant(new Indexed { Seed = 40 });
        var ignored = Expr.Lambda<Action>(Expr.Block(
            Expr.Assign(Expr.MakeIndex(constant, indexer, new[] { Expr.Constant(2) }), Expr.Constant(62)), Expr.Empty())).CompileFast(true);
        if (!t.IsNotNull(ignored)) return;
        Calls = 0;
        ignored();
        t.AreEqual(1, Calls);
    }

    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { value }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public delegate object Read<T>(ref T value);
    public delegate bool HasValue(ref int? value);
    public interface IIndexed { int this[int index] { get; set; } int Value { get; set; } int Read(); }
    public static int Calls;
    public struct Indexed : IIndexed
    {
        public int Seed;
        public int this[int index] { get { ++Calls; return Seed + index; } set { ++Calls; Seed = value - index; } }
        public int Value { get { ++Calls; return Seed; } set { ++Calls; Seed = value; } }
        public int Read() => Seed + 1;
    }
}
