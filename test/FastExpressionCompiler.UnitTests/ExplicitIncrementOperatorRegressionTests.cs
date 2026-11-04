#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FastExpressionCompiler.UnitTests;

public struct ExplicitIncrementOperatorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_method_controls_result_and_assignment(t);
        Numeric_method_controls_result_and_assignment(t);
        Lifted_method_propagates_null(t);
        Nullable_method_receives_null(t);
    }

    public void Reference_method_controls_result_and_assignment(TestContext t) => Check(t, typeof(string), "Replace", new object[] { null, "hello" });
    public void Numeric_method_controls_result_and_assignment(TestContext t) => Check(t, typeof(int), "Change", new object[] { 0, 42 });
    public void Lifted_method_propagates_null(TestContext t) => Check(t, typeof(int?), "Change", new object[] { null, 42 });
    public void Nullable_method_receives_null(TestContext t) => Check(t, typeof(int?), "ChangeNullable", new object[] { null, 42 });

    private static void Check(TestContext t, Type type, string methodName, object[] values)
    {
        var method = typeof(ExplicitIncrementOperatorRegressionTests).GetMethod(methodName);
        foreach (var operation in new[] { ExpressionType.PreIncrementAssign, ExpressionType.PostIncrementAssign,
            ExpressionType.PreDecrementAssign, ExpressionType.PostDecrementAssign })
        for (var shape = 0; shape != 4; ++shape)
        {
            var parameter = Expression.Parameter(shape == 3 ? type.MakeByRefType() : type);
            var local = Expression.Variable(type);
            var cell = Expression.Variable(typeof(Cell<>).MakeGenericType(type));
            var location = shape == 0 ? (Expression)local : shape == 2 ? Expression.Field(cell, "Value") : parameter;
            var results = Expression.NewArrayInit(typeof(object),
                Expression.Convert(Expression.MakeUnary(operation, location, null, method), typeof(object)),
                Expression.Convert(location, typeof(object)));
            var body = shape == 0
                ? Expression.Block(new[] { local }, Expression.Assign(local, parameter), results)
                : shape == 2
                    ? Expression.Block(new[] { cell }, Expression.Assign(cell, Expression.New(cell.Type)), Expression.Assign(location, parameter), results)
                    : (Expression)results;
            var expression = shape == 3
                ? Expression.Lambda(typeof(Mutation<>).MakeGenericType(type), body, parameter)
                : Expression.Lambda(body, parameter);
            var system = expression.Compile();
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in values)
            {
                Calls = 0;
                var expected = (object[])system.DynamicInvoke(new[] { value });
                var expectedCalls = Calls;
                Calls = 0;
                var actual = (object[])fast.DynamicInvoke(new[] { value });
                t.AreEqual(expected[0], actual[0]);
                t.AreEqual(expected[1], actual[1]);
                t.AreEqual(expectedCalls, Calls);
            }
        }
    }

    public static int Calls;
    public static string Replace(string value) { ++Calls; return "Eggplant"; }
    public static int Change(int value) { ++Calls; return value + 10; }
    public static int? ChangeNullable(int? value) { ++Calls; return (value ?? 100) + 10; }
    public delegate object[] Mutation<T>(ref T value);
    public class Cell<T> { public T Value; }
}
#endif
