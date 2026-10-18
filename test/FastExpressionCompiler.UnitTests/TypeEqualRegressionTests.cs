using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct TypeEqualRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Value_nullable_and_reference_type_tests_match_system(t);
        Reference_type_tests_require_exact_runtime_type(t);
        Type_tests_preserve_operand_effects_and_void_semantics(t);
        By_reference_operands_are_compared_by_value(t);
    }

    public class Base { }
    public sealed class Derived : Base { }

    public void Value_nullable_and_reference_type_tests_match_system(TestContext t)
    {
        Type[] targets = { typeof(int), typeof(int?), typeof(long), typeof(long?), typeof(object), typeof(ValueType),
            typeof(IComparable), typeof(string), typeof(void), typeof(int).MakePointerType(), typeof(DayOfWeek), typeof(DayOfWeek?) };
        foreach (var source in new[] { typeof(int), typeof(int?), typeof(object), typeof(IComparable), typeof(DayOfWeek), typeof(DayOfWeek?) })
        foreach (var target in targets)
        {
            var parameter = SysExpr.Parameter(source);
            var lambda = SysExpr.Lambda(SysExpr.TypeEqual(parameter, target), parameter);
            var expected = lambda.Compile();
            var fast = lambda.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            object value = source == typeof(DayOfWeek) || source == typeof(DayOfWeek?) ? (object)DayOfWeek.Friday : 42;
            t.AreEqual(expected.DynamicInvoke(value), fast.DynamicInvoke(value));
            if (!source.IsValueType || Nullable.GetUnderlyingType(source) != null)
                t.AreEqual(expected.DynamicInvoke(new object[] { null }), fast.DynamicInvoke(new object[] { null }));
        }
    }

    public void Reference_type_tests_require_exact_runtime_type(TestContext t)
    {
        foreach (var target in new[] { typeof(object), typeof(Base), typeof(Derived), typeof(Action<string>), typeof(Action<object>) })
        {
            var parameter = SysExpr.Parameter(typeof(object));
            var lambda = SysExpr.Lambda<Func<object, bool>>(SysExpr.TypeEqual(parameter, target), parameter);
            var expected = lambda.Compile();
            var fast = lambda.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new object[] { null, new object(), new Base(), new Derived(), (Action<string>)(_ => { }), (Action<object>)(_ => { }) })
                t.AreEqual(expected(value), fast(value));
        }
    }

    public void Type_tests_preserve_operand_effects_and_void_semantics(TestContext t)
    {
        foreach (var source in new[] { typeof(int), typeof(int?), typeof(object), typeof(void) })
        foreach (var target in new[] { typeof(int), typeof(int?), typeof(object), typeof(void) })
        {
            var counter = SysExpr.Parameter(typeof(Action));
            var operand = SysExpr.Block(SysExpr.Invoke(counter), SysExpr.Default(source));
            var body = SysExpr.TypeEqual(operand, target);
            var lambda = SysExpr.Lambda<Func<Action, bool>>(body, counter);
            var fast = lambda.FromSysExpression().CompileFast(true);
            var ignored = SysExpr.Lambda<Action<Action>>(SysExpr.Block(body, SysExpr.Empty()), counter).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
            var calls = 0;
            t.AreEqual(lambda.Compile()(() => { }), fast(() => ++calls));
            t.AreEqual(1, calls);
            ignored(() => ++calls);
            t.AreEqual(2, calls);
        }
    }

    public delegate bool RefInt(ref int value);
    public delegate bool RefNullable(ref int? value);

    public void By_reference_operands_are_compared_by_value(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(int).MakeByRefType());
        var lambda = SysExpr.Lambda<RefInt>(SysExpr.TypeEqual(parameter, typeof(int)), parameter);
        var fast = lambda.FromSysExpression().CompileFast(true);
        var nullableParameter = SysExpr.Parameter(typeof(int?).MakeByRefType());
        var nullableLambda = SysExpr.Lambda<RefNullable>(SysExpr.TypeEqual(nullableParameter, typeof(int?)), nullableParameter);
        var nullableFast = nullableLambda.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast) || !t.IsNotNull(nullableFast)) return;
        var value = 42;
        int? nullable = 42;
        t.IsTrue(fast(ref value));
        t.IsTrue(nullableFast(ref nullable));
        nullable = null;
        t.IsFalse(nullableFast(ref nullable));
    }
}
