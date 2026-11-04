using System;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct BoxedConstantIdentityRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_typed_constants_return_the_original_box(t);
        Repeated_constants_preserve_box_identity(t);
        Value_and_reference_constants_can_share_the_same_box(t);
        Nested_lambdas_preserve_box_identity(t);
    }

    private static readonly object[] Values = { 42, true, 1.25m, DayOfWeek.Friday, DateTime.MinValue, new MutableValue { Number = 42 } };

    public struct MutableValue { public int Number; }

    private static Type[] ReferenceTypes(object value) => value is Enum
        ? new[] { typeof(object), typeof(ValueType), typeof(Enum), typeof(IComparable) }
        : value is IComparable
            ? new[] { typeof(object), typeof(ValueType), typeof(IComparable) }
            : new[] { typeof(object), typeof(ValueType) };

    public void Reference_typed_constants_return_the_original_box(TestContext t)
    {
        foreach (var value in Values)
        foreach (var type in ReferenceTypes(value))
        {
            var fast = Expr.Lambda<Func<object>>(Expr.Constant(value, type)).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.IsTrue(ReferenceEquals(value, fast()));
            t.IsTrue(ReferenceEquals(fast(), fast()));
        }
    }

    public void Repeated_constants_preserve_box_identity(TestContext t)
    {
        foreach (var value in Values)
        foreach (var type in ReferenceTypes(value))
        {
            var constant = Expr.Constant(value, type);
            var fast = Expr.Lambda<Func<object[]>>(Expr.NewArrayInit(typeof(object), constant, constant, constant)).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var item in fast()) t.IsTrue(ReferenceEquals(value, item));
            var same = Expr.Lambda<Func<bool>>(Expr.ReferenceEqual(constant, constant)).CompileFast(true);
            if (!t.IsNotNull(same)) return;
            t.IsTrue(same());
        }
    }

    public void Value_and_reference_constants_can_share_the_same_box(TestContext t)
    {
        foreach (var value in Values)
        {
            var valueConstant = Expr.Constant(value, value.GetType());
            var referenceConstant = Expr.Constant(value, typeof(object));
            var fast = Expr.Lambda<Func<object[]>>(Expr.NewArrayInit(typeof(object),
                Expr.Convert(valueConstant, typeof(object)), referenceConstant, referenceConstant,
                Expr.Convert(valueConstant, typeof(object)))).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var items = fast();
            t.AreEqual(value, items[0]);
            t.AreEqual(value, items[3]);
            t.IsTrue(ReferenceEquals(value, items[1]));
            t.IsTrue(ReferenceEquals(value, items[2]));
        }
    }

    public void Nested_lambdas_preserve_box_identity(TestContext t)
    {
        foreach (var value in Values)
        {
            var inner = Expr.Lambda<Func<object>>(Expr.Constant(value, typeof(object)));
            var fast = Expr.Lambda<Func<Func<object>>>(inner).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.IsTrue(ReferenceEquals(value, fast()()));
        }
    }
}
