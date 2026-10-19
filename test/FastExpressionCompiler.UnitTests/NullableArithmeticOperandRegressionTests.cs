using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NullableArithmeticOperandRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Inlined_nullable_arguments_use_values(t);
        Nullable_block_variables_use_values(t);
        Captured_nullable_arguments_use_values(t);
    }

    public void Inlined_nullable_arguments_use_values(TestContext t) => Check(t, 0);
    public void Nullable_block_variables_use_values(TestContext t) => Check(t, 1);
    public void Captured_nullable_arguments_use_values(TestContext t) => Check(t, 2);

    private static void Check(TestContext t, int shape)
    {
        Type[] types = { typeof(short), typeof(int), typeof(long), typeof(uint), typeof(ulong), typeof(float), typeof(double) };
        Node[] operations = { Node.Add, Node.Subtract, Node.Multiply, Node.Divide, Node.Modulo };
        foreach (var type in types)
        foreach (var operation in operations)
        foreach (var a in new object[] { null, Convert.ChangeType(0, type), Convert.ChangeType(3, type) })
        foreach (var b in new object[] { null, Convert.ChangeType(0, type), Convert.ChangeType(2, type) })
        {
            var nullable = typeof(Nullable<>).MakeGenericType(type);
            var left = SysExpr.Parameter(nullable);
            var right = SysExpr.Parameter(nullable);
            var body = SysExpr.MakeBinary(operation, left, right);
            var x = SysExpr.Constant(a, nullable);
            var y = SysExpr.Constant(b, nullable);
            var expression = shape == 0
                ? SysExpr.Lambda(SysExpr.Invoke(SysExpr.Lambda(body, left, right), x, y))
                : shape == 1
                    ? SysExpr.Lambda(SysExpr.Block(new[] { left, right }, SysExpr.Assign(left, x), SysExpr.Assign(right, y), body))
                    : SysExpr.Lambda(SysExpr.Lambda(body), left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var args = shape == 2 ? new[] { a, b } : Array.Empty<object>();
            t.AreEqual(Invoke(system, args, shape == 2), Invoke(fast, args, shape == 2));
        }
    }

    private static object Invoke(Delegate function, object[] arguments, bool nested)
    {
        try
        {
            var value = function.DynamicInvoke(arguments);
            return nested ? ((Delegate)value).DynamicInvoke() : value;
        }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
