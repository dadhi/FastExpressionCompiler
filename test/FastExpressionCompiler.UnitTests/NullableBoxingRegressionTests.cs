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

public struct NullableBoxingRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nullable_parameters_box_to_reference_types(t);
        Nullable_constants_box_to_reference_types(t);
        Nullable_locals_box_to_reference_types(t);
        User_defined_conversion_to_reference_type_is_preserved(t);
    }

    public void Nullable_parameters_box_to_reference_types(TestContext t) => Check(t, 0);
    public void Nullable_constants_box_to_reference_types(TestContext t) => Check(t, 1);
    public void Nullable_locals_box_to_reference_types(TestContext t) => Check(t, 2);

    public void User_defined_conversion_to_reference_type_is_preserved(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(Convertible));
        var fast = Expr.Lambda<Func<Convertible, Box>>(Expr.Convert(parameter, typeof(Box)), parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(42, fast(new Convertible { Value = 42 }).Value);
    }

    private static void Check(TestContext t, int shape)
    {
        Type[] types = { typeof(int), typeof(DayOfWeek), typeof(DateTime), typeof(Tagged) };
        object[] values = { 42, DayOfWeek.Friday, DateTime.MinValue, new Tagged { Text = "value" } };
        Type[] targets = { typeof(object), typeof(ValueType), typeof(Enum), typeof(IComparable), typeof(ITagged) };
        for (var i = 0; i < types.Length; ++i)
        foreach (var target in targets)
        foreach (var value in new[] { null, values[i] })
        foreach (var isChecked in new[] { false, true })
        {
            var nullable = typeof(Nullable<>).MakeGenericType(types[i]);
            var parameter = SysExpr.Parameter(nullable);
            System.Linq.Expressions.UnaryExpression conversion;
            try { conversion = isChecked ? SysExpr.ConvertChecked(parameter, target) : SysExpr.Convert(parameter, target); }
            catch (InvalidOperationException) { continue; }
            var constant = SysExpr.Constant(value, nullable);
            var body = shape == 1 ? conversion.Update(constant)
                : shape == 2 ? (SysExpr)SysExpr.Block(new[] { parameter }, SysExpr.Assign(parameter, constant), conversion)
                : conversion;
            var expression = shape == 0 ? SysExpr.Lambda(body, parameter) : SysExpr.Lambda(body);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var args = shape == 0 ? new[] { value } : Array.Empty<object>();
            t.AreEqual(Invoke(expression.Compile(), args), Invoke(fast, args));
        }
    }

    private static object Invoke(Delegate function, object[] arguments)
    {
        try { return function.DynamicInvoke(arguments); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public interface ITagged { }
    public struct Tagged : ITagged { public string Text; }
    public class Box { public int Value; }
    public struct Convertible
    {
        public int Value;
        public static implicit operator Box(Convertible value) => new Box { Value = value.Value };
    }
}
