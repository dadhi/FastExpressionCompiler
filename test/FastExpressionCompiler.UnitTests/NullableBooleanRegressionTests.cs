using System;
using SysExpr = System.Linq.Expressions.Expression;
using SysExpressionType = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NullableBooleanRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nullable_not_truth_table(t);
        Factory_nodes_preserve_nullable_type(t);
        Conditional_constants_compile_without_fallback(t);
        Nullable_logical_truth_tables(t);
        Short_circuit_evaluates_operands_once(t);
        Ignored_results_keep_short_circuit_effects(t);
        Nullable_results_support_member_access(t);
        Short_circuit_skips_throwing_right_operand(t);
        Long_right_operand_uses_valid_branches(t);
    }

    private static readonly bool?[] Values = { null, false, true };

    public void Nullable_not_truth_table(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(bool?));
        var expression = SysExpr.Lambda<Func<bool?, bool?>>(SysExpr.Not(parameter), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in Values) t.AreEqual(!value, fast(value));
        var ignored = SysExpr.Lambda<Action<bool?>>(SysExpr.Block(SysExpr.Not(parameter), SysExpr.Empty()), parameter)
            .FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(ignored)) return;
        foreach (var value in Values) ignored(value);
    }

    public void Factory_nodes_preserve_nullable_type(TestContext t)
    {
        var left = Expr.Parameter(typeof(bool?));
        var right = Expr.Parameter(typeof(bool?));
        foreach (var body in new[] { Expr.AndAlso(left, right), Expr.OrElse(left, right), Expr.And(left, right), Expr.Or(left, right) })
        {
            t.AreEqual(typeof(bool?), body.Type);
            var fast = Expr.Lambda<Func<bool?, bool?, bool?>>(body, left, right).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in Values)
            foreach (var b in Values)
                t.AreEqual(body.NodeType is SysExpressionType.AndAlso or SysExpressionType.And ? a & b : a | b, fast(a, b));
        }
    }

    public void Conditional_constants_compile_without_fallback(TestContext t)
    {
        foreach (var a in Values)
        foreach (var b in Values)
        {
            var left = Expr.Condition(Expr.Equal(Expr.Constant(true), Expr.Constant(true)),
                Expr.Constant(a, typeof(bool?)), Expr.Constant(null, typeof(bool?)));
            var right = Expr.Condition(Expr.Equal(Expr.Constant(true), Expr.Constant(true)),
                Expr.Constant(b, typeof(bool?)), Expr.Constant(null, typeof(bool?)));
            foreach (var body in new[] { Expr.AndAlso(left, right), Expr.OrElse(left, right) })
            {
                var fast = Expr.Lambda<Func<object>>(Expr.Convert(body, typeof(object))).CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                t.AreEqual<object>(body.NodeType == SysExpressionType.AndAlso ? a & b : a | b, fast());
            }
        }
    }

    public void Nullable_logical_truth_tables(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(bool?));
        var right = SysExpr.Parameter(typeof(bool?));
        foreach (var node in new[] { SysExpressionType.AndAlso, SysExpressionType.OrElse, SysExpressionType.And, SysExpressionType.Or })
        {
            var expression = SysExpr.Lambda<Func<bool?, bool?, bool?>>(SysExpr.MakeBinary(node, left, right), left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in Values)
            foreach (var b in Values) t.AreEqual(system(a, b), fast(a, b));
        }
    }

    public void Short_circuit_evaluates_operands_once(TestContext t) => CheckSideEffects(t, false);
    public void Ignored_results_keep_short_circuit_effects(TestContext t) => CheckSideEffects(t, true);

    private static void CheckSideEffects(TestContext t, bool ignoreResult)
    {
        var left = SysExpr.Parameter(typeof(Func<bool?>));
        var right = SysExpr.Parameter(typeof(Func<bool?>));
        foreach (var node in new[] { SysExpressionType.AndAlso, SysExpressionType.OrElse, SysExpressionType.And, SysExpressionType.Or })
        {
            var body = SysExpr.MakeBinary(node, SysExpr.Invoke(left), SysExpr.Invoke(right));
            var fast = ignoreResult
                ? (Delegate)SysExpr.Lambda<Action<Func<bool?>, Func<bool?>>>(SysExpr.Block(body, SysExpr.Empty()), left, right).FromSysExpression().CompileFast(true)
                : SysExpr.Lambda<Func<Func<bool?>, Func<bool?>, bool?>>(body, left, right).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in Values)
            foreach (var b in Values)
            {
                var leftCalls = 0;
                var rightCalls = 0;
                Func<bool?> getLeft = () => { ++leftCalls; return a; };
                Func<bool?> getRight = () => { ++rightCalls; return b; };
                if (ignoreResult) ((Action<Func<bool?>, Func<bool?>>)fast)(getLeft, getRight);
                else t.AreEqual(node is SysExpressionType.AndAlso or SysExpressionType.And ? a & b : a | b,
                    ((Func<Func<bool?>, Func<bool?>, bool?>)fast)(getLeft, getRight));
                t.AreEqual(1, leftCalls);
                t.AreEqual((node == SysExpressionType.AndAlso && a == false) || (node == SysExpressionType.OrElse && a == true) ? 0 : 1, rightCalls);
            }
        }
    }

    public void Nullable_results_support_member_access(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(bool?));
        var right = SysExpr.Parameter(typeof(bool?));
        foreach (var body in new[] { SysExpr.Not(left), (SysExpr)SysExpr.AndAlso(left, right), SysExpr.OrElse(left, right) })
        {
            var expression = SysExpr.Lambda<Func<bool?, bool?, bool>>(SysExpr.Property(body, "HasValue"), left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in Values)
            foreach (var b in Values) t.AreEqual(system(a, b), fast(a, b));
        }
    }

    public void Short_circuit_skips_throwing_right_operand(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(bool?));
        var right = SysExpr.Parameter(typeof(Func<bool?>));
        foreach (var node in new[] { SysExpressionType.AndAlso, SysExpressionType.OrElse })
        {
            var expression = SysExpr.Lambda<Func<bool?, Func<bool?>, bool?>>(SysExpr.MakeBinary(node, left, SysExpr.Invoke(right)), left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Func<bool?> fail = () => throw new InvalidOperationException();
            var shortCircuit = node == SysExpressionType.OrElse;
            t.AreEqual((bool?)shortCircuit, fast(shortCircuit, fail));
            t.Throws<InvalidOperationException>(() => fast(null, fail));
            t.Throws<InvalidOperationException>(() => fast(!shortCircuit, fail));
        }
    }

    public void Long_right_operand_uses_valid_branches(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(bool?));
        var right = SysExpr.Parameter(typeof(Func<bool?>));
        var steps = new SysExpr[100];
        for (var i = 0; i < steps.Length; ++i) steps[i] = SysExpr.Invoke(right);
        var body = SysExpr.AndAlso(left, SysExpr.Block(steps));
        var fast = SysExpr.Lambda<Func<bool?, Func<bool?>, bool?>>(body, left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        Func<bool?> getRight = () => { ++calls; return true; };
        t.AreEqual((bool?)false, fast(false, getRight));
        t.AreEqual(0, calls);
        t.AreEqual((bool?)null, fast(null, getRight));
        t.AreEqual(100, calls);
    }
}
