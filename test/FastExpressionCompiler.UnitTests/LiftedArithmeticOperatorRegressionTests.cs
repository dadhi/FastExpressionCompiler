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
