using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ExplicitComparisonMethodRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Closed_generic_comparison_methods_keep_their_return_type(t);
        Negating_explicit_equality_calls_the_supplied_method(t);
        Lifted_custom_equality_keeps_null_semantics(t);
        Value_type_comparisons_consume_unbox_values(t);
    }

    public static class Operations<T>
    {
        public static int Add(int left, int right) => unchecked(left + right);
        public static bool Greater(int left, int right) => left > right;
    }

    public void Value_type_comparisons_consume_unbox_values(TestContext t)
    {
        foreach (var type in new[] { typeof(decimal), typeof(DateTime) })
        {
            object value = type == typeof(decimal) ? (object)13m : DateTime.MinValue;
            object different = type == typeof(decimal) ? (object)14m : DateTime.MinValue.AddDays(1);
            var boxed = SysExpr.Parameter(typeof(object));
            var comparison = SysExpr.Equal(SysExpr.Constant(value, type), SysExpr.Unbox(boxed, type));
            var expression = SysExpr.Lambda<Func<object, bool>>(comparison, boxed);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            var ignored = SysExpr.Lambda<Action<object>>(SysExpr.Block(comparison, SysExpr.Empty()), boxed).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
            t.AreEqual(system(value), fast(value));
            t.AreEqual(system(different), fast(different));
            t.Throws<InvalidCastException>(() => ignored(new object()));
        }
    }

    public void Closed_generic_comparison_methods_keep_their_return_type(TestContext t)
    {
        var method = typeof(Operations<int>).GetMethod(nameof(Operations<int>.Add));
        foreach (var kind in new[] { ExpressionType.Equal, ExpressionType.NotEqual, ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual })
        {
            var parameter = SysExpr.Parameter(typeof(int));
            var body = SysExpr.MakeBinary(kind, parameter, SysExpr.Constant(2), false, method);
            var expression = SysExpr.Lambda<Func<int, int>>(body, parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            var ignored = SysExpr.Lambda<Action<int>>(SysExpr.Block(body, SysExpr.Empty()), parameter).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
            var expected = expression.Compile();
            foreach (var value in new[] { int.MinValue, -1, 0, 3, int.MaxValue })
            {
                t.AreEqual(unchecked(value + 2), fast(value));
                t.AreEqual(expected(value), fast(value));
                ignored(value);
            }
            var constantExpression = SysExpr.Lambda<Func<int>>(SysExpr.MakeBinary(kind, SysExpr.Constant(3), SysExpr.Constant(2), false, method));
            var constantFast = constantExpression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(constantFast)) return;
            t.AreEqual(5, constantFast());
        }
    }

    public void Negating_explicit_equality_calls_the_supplied_method(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(int));
        var equality = SysExpr.MakeBinary(ExpressionType.Equal, parameter, SysExpr.Constant(2), false,
            typeof(Operations<int>).GetMethod(nameof(Operations<int>.Greater)));
        var expression = SysExpr.Lambda<Func<int, bool>>(SysExpr.Not(equality), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var expected = expression.Compile();
        foreach (var value in new[] { -1, 2, 3 })
        {
            t.AreEqual(!(value > 2), fast(value));
            t.AreEqual(expected(value), fast(value));
        }
    }

    public struct Number
    {
        public int Value;
        public override bool Equals(object other) => other is Number n && Value == n.Value;
        public override int GetHashCode() => Value;
        public static bool operator ==(Number left, Number right) => left.Value == right.Value;
        public static bool operator !=(Number left, Number right) => left.Value != right.Value;
    }

    public void Lifted_custom_equality_keeps_null_semantics(TestContext t)
    {
        foreach (var kind in new[] { ExpressionType.Equal, ExpressionType.NotEqual })
        foreach (var liftToNull in new[] { false, true })
        {
            var left = SysExpr.Parameter(typeof(Number?));
            var right = SysExpr.Parameter(typeof(Number?));
            var method = typeof(Number).GetMethod(kind == ExpressionType.Equal ? "op_Equality" : "op_Inequality");
            var expression = SysExpr.Lambda(SysExpr.MakeBinary(kind, left, right, liftToNull, method), left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var expected = expression.Compile();
            foreach (var leftValue in new object[] { null, new Number { Value = 1 }, new Number { Value = 2 } })
            foreach (var rightValue in new object[] { null, new Number { Value = 1 }, new Number { Value = 2 } })
                t.AreEqual(expected.DynamicInvoke(leftValue, rightValue), fast.DynamicInvoke(leftValue, rightValue));
        }
    }
}
