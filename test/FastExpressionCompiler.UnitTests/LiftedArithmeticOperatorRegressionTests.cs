using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct LiftedArithmeticOperatorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Lifted_decimal_operators_match_system(t);
        Lifted_custom_operators_match_system(t);
        Lifted_operator_with_different_result_type_matches_system(t);
        Non_lifted_arithmetic_with_atomic_right_and_real_finally_matches_system(t);
        Explicit_nullable_formal_operator_with_real_finally_matches_system(t);
#if LIGHT_EXPRESSION
        Intrinsic_constant_with_hidden_finally_keeps_safe_operand_emission(t);
#endif
    }

    public void Lifted_decimal_operators_match_system(TestContext t)
    {
        foreach (var operation in new[] { Node.Add, Node.AddChecked, Node.Subtract, Node.SubtractChecked,
            Node.Multiply, Node.MultiplyChecked, Node.Divide, Node.Modulo })
            Check(t, typeof(decimal?), operation, new object[] { null, 0m, 1m, -1m, decimal.MinValue, decimal.MaxValue });
    }

    public void Lifted_custom_operators_match_system(TestContext t)
    {
        foreach (var operation in new[] { Node.Add, Node.Multiply })
            Check(t, typeof(Measure?), operation, new object[] { null, new Measure { Value = 0 }, new Measure { Value = 3 } });
    }

    public void Lifted_operator_with_different_result_type_matches_system(TestContext t) =>
        Check(t, typeof(DateTime?), Node.Subtract, new object[] { null, DateTime.MinValue, DateTime.MaxValue });

    public void Non_lifted_arithmetic_with_atomic_right_and_real_finally_matches_system(TestContext t)
    {
        CheckFinallyOperands(t, typeof(decimal), null, new object[] { 0m, 7m, decimal.MaxValue });
        CheckFinallyOperands(t, typeof(Measure), null, new object[] { new Measure { Value = 0 }, new Measure { Value = 3 } });
        CheckFinallyOperands(t, typeof(Measure), typeof(LiftedArithmeticOperatorRegressionTests).GetMethod(nameof(CombineMeasures)),
            new object[] { new Measure { Value = 2 }, new Measure { Value = 3 } });
    }

    public void Explicit_nullable_formal_operator_with_real_finally_matches_system(TestContext t) =>
        CheckFinallyOperands(t, typeof(decimal?), typeof(LiftedArithmeticOperatorRegressionTests).GetMethod(nameof(CombineNullable)),
            new object[] { null, 0m, 7m });

    public static Measure CombineMeasures(Measure left, Measure right) => new Measure { Value = left.Value * 10 + right.Value };
    public static decimal? CombineNullable(decimal? left, decimal? right) => (left ?? 10m) + (right ?? 20m);

    private static void CheckFinallyOperands(TestContext t, Type type, MethodInfo method, object[] values)
    {
        foreach (var finallyOnRight in new[] { false, true })
        {
            var trace = "";
            Action markLeft = () => trace += "left;";
            Action markRight = () => trace += "right;";
            Action markFinally = () => trace += "finally;";
            var left = SysExpr.Parameter(type, "left");
            var right = SysExpr.Parameter(type, "right");
            System.Linq.Expressions.Expression leftOperand = SysExpr.Block(SysExpr.Invoke(SysExpr.Constant(markLeft)), left);
            System.Linq.Expressions.Expression rightOperand = right;
            if (finallyOnRight)
                rightOperand = SysExpr.TryFinally(SysExpr.Block(SysExpr.Invoke(SysExpr.Constant(markRight)), right), SysExpr.Invoke(SysExpr.Constant(markFinally)));
            else
                leftOperand = SysExpr.TryFinally(leftOperand, SysExpr.Invoke(SysExpr.Constant(markFinally)));
            var binary = SysExpr.Add(leftOperand, rightOperand, method);
            var expression = SysExpr.Lambda(binary, left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in values)
            foreach (var b in values)
            {
                trace = "";
                var expected = Invoke(system, a, b);
                var expectedTrace = trace;
                trace = "";
                t.AreEqual(expected, Invoke(fast, a, b));
                t.AreEqual(expectedTrace, trace);
            }
        }
    }

#if LIGHT_EXPRESSION
    public void Intrinsic_constant_with_hidden_finally_keeps_safe_operand_emission(TestContext t)
    {
        var count = 0;
        Action mark = () => ++count;
        var system = SysExpr.Lambda<Func<decimal>>(SysExpr.Add(SysExpr.Constant(7m),
            SysExpr.TryFinally(SysExpr.Constant(11m), SysExpr.Invoke(SysExpr.Constant(mark))))).Compile();
        var expected = system();
        var expectedCount = count;
        count = 0;
        var expression = Expression.Lambda<Func<decimal>>(Expression.Add(Expression.Constant(7m), new HiddenFinallyConstant(mark)));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(expected, fast());
        t.AreEqual(expectedCount, count);
    }

    private sealed class HiddenFinallyConstant : ConstantExpression
    {
        private readonly Expression _operand;
        public HiddenFinallyConstant(Action mark) => _operand = Expression.TryFinally(Expression.Constant(11m), Expression.Invoke(Expression.Constant(mark)));
        public override Type Type => typeof(decimal);
        public override object Value => throw new InvalidOperationException("An intrinsic constant must dispatch its emitter.");
        public override bool IsIntrinsic => true;
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda,
            ref ImTools.SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas) =>
            ExpressionCompiler.TryCollectInfo(ref context, _operand, nestedLambda, ref rootNestedLambdas);
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, System.Reflection.Emit.ILGenerator il,
            ExpressionCompiler.ParentFlags parent, int byRefIndex = -1) =>
            ExpressionCompiler.EmittingVisitor.TryEmit(_operand, il, ref context, parent, byRefIndex);
    }
#endif

    private static void Check(TestContext t, Type type, Node operation, object[] values)
    {
        var left = SysExpr.Parameter(type);
        var right = SysExpr.Parameter(type);
        var expression = SysExpr.Lambda(SysExpr.MakeBinary(operation, left, right), left, right);
        var system = expression.Compile();
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var a in values)
        foreach (var b in values) t.AreEqual(Invoke(system, a, b), Invoke(fast, a, b));
    }

    private static object Invoke(Delegate function, object a, object b)
    {
        try { return function.DynamicInvoke(new[] { a, b }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public struct Measure
    {
        public int Value;
        public static Measure operator +(Measure a, Measure b) => new Measure { Value = a.Value + b.Value };
        public static Measure operator *(Measure a, Measure b) => new Measure { Value = a.Value * b.Value };
    }
}
