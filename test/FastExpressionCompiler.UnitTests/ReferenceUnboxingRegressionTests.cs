using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReferenceUnboxingRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_parameters_unbox_values_and_nullable_values(t);
        Reference_constants_unbox_values_and_nullable_values(t);
    }

    public void Reference_parameters_unbox_values_and_nullable_values(TestContext t) => Check(t, false);
    public void Reference_constants_unbox_values_and_nullable_values(TestContext t) => Check(t, true);

    private static void Check(TestContext t, bool constant)
    {
        Type[] sources = { typeof(object), typeof(ValueType), typeof(Enum), typeof(IComparable), typeof(ITagged) };
        Type[] valuesTypes = { typeof(int), typeof(long), typeof(decimal), typeof(DateTime), typeof(DayOfWeek), typeof(Tagged) };
        object[] values = { null, 42, 42L, 42m, DateTime.MinValue, DayOfWeek.Friday, new Tagged { Text = "value" }, "text" };
        foreach (var source in sources)
        foreach (var valueType in valuesTypes)
        foreach (var target in new[] { valueType, typeof(Nullable<>).MakeGenericType(valueType) })
        foreach (var isChecked in new[] { false, true })
        foreach (var value in values)
        {
            if (value != null && !source.IsInstanceOfType(value)) continue;
            var parameter = SysExpr.Parameter(source);
            System.Linq.Expressions.UnaryExpression conversion;
            try { conversion = isChecked ? SysExpr.ConvertChecked(parameter, target) : SysExpr.Convert(parameter, target); }
            catch (InvalidOperationException) { continue; } // Only conversions accepted by System.Linq.Expressions.
            var system = SysExpr.Lambda(conversion, parameter).Compile();
            var fastParameter = Expr.Parameter(source);
            var operand = constant ? Expr.Constant(value, source) : (Expr)fastParameter;
            var body = isChecked ? Expr.ConvertChecked(operand, target) : Expr.Convert(operand, target);
            var fast = constant ? Expr.Lambda(body).CompileFast(true) : Expr.Lambda(body, fastParameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(Invoke(system, new[] { value }), Invoke(fast, constant ? Array.Empty<object>() : new[] { value }));
        }
    }

    private static object Invoke(Delegate function, object[] arguments)
    {
        try { return function.DynamicInvoke(arguments); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public interface ITagged { }
    public struct Tagged : ITagged { public string Text; }
}
