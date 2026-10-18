#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionCompiler.UnitTests;

public struct UnaryIncrementValueRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Primitive_and_nullable_values_match_system(t);
        Lifted_custom_operators_match_system(t);
        Explicit_methods_and_nullable_methods_match_system(t);
        Ignored_results_preserve_operator_effects(t);
        By_ref_operands_are_read_without_assignment(t);
    }

    public void Primitive_and_nullable_values_match_system(TestContext t)
    {
        foreach (var type in new[] { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) })
        {
            var value = type.GetField("MaxValue").GetValue(null);
            Check(t, type, value);
            Check(t, typeof(Nullable<>).MakeGenericType(type), value);
            Check(t, typeof(Nullable<>).MakeGenericType(type), null);
        }
    }

    public void Lifted_custom_operators_match_system(TestContext t)
    {
        Check(t, typeof(Counter), new Counter { Value = 42 });
        Check(t, typeof(Counter?), new Counter { Value = 42 });
        Check(t, typeof(Counter?), null);
    }

    public void Explicit_methods_and_nullable_methods_match_system(TestContext t)
    {
        var numeric = typeof(UnaryIncrementValueRegressionTests).GetMethod(nameof(Change));
        var nullable = typeof(UnaryIncrementValueRegressionTests).GetMethod(nameof(ChangeNullable));
        var reference = typeof(UnaryIncrementValueRegressionTests).GetMethod(nameof(ChangeReference));
        Check(t, typeof(int), int.MaxValue, numeric);
        Check(t, typeof(int?), 42, numeric);
        Check(t, typeof(int?), null, numeric);
        Check(t, typeof(int?), 42, nullable);
        Check(t, typeof(int?), null, nullable);
        Check(t, typeof(string), "value", reference);
        Check(t, typeof(string), null, reference);
    }

    public void Ignored_results_preserve_operator_effects(TestContext t)
    {
        foreach (var operation in new[] { ExpressionType.Increment, ExpressionType.Decrement })
        {
            var method = typeof(UnaryIncrementValueRegressionTests).GetMethod(nameof(Change));
            var body = Expression.Block(Expression.MakeUnary(operation, Expression.Constant(42), null, method), Expression.Constant(7));
            var expression = Expression.Lambda<Func<int>>(body);
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(7, fast());
            t.AreEqual(1, Calls);
        }
    }

    public void By_ref_operands_are_read_without_assignment(TestContext t)
    {
        foreach (var type in new[] { typeof(int), typeof(int?), typeof(Counter), typeof(Counter?) })
        {
            var value = type == typeof(int) || type == typeof(int?) ? (object)42 : new Counter { Value = 42 };
            Check(t, type, value, null, true);
            if (Nullable.GetUnderlyingType(type) != null) Check(t, type, null, null, true);
        }
    }

    private static void Check(TestContext t, Type type, object value, MethodInfo method = null, bool byRef = false)
    {
        foreach (var operation in new[] { ExpressionType.Increment, ExpressionType.Decrement })
        {
            var parameter = Expression.Parameter(byRef ? type.MakeByRefType() : type);
            var result = Expression.NewArrayInit(typeof(object),
                Expression.Convert(Expression.MakeUnary(operation, parameter, null, method), typeof(object)),
                Expression.Convert(parameter, typeof(object)));
            var expression = byRef
                ? Expression.Lambda(typeof(Read<>).MakeGenericType(type), result, parameter)
                : Expression.Lambda(result, parameter);
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = Invoke(expression.Compile(), value);
            var actual = Invoke(fast, value);
            if (expected is object[] e && actual is object[] a)
            {
                t.AreEqual(e[0], a[0]);
                t.AreEqual(e[1], a[1]);
            }
            else t.AreEqual(expected, actual);
        }
    }

    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { value }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public delegate object[] Read<T>(ref T value);
    public static int Calls;
    public static int Change(int value) { ++Calls; return unchecked(value + 10); }
    public static int? ChangeNullable(int? value) { ++Calls; return (value ?? 100) + 10; }
    public static string ChangeReference(string value) { ++Calls; return "changed"; }
    public struct Counter
    {
        public int Value;
        public static Counter operator ++(Counter value) { value.Value += 3; return value; }
        public static Counter operator --(Counter value) { value.Value -= 3; return value; }
    }
}
#endif
