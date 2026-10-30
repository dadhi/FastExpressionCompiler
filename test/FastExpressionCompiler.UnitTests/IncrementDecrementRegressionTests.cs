using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct IncrementDecrementRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Increment_decrement_local_values_match_system(t);
        Increment_decrement_parameters_match_system(t);
        Increment_decrement_fields_match_system(t);
        Increment_decrement_by_ref_values_match_system(t);
    }

    public void Increment_decrement_local_values_match_system(TestContext t) => Check(t, 0);
    public void Increment_decrement_parameters_match_system(TestContext t) => Check(t, 1);
    public void Increment_decrement_fields_match_system(TestContext t) => Check(t, 2);
    public void Increment_decrement_by_ref_values_match_system(TestContext t) => Check(t, 3);

    private static void Check(TestContext t, int shape)
    {
        Type[] types = { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal), typeof(Counter) };
        Node[] operations = { Node.PreIncrementAssign, Node.PostIncrementAssign, Node.PreDecrementAssign, Node.PostDecrementAssign };
        foreach (var underlying in types)
        foreach (var nullable in new[] { false, true })
        foreach (var operation in operations)
        {
            var type = nullable ? typeof(Nullable<>).MakeGenericType(underlying) : underlying;
            var parameter = SysExpr.Parameter(shape == 3 ? type.MakeByRefType() : type);
            var local = SysExpr.Variable(type);
            var cell = SysExpr.Variable(typeof(Cell<>).MakeGenericType(type));
            var location = shape == 0 ? (SysExpr)local : shape == 2 ? SysExpr.Field(cell, "Value") : parameter;
            var results = SysExpr.NewArrayInit(typeof(object),
                SysExpr.Convert(SysExpr.MakeUnary(operation, location, null), typeof(object)),
                SysExpr.Convert(location, typeof(object)));
            var body = shape == 0
                ? SysExpr.Block(new[] { local }, SysExpr.Assign(local, parameter), results)
                : shape == 2
                    ? SysExpr.Block(new[] { cell }, SysExpr.Assign(cell, SysExpr.New(cell.Type)), SysExpr.Assign(location, parameter), results)
                    : (SysExpr)results;
            var expression = shape == 3
                ? SysExpr.Lambda(typeof(Mutation<>).MakeGenericType(type), body, parameter)
                : SysExpr.Lambda(body, parameter);
            var system = expression.Compile();
            var fastParameter = Expr.Parameter(shape == 3 ? type.MakeByRefType() : type);
            var fastLocal = Expr.Variable(type);
            var fastCell = Expr.Variable(cell.Type);
            var fastLocation = shape == 0 ? (Expr)fastLocal : shape == 2 ? Expr.Field(fastCell, "Value") : fastParameter;
            var fastResults = Expr.NewArrayInit(typeof(object),
                Expr.Convert(Expr.MakeUnary(operation, fastLocation, null), typeof(object)),
                Expr.Convert(fastLocation, typeof(object)));
            var fastBody = shape == 0
                ? Expr.Block(new[] { fastLocal }, Expr.Assign(fastLocal, fastParameter), fastResults)
                : shape == 2
                    ? Expr.Block(new[] { fastCell }, Expr.Assign(fastCell, Expr.New(fastCell.Type)), Expr.Assign(fastLocation, fastParameter), fastResults)
                    : (Expr)fastResults;
            var fastExpression = shape == 3
                ? Expr.Lambda(typeof(Mutation<>).MakeGenericType(type), fastBody, fastParameter)
                : Expr.Lambda(fastBody, fastParameter);
            var fast = fastExpression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            object[] values = underlying == typeof(Counter)
                ? new object[] { new Counter { Value = 0, Label = "zero" }, new Counter { Value = 42, Label = "value" } }
                : new[] { Convert.ChangeType(0, underlying), Convert.ChangeType(1, underlying), underlying.GetField("MaxValue").GetValue(null) };
            foreach (var value in values) Compare(t, Invoke(system, value), Invoke(fast, value));
            if (nullable) Compare(t, Invoke(system, null), Invoke(fast, null));
        }
    }

    private static void Compare(TestContext t, object expected, object actual)
    {
        if (expected is object[] e && actual is object[] a)
        {
            t.AreEqual(e[0], a[0]);
            t.AreEqual(e[1], a[1]);
        }
        else t.AreEqual(expected, actual);
    }

    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { RuntimeHelpers.GetObjectValue(value) }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }

    public delegate object[] Mutation<T>(ref T value);
    public class Cell<T> { public T Value; }
    public struct Counter
    {
        public int Value;
        public string Label;
        public static Counter operator ++(Counter value) { ++value.Value; return value; }
        public static Counter operator --(Counter value) { --value.Value; return value; }
    }
}
