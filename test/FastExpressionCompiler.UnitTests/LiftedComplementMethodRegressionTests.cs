#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionCompiler.UnitTests;

public struct LiftedComplementMethodRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Lifted_custom_operators_preserve_null(t);
        Explicit_lifted_methods_support_different_result_types(t);
        Nullable_methods_receive_null(t);
        Ignored_results_preserve_method_effects(t);
        Ref_operands_are_read_without_assignment(t);
    }

    public void Lifted_custom_operators_preserve_null(TestContext t) =>
        Check(t, typeof(Counter?), null, new object[] { null, new Counter { Value = 7 }, new Counter { Value = 42 } });

    public void Explicit_lifted_methods_support_different_result_types(TestContext t) =>
        Check(t, typeof(int?), nameof(Change), new object[] { null, 7, 42 });

    public void Nullable_methods_receive_null(TestContext t) =>
        Check(t, typeof(int?), nameof(ChangeNullable), new object[] { null, 7, 42 });

    public void Ref_operands_are_read_without_assignment(TestContext t)
    {
        Check(t, typeof(int?), nameof(Change), new object[] { null, 7, 42 }, true);
        Check(t, typeof(Counter?), null, new object[] { null, new Counter { Value = 7 }, new Counter { Value = 42 } }, true);
    }

    public void Ignored_results_preserve_method_effects(TestContext t)
    {
        foreach (var operation in new[] { ExpressionType.Not, ExpressionType.OnesComplement })
        foreach (var value in new int?[] { null, 7, 42 })
        {
            var method = typeof(LiftedComplementMethodRegressionTests).GetMethod(nameof(Change));
            var expression = Expression.Lambda<Func<int>>(Expression.Block(
                Expression.MakeUnary(operation, Expression.Constant(value, typeof(int?)), null, method), Expression.Constant(7)));
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            var expected = Invoke(expression.Compile(), Array.Empty<object>());
            var expectedCalls = Calls;
            Calls = 0;
            var actual = Invoke(fast, Array.Empty<object>());
            t.AreEqual(expected, actual);
            t.AreEqual(expectedCalls, Calls);
        }
    }

    private static void Check(TestContext t, Type type, string methodName, object[] values, bool byRef = false)
    {
        var method = methodName == null ? null : typeof(LiftedComplementMethodRegressionTests).GetMethod(methodName);
        foreach (var operation in new[] { ExpressionType.Not, ExpressionType.OnesComplement })
        {
            var parameter = Expression.Parameter(byRef ? type.MakeByRefType() : type);
            var body = Expression.NewArrayInit(typeof(object),
                Expression.Convert(Expression.MakeUnary(operation, parameter, null, method), typeof(object)),
                Expression.Convert(parameter, typeof(object)));
            var expression = byRef
                ? Expression.Lambda(typeof(Read<>).MakeGenericType(type), body, parameter)
                : Expression.Lambda(body, parameter);
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var system = expression.Compile();
            foreach (var value in values)
            {
                Calls = 0;
                var expected = Invoke(system, new[] { value });
                var expectedCalls = Calls;
                Calls = 0;
                var actual = Invoke(fast, new[] { value });
                if (expected is object[] e && actual is object[] a)
                {
                    t.AreEqual(e[0], a[0]);
                    t.AreEqual(e[1], a[1]);
                }
                else t.AreEqual(expected, actual);
                t.AreEqual(expectedCalls, Calls);
            }
        }
    }

    private static object Invoke(Delegate function, object[] arguments)
    {
        try { return function.DynamicInvoke(arguments); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public static int Calls;
    public static long Change(int value) { ++Calls; if (value == 42) throw new InvalidOperationException(); return ~((long)value); }
    public static long? ChangeNullable(int? value) { ++Calls; return value.HasValue ? ~((long)value.Value) : 100; }
    public delegate object[] Read<T>(ref T value);
    public struct Counter
    {
        public int Value;
        public static Counter operator ~(Counter value) { ++Calls; return new Counter { Value = ~value.Value }; }
        public static Counter operator !(Counter value) { ++Calls; return new Counter { Value = -value.Value }; }
    }
}
#endif
