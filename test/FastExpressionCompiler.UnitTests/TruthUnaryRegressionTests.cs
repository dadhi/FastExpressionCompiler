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

public struct TruthUnaryRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Boolean_truth_operators_preserve_nullable_null(t);
        Reference_truth_operators_are_called(t);
        Lifted_struct_truth_operators_are_called(t);
        Ignored_truth_results_preserve_effects(t);
        Ref_boolean_operands_are_read_as_values(t);
#if !LIGHT_EXPRESSION
        Explicit_truth_methods_are_called(t);
#endif
    }

    public void Boolean_truth_operators_preserve_nullable_null(TestContext t)
    {
        foreach (var value in new object[] { false, true }) { Check(t, typeof(bool), value); Check(t, typeof(bool?), value); }
        Check(t, typeof(bool?), null);
    }
    public void Reference_truth_operators_are_called(TestContext t)
    {
        Check(t, typeof(Truth), new Truth { Value = false });
        Check(t, typeof(Truth), new Truth { Value = true });
    }
    public void Lifted_struct_truth_operators_are_called(TestContext t)
    {
        foreach (var value in new[] { new ValueTruth { Value = false }, new ValueTruth { Value = true } })
        { Check(t, typeof(ValueTruth), value); Check(t, typeof(ValueTruth?), value); }
        Check(t, typeof(ValueTruth?), null);
    }
    public void Ref_boolean_operands_are_read_as_values(TestContext t)
    {
        Check(t, typeof(bool), false, true);
        Check(t, typeof(bool?), true, true);
        Check(t, typeof(bool?), null, true);
    }
    public void Ignored_truth_results_preserve_effects(TestContext t)
    {
        foreach (var operation in new[] { Node.IsTrue, Node.IsFalse })
        {
            var body = Expr.Block(Expr.MakeUnary(operation, Expr.Constant(new Truth { Value = true }), null), Expr.Constant(7));
            var fast = Expr.Lambda<Func<int>>(body).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            t.AreEqual(7, fast());
            t.AreEqual(1, Calls);
        }
    }
#if !LIGHT_EXPRESSION
    public void Explicit_truth_methods_are_called(TestContext t)
    {
        Check(t, typeof(Truth), new Truth { Value = false }, false, true);
        Check(t, typeof(Truth), new Truth { Value = true }, false, true);
        Check(t, typeof(bool), false, false, true);
    }
#endif
    private static void Check(TestContext t, Type type, object value, bool byRef = false, bool explicitMethod = false)
    {
        foreach (var operation in new[] { Node.IsTrue, Node.IsFalse })
        {
            var method = explicitMethod ? type == typeof(bool) ? typeof(TruthUnaryRegressionTests).GetMethod(nameof(Identity))
                : type.GetMethod(operation == Node.IsTrue ? "op_True" : "op_False") : null;
            var parameter = SysExpr.Parameter(byRef ? type.MakeByRefType() : type);
            var body = SysExpr.Convert(SysExpr.MakeUnary(operation, parameter, null, method), typeof(object));
            var expression = byRef ? SysExpr.Lambda(typeof(Read<>).MakeGenericType(type), body, parameter) : SysExpr.Lambda(body, parameter);
            var fp = Expr.Parameter(byRef ? type.MakeByRefType() : type);
            var fb = Expr.Convert(Expr.MakeUnary(operation, fp, null, method), typeof(object));
            var fe = byRef ? Expr.Lambda(typeof(Read<>).MakeGenericType(type), fb, fp) : Expr.Lambda(fb, fp);
            var fast = fe.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Calls = 0;
            var expected = expression.Compile().DynamicInvoke(new[] { value });
            var expectedCalls = Calls;
            Calls = 0;
            t.AreEqual(expected, fast.DynamicInvoke(new[] { value }));
            t.AreEqual(expectedCalls, Calls);
        }
    }
    public static int Calls;
    public static bool Identity(bool value) { ++Calls; return value; }
    public delegate object Read<T>(ref T value);
    public class Truth
    {
        public bool Value;
        public static bool operator true(Truth value) { ++Calls; return value.Value; }
        public static bool operator false(Truth value) { ++Calls; return !value.Value; }
    }
    public struct ValueTruth
    {
        public bool Value;
        public static bool operator true(ValueTruth value) { ++Calls; return value.Value; }
        public static bool operator false(ValueTruth value) { ++Calls; return !value.Value; }
    }
}
