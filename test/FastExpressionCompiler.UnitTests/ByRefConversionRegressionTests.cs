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

public struct ByRefConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        By_ref_values_box_inside_array_initializers(t);
        By_ref_nullable_values_box_inside_array_initializers(t);
    }

    public void By_ref_values_box_inside_array_initializers(TestContext t) => Check(t, false);
    public void By_ref_nullable_values_box_inside_array_initializers(TestContext t) => Check(t, true);

    private static void Check(TestContext t, bool nullable)
    {
        Type[] types = { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(Tagged) };
        object[] values = { (short)42, 42, 42L, 42f, 42d, 42m, new Tagged { Text = "value" } };
        for (var i = 0; i < types.Length; ++i)
        {
            var type = nullable ? typeof(Nullable<>).MakeGenericType(types[i]) : types[i];
            var delegateType = typeof(Read<>).MakeGenericType(type);
            var parameter = SysExpr.Parameter(type.MakeByRefType());
            var system = SysExpr.Lambda(delegateType, SysExpr.NewArrayInit(typeof(object),
                SysExpr.Convert(parameter, typeof(object)), SysExpr.Convert(parameter, typeof(object))), parameter).Compile();
            var fastParameter = Expr.Parameter(type.MakeByRefType());
            var fast = Expr.Lambda(delegateType, Expr.NewArrayInit(typeof(object),
                Expr.Convert(fastParameter, typeof(object)), Expr.Convert(fastParameter, typeof(object))), fastParameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Compare(t, system, fast, values[i]);
            if (nullable) Compare(t, system, fast, null);
        }
    }

    private static void Compare(TestContext t, Delegate system, Delegate fast, object value)
    {
        var expected = (object[])system.DynamicInvoke(new[] { value });
        var actual = (object[])fast.DynamicInvoke(new[] { value });
        t.AreEqual(expected[0], actual[0]);
        t.AreEqual(expected[1], actual[1]);
    }

    public delegate object[] Read<T>(ref T value);
    public struct Tagged { public string Text; }
}
