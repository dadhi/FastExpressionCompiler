#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection;
namespace FastExpressionCompiler.UnitTests;
public struct RefConversionMethodRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Ref_method_mutates_ref_operand(t);
        Lifted_ref_method_uses_copy_and_throws_on_null(t);
        Ref_method_handles_other_operand_locations(t);
        Nonref_method_still_dereferences_ref_operand(t);
        Lifted_ref_method_returns_nullable_struct(t);
        Ignored_ref_conversion_keeps_mutation(t);
    }
    public void Ref_method_mutates_ref_operand(TestContext t) => Check(t, false, 0, nameof(ConvertRef));
    public void Lifted_ref_method_uses_copy_and_throws_on_null(TestContext t) => Check(t, true, 0, nameof(ConvertRef));
    public void Ref_method_handles_other_operand_locations(TestContext t)
    { for (var shape = 1; shape != 6; ++shape) { Check(t, false, shape, nameof(ConvertRef)); Check(t, true, shape, nameof(ConvertRef)); } }
    public void Nonref_method_still_dereferences_ref_operand(TestContext t)
    { Check(t, false, 0, nameof(ConvertValue)); Check(t, true, 0, nameof(ConvertValue)); }
    public void Lifted_ref_method_returns_nullable_struct(TestContext t) => Check(t, true, 0, nameof(ConvertStructRef));
    public void Ignored_ref_conversion_keeps_mutation(TestContext t)
    {
        var p = Expression.Parameter(typeof(int).MakeByRefType());
        var expression = Expression.Lambda<Mutation<int>>(Expression.Block(
            Expression.Convert(p, typeof(Converted), typeof(RefConversionMethodRegressionTests).GetMethod(nameof(ConvertRef))), Expression.Constant(7)), p);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var value = 5;
        t.AreEqual(7, fast(ref value));
        t.AreEqual(6, value);
    }
    private static void Check(TestContext t, bool nullable, int shape, string methodName)
    {
        var type = nullable ? typeof(int?) : typeof(int);
        var p = Expression.Parameter(shape == 0 ? type.MakeByRefType() : type);
        var local = Expression.Variable(type);
        var cell = Expression.Variable(typeof(Cell<>).MakeGenericType(type));
        Expression location = shape == 2 ? local : shape == 3 ? Expression.Field(cell, nameof(Cell<int>.Value))
            : shape == 4 ? Expression.Constant(nullable ? (object)(int?)5 : 5, type)
            : shape == 5 ? Expression.Call(typeof(RefConversionMethodRegressionTests).GetMethod(nullable ? nameof(ReadNullableOperand) : nameof(ReadOperand))) : p;
        var method = typeof(RefConversionMethodRegressionTests).GetMethod(methodName);
        var targetType = methodName == nameof(ConvertStructRef) ? typeof(ConvertedStruct?) : typeof(Converted);
        var result = Expression.NewArrayInit(typeof(object), Expression.Convert(Expression.Convert(location, targetType, method), typeof(object)), Expression.Convert(shape >= 4 ? p : location, typeof(object)));
        var body = shape == 2 ? Expression.Block(new[] { local }, Expression.Assign(local, p), result)
            : shape == 3 ? Expression.Block(new[] { cell }, Expression.Assign(cell, Expression.New(cell.Type)), Expression.Assign(location, p), result) : (Expression)result;
        var expression = shape == 0 ? Expression.Lambda(typeof(Read<>).MakeGenericType(type), body, p) : Expression.Lambda(body, p);
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var value in nullable ? new object[] { null, 5 } : new object[] { 5 })
        {
            var expectedArgs = new[] { value };
            var actualArgs = new[] { value };
            OperandCalls = 0;
            var expected = Invoke(expression.Compile(), expectedArgs);
            var expectedCalls = OperandCalls;
            OperandCalls = 0;
            var actual = Invoke(fast, actualArgs);
            if (expected is object[] e && actual is object[] a)
            {
                if (e[0] is Converted ec && a[0] is Converted ac) t.AreEqual(ec.Value, ac.Value);
                else t.AreEqual(e[0], a[0]);
                t.AreEqual(e[1], a[1]);
            }
            else t.AreEqual(expected, actual);
            t.AreEqual(expectedArgs[0], actualArgs[0]);
            t.AreEqual(expectedCalls, OperandCalls);
        }
    }
    private static object Invoke(Delegate function, object[] arguments)
    { try { return function.DynamicInvoke(arguments); } catch (TargetInvocationException e) { return e.InnerException.GetType(); } }
    public delegate object[] Read<T>(ref T value);
    public delegate int Mutation<T>(ref T value);
    public static int OperandCalls;
    public static int ReadOperand() { ++OperandCalls; return 5; }
    public static int? ReadNullableOperand() { ++OperandCalls; return 5; }
    public static Converted ConvertRef(ref int value) => new Converted { Value = value++ };
    public static Converted ConvertValue(int value) => new Converted { Value = value };
    public static ConvertedStruct ConvertStructRef(ref int value) => new ConvertedStruct { Value = value++ };
    public class Converted { public int Value; }
    public struct ConvertedStruct { public int Value; }
    public class Cell<T> { public T Value; }
}
#endif
