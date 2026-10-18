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

public struct SmallCheckedArithmeticRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Checked_small_parameter_arithmetic_preserves_overflow(t);
        Checked_small_constant_arithmetic_preserves_overflow(t);
        Checked_nullable_small_arithmetic_preserves_overflow_and_null(t);
        Checked_floating_arithmetic_matches_system(t);
    }
    public void Checked_small_parameter_arithmetic_preserves_overflow(TestContext t) => Check(t, false, false);
    public void Checked_small_constant_arithmetic_preserves_overflow(TestContext t) => Check(t, false, true);
    public void Checked_nullable_small_arithmetic_preserves_overflow_and_null(TestContext t) { Check(t, true, false); Check(t, true, true); }

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

    private static void Check(TestContext t, bool nullable, bool constants)
    {
        foreach (var type in new[] { typeof(short), typeof(ushort) })
        {
            var values = nullable ? new object[] { null, Convert.ChangeType(0, type), Convert.ChangeType(1, type), type.GetField("MinValue").GetValue(null), type.GetField("MaxValue").GetValue(null) }
                : new object[] { Convert.ChangeType(0, type), Convert.ChangeType(1, type), type.GetField("MinValue").GetValue(null), type.GetField("MaxValue").GetValue(null) };
            var exprType = nullable ? typeof(Nullable<>).MakeGenericType(type) : type;
            foreach (var operation in new[] { Node.AddChecked, Node.SubtractChecked, Node.MultiplyChecked })
            foreach (var a in values)
            foreach (var b in values)
            {
                var p = SysExpr.Parameter(exprType);
                var q = SysExpr.Parameter(exprType);
                var body = SysExpr.MakeBinary(operation, constants ? SysExpr.Constant(a, exprType) : p, constants ? SysExpr.Constant(b, exprType) : q);
                var expression = constants ? SysExpr.Lambda(body) : SysExpr.Lambda(body, p, q);
                var fast = expression.FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                var arguments = constants ? Array.Empty<object>() : new[] { a, b };
                t.AreEqual(Invoke(expression.Compile(), arguments), Invoke(fast, arguments));
            }
        }
    }
    private static object Invoke(Delegate function, object[] values)
    {
        try { return function.DynamicInvoke(values); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
