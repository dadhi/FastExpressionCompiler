using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NegationBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Short_checked_negation_matches_runtime(t);
        Nullable_short_checked_negation_matches_runtime(t);
        Primitive_negation_preserves_values_and_overflow(t);
        Nullable_negation_preserves_values_overflow_and_null(t);
        Ref_negation_reads_value_without_assignment(t);
        Ignored_checked_negation_still_throws(t);
        Custom_negation_operators_are_called(t);
#if !LIGHT_EXPRESSION
        Explicit_negation_methods_are_called(t);
        Unsupported_builtin_types_are_rejected(t);
#endif
    }

    public void Short_checked_negation_matches_runtime(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(short));
        var expression = SysExpr.Lambda<Func<short, short>>(SysExpr.NegateChecked(parameter), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new short[] { 0, 1, -1, short.MaxValue }) t.AreEqual((short)-value, fast(value));
        t.Throws<OverflowException>(() => fast(short.MinValue));
    }

    public void Nullable_short_checked_negation_matches_runtime(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(short?));
        var expression = SysExpr.Lambda<Func<short?, short?>>(SysExpr.NegateChecked(parameter), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new short?[] { null, 0, 1, -1, short.MaxValue }) t.AreEqual((short?)-value, fast(value));
        t.Throws<OverflowException>(() => fast(short.MinValue));
    }

    public void Primitive_negation_preserves_values_and_overflow(TestContext t) => CheckPrimitives(t, false, false);
    public void Nullable_negation_preserves_values_overflow_and_null(TestContext t) => CheckPrimitives(t, true, false);
    public void Ref_negation_reads_value_without_assignment(TestContext t)
    {
        CheckPrimitives(t, false, true);
        CheckPrimitives(t, true, true);
    }

    private static void CheckPrimitives(TestContext t, bool nullable, bool byRef)
    {
        foreach (var type in new[] { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) })
        {
            var values = new object[] { Convert.ChangeType(0, type), Convert.ChangeType(1, type), Convert.ChangeType(-1, type),
                type.GetField("MinValue").GetValue(null), type.GetField("MaxValue").GetValue(null) };
            var actualType = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
            foreach (var value in values) Check(t, actualType, value, byRef);
            if (nullable) Check(t, actualType, null, byRef);
        }
    }

    public void Ignored_checked_negation_still_throws(TestContext t)
    {
        foreach (var nullable in new[] { false, true })
        {
            var type = nullable ? typeof(short?) : typeof(short);
            var parameter = SysExpr.Parameter(type);
            var expression = SysExpr.Lambda(SysExpr.Block(SysExpr.NegateChecked(parameter), SysExpr.Constant(7)), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(typeof(OverflowException), Invoke(fast, short.MinValue));
            t.AreEqual(7, Invoke(fast, (short)1));
            if (nullable) t.AreEqual(7, Invoke(fast, null));
        }
    }

    public void Custom_negation_operators_are_called(TestContext t)
    {
        Check(t, typeof(Counter), new Counter { Value = 42 }, false);
        Check(t, typeof(Counter?), new Counter { Value = 42 }, false);
        Check(t, typeof(Counter?), null, false);
    }

#if !LIGHT_EXPRESSION
    public void Explicit_negation_methods_are_called(TestContext t)
    {
        foreach (var method in new[] { nameof(Change), nameof(ChangeNullable) })
        {
            var info = typeof(NegationBehaviorRegressionTests).GetMethod(method);
            Check(t, typeof(int?), 42, false, info);
            Check(t, typeof(int?), null, false, info);
        }
    }

    public void Unsupported_builtin_types_are_rejected(TestContext t)
    {
        foreach (var type in new[] { typeof(byte), typeof(sbyte), typeof(char), typeof(ushort), typeof(uint), typeof(ulong), typeof(bool) })
        foreach (var nullable in new[] { false, true })
        {
            var parameter = SysExpr.Parameter(nullable ? typeof(Nullable<>).MakeGenericType(type) : type);
            t.Throws<InvalidOperationException>(() => SysExpr.Negate(parameter));
            t.Throws<InvalidOperationException>(() => SysExpr.NegateChecked(parameter));
        }
    }
#endif

    private static void Check(TestContext t, Type type, object value, bool byRef, MethodInfo method = null)
    {
        foreach (var node in new[] { Node.Negate, Node.NegateChecked })
        {
            var parameter = SysExpr.Parameter(byRef ? type.MakeByRefType() : type);
            var body = SysExpr.NewArrayInit(typeof(object), SysExpr.Convert(SysExpr.MakeUnary(node, parameter, null, method), typeof(object)), SysExpr.Convert(parameter, typeof(object)));
            var expression = byRef ? SysExpr.Lambda(typeof(Read<>).MakeGenericType(type), body, parameter) : SysExpr.Lambda(body, parameter);
            var fastParameter = Expr.Parameter(byRef ? type.MakeByRefType() : type);
            var fastBody = Expr.NewArrayInit(typeof(object), Expr.Convert(Expr.MakeUnary(node, fastParameter, null, method), typeof(object)), Expr.Convert(fastParameter, typeof(object)));
            var fastExpression = byRef ? Expr.Lambda(typeof(Read<>).MakeGenericType(type), fastBody, fastParameter) : Expr.Lambda(fastBody, fastParameter);
            var fast = fastExpression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            var expected = Invoke(expression.Compile(), value);
            var expectedCalls = Calls;
            Calls = 0;
            var actual = Invoke(fast, value);
            if (expected is object[] e && actual is object[] a) { t.AreEqual(e[0], a[0]); t.AreEqual(e[1], a[1]); }
            else t.AreEqual(expected, actual);
            t.AreEqual(expectedCalls, Calls);
        }
    }

    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { value }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public delegate object[] Read<T>(ref T value);
    public static int Calls;
    public static long Change(int value) { ++Calls; return value + 10L; }
    public static long? ChangeNullable(int? value) { ++Calls; return value ?? 100; }
    public struct Counter
    {
        public int Value;
        public static Counter operator -(Counter value) { ++Calls; return new Counter { Value = -value.Value }; }
    }
}
