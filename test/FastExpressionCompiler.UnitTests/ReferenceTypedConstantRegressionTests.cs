using System;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReferenceTypedConstantRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_typed_values_return_boxed_objects(t);
        Reference_typed_values_support_type_tests(t);
        Interface_calls_receive_boxed_constants(t);
        Repeated_closure_constants_keep_reference_stack_type(t);
    }

    private static readonly object[] Values = { 42, DayOfWeek.Friday, DateTime.MinValue, new Tagged { Text = "value" } };

    public void Reference_typed_values_return_boxed_objects(TestContext t)
    {
        foreach (var value in Values)
        foreach (var type in ReferenceTypes(value))
        {
            var fast = Expr.Lambda<Func<object>>(Expr.Constant(value, type)).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(value, fast());
        }
    }

    public void Reference_typed_values_support_type_tests(TestContext t)
    {
        foreach (var value in Values)
        foreach (var type in ReferenceTypes(value))
        {
            var constant = Expr.Constant(value, type);
            var typeTest = Expr.Lambda<Func<bool>>(Expr.TypeIs(constant, value.GetType())).CompileFast(true);
            if (!t.IsNotNull(typeTest)) return;
            t.IsTrue(typeTest());
        }
    }

    public void Interface_calls_receive_boxed_constants(TestContext t)
    {
        var constant = Expr.Constant(42, typeof(IComparable));
        var call = Expr.Call(constant, typeof(IComparable).GetMethod("CompareTo"), Expr.Constant(41, typeof(object)));
        var fast = Expr.Lambda<Func<int>>(call).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(1, fast());
    }

    public void Repeated_closure_constants_keep_reference_stack_type(TestContext t)
    {
        var value = new Tagged { Text = "shared" };
        var constant = Expr.Constant(value, typeof(ITagged));
        var fast = Expr.Lambda<Func<object[]>>(Expr.NewArrayInit(typeof(object), constant, constant, constant)).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var item in fast()) t.AreEqual((object)value, item);
    }

    private static Type[] ReferenceTypes(object value) => value is Enum
        ? new[] { typeof(object), typeof(ValueType), typeof(Enum), typeof(IComparable) }
        : value is IComparable
            ? new[] { typeof(object), typeof(ValueType), typeof(IComparable) }
            : new[] { typeof(object), typeof(ValueType), typeof(ITagged) };

    public interface ITagged { }
    public struct Tagged : ITagged { public string Text; }
}
