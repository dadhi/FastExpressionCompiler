using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

[RunTest]
public struct ComparisonReductionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Conditional_negated_explicit_methods_keep_their_semantics(t);
        Conditional_negated_reference_equality_keeps_identity(t);
        Equality_and_inequality_operators_need_not_be_inverses(t);
        Explicit_zero_comparisons_keep_their_method(t);
        Nullable_equality_keeps_null_semantics(t);
    }

    public static int MethodCalls;
    public static bool Greater(int left, int right) { ++MethodCalls; return left > right; }

    public void Conditional_negated_explicit_methods_keep_their_semantics(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(int));
        foreach (var constantLeft in new[] { false, true })
        {
            var equality = SysExpr.Equal(constantLeft ? SysExpr.Constant(3) : parameter, SysExpr.Constant(2), false,
                typeof(ComparisonReductionRegressionTests).GetMethod(nameof(Greater)));
            var expression = SysExpr.Lambda<Func<int, int>>(SysExpr.Condition(SysExpr.Not(equality), SysExpr.Constant(11), SysExpr.Constant(22)), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            MethodCalls = 0;
            t.AreEqual(22, fast(3));
            t.AreEqual(1, MethodCalls);
            t.AreEqual(constantLeft ? 22 : 11, fast(1));
            t.AreEqual(2, MethodCalls);
        }
    }

    public void Explicit_zero_comparisons_keep_their_method(TestContext t)
    {
        var constants = SysExpr.Lambda<Func<int>>(SysExpr.Condition(
            SysExpr.Equal(SysExpr.Constant(3), SysExpr.Constant(2), false,
                typeof(ComparisonReductionRegressionTests).GetMethod(nameof(Greater))),
            SysExpr.Constant(11), SysExpr.Constant(22))).FromSysExpression().CompileFast(true);
        if (t.IsNotNull(constants))
        {
            MethodCalls = 0;
            t.AreEqual(11, constants());
            t.AreEqual(1, MethodCalls);
        }
        var parameter = SysExpr.Parameter(typeof(int));
        var equality = SysExpr.Equal(parameter, SysExpr.Constant(0), false,
            typeof(ComparisonReductionRegressionTests).GetMethod(nameof(Greater)));
        var source = SysExpr.Lambda<Func<int, int>>(
            SysExpr.Condition(equality, SysExpr.Constant(11), SysExpr.Constant(22)), parameter);
        var system = source.Compile();
        var fast = source.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in new[] { -1, 0, 1 })
        {
            var expected = system(value);
            MethodCalls = 0;
            t.AreEqual(expected, fast(value));
            t.AreEqual(1, MethodCalls);
        }
    }

    public void Nullable_equality_keeps_null_semantics(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?));
        var right = SysExpr.Parameter(typeof(int?));
        foreach (var inequality in new[] { false, true })
        {
            var comparison = inequality ? SysExpr.NotEqual(left, right) : SysExpr.Equal(left, right);
            var source = SysExpr.Lambda<Func<int?, int?, bool>>(SysExpr.Not(comparison), left, right);
            var system = source.Compile();
            var fast = source.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var l in new int?[] { null, 1, 2 })
            foreach (var r in new int?[] { null, 1, 2 })
                t.AreEqual(system(l, r), fast(l, r));
        }
    }

    public sealed class UnusualEquality
    {
        public static int EqualCalls, NotEqualCalls;
        public static bool operator ==(UnusualEquality left, UnusualEquality right) { ++EqualCalls; return false; }
        public static bool operator !=(UnusualEquality left, UnusualEquality right) { ++NotEqualCalls; return false; }
        public override bool Equals(object value) => false;
        public override int GetHashCode() => 0;
    }

    public void Conditional_negated_reference_equality_keeps_identity(TestContext t)
    {
        var left = Expr.Parameter(typeof(UnusualEquality));
        var right = Expr.Parameter(typeof(UnusualEquality));
        var condition = Expr.Not(Expr.ReferenceEqual(left, right));
        var fast = Expr.Lambda<Func<UnusualEquality, UnusualEquality, int>>(
            Expr.Condition(condition, Expr.Constant(11), Expr.Constant(22)), left, right).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var same = new UnusualEquality();
        UnusualEquality.EqualCalls = UnusualEquality.NotEqualCalls = 0;
        t.AreEqual(11, fast(same, new UnusualEquality()));
        t.AreEqual(22, fast(same, same));
        t.AreEqual(0, UnusualEquality.EqualCalls);
        t.AreEqual(0, UnusualEquality.NotEqualCalls);
    }

    public void Equality_and_inequality_operators_need_not_be_inverses(TestContext t)
    {
        var left = Expr.Parameter(typeof(UnusualEquality));
        var right = Expr.Parameter(typeof(UnusualEquality));
        var notEqual = Expr.Not(Expr.Equal(left, right));
        var direct = Expr.Lambda<Func<UnusualEquality, UnusualEquality, bool>>(notEqual, left, right).CompileFast(true);
        var conditional = Expr.Lambda<Func<UnusualEquality, UnusualEquality, int>>(
            Expr.Condition(notEqual, Expr.Constant(11), Expr.Constant(22)), left, right).CompileFast(true);
        if (!t.IsNotNull(direct) || !t.IsNotNull(conditional)) return;
        var value = new UnusualEquality();
        UnusualEquality.EqualCalls = UnusualEquality.NotEqualCalls = 0;
        t.IsTrue(direct(value, value));
        t.AreEqual(11, conditional(value, value));
        t.AreEqual(2, UnusualEquality.EqualCalls);
        t.AreEqual(0, UnusualEquality.NotEqualCalls);
    }
}
