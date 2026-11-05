using System;
using System.Linq;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CheckedFloatingArithmeticRegressionTests : ITestX
{
    public void Run(TestRun t) => Checked_floating_arithmetic_matches_system(t);

    public void Checked_floating_arithmetic_matches_system(TestContext t)
    {
        foreach (var type in new[] { typeof(float), typeof(double) })
        foreach (var nullable in new[] { false, true })
        {
            var operandType = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
            var values = new object[] { Convert.ChangeType(0, type), Convert.ChangeType(1, type),
                type.GetField("MaxValue").GetValue(null), type.GetField("MinValue").GetValue(null),
                type.GetField("NaN").GetValue(null), type.GetField("PositiveInfinity").GetValue(null) };
            if (nullable) values = new[] { (object)null }.Concat(values).ToArray();
            foreach (var operation in new[] { Node.AddChecked, Node.SubtractChecked, Node.MultiplyChecked })
            {
                var p = SysExpr.Parameter(operandType);
                var q = SysExpr.Parameter(operandType);
                var expression = SysExpr.Lambda(SysExpr.MakeBinary(operation, p, q), p, q);
                var system = expression.Compile();
                var fast = expression.FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                foreach (var left in values)
                foreach (var right in values)
                    t.AreEqual(Invoke(system, new[] { left, right }), Invoke(fast, new[] { left, right }));
            }
        }
    }

    private static object Invoke(Delegate function, object[] values)
    {
        try { return function.DynamicInvoke(values); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
