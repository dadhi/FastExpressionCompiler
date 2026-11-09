using System;
using System.Reflection;
using E = System.Linq.Expressions;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct OperandValueContextRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Not_operands_are_values(t);
        Lifted_unary_and_type_as_operands_are_values(t);
        Conversion_operands_are_values(t);
        Type_test_operands_are_values(t);
        Coalesce_operands_are_values(t);
        Primitive_and_enum_field_receivers_keep_values(t);
    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo };

    // Exercise the same legal tree with a passed parameter, a local and a captured parameter.
    private static void Check<T>(TestContext t, E.ParameterExpression parameter, E.Expression body, T value, int expected, E.Expression systemOracle = null)
    {
        var direct = S.Lambda<Func<T, int>>(body, parameter);
        var local = S.Variable(typeof(T));
        var localBody = new ReplaceParameter(parameter, local).Visit(body);
        var withLocal = S.Lambda<Func<T, int>>(S.Block(new[] { local }, S.Assign(local, parameter), localBody), parameter);
        var captured = S.Lambda<Func<T, Func<int>>>(S.Lambda<Func<int>>(body), parameter);
        var oracleBody = systemOracle ?? body;
        var oracleLocal = new ReplaceParameter(parameter, local).Visit(oracleBody);
        t.AreEqual(expected, S.Lambda<Func<T, int>>(oracleBody, parameter).Compile()(value));
        t.AreEqual(expected, S.Lambda<Func<T, int>>(S.Block(new[] { local }, S.Assign(local, parameter), oracleLocal), parameter).Compile()(value));
        t.AreEqual(expected, S.Lambda<Func<T, Func<int>>>(S.Lambda<Func<int>>(oracleBody), parameter).Compile()(value)());
        foreach (var flags in Flags)
        {
            var fastDirect = direct.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastDirect)) t.AreEqual(expected, fastDirect(value));
            var fastLocal = withLocal.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastLocal)) t.AreEqual(expected, fastLocal(value));
            var fastCaptured = captured.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastCaptured)) t.AreEqual(expected, fastCaptured(value)());
        }
    }

    private sealed class ReplaceParameter : E.ExpressionVisitor
    {
        private readonly E.ParameterExpression _from, _to;
        public ReplaceParameter(E.ParameterExpression from, E.ParameterExpression to) { _from = from; _to = to; }
        protected override E.Expression VisitParameter(E.ParameterExpression node) => node == _from ? _to : node;
    }

    public void Not_operands_are_values(TestContext t)
    {
        var value = S.Parameter(typeof(bool));
        var compare = typeof(bool).GetMethod(nameof(bool.CompareTo), new[] { typeof(bool) });
        var body = S.Call(S.Not(value), compare, S.Constant(false));
        Check(t, value, body, false, 1);
        Check(t, value, body, true, 0);
    }

    public void Lifted_unary_and_type_as_operands_are_values(TestContext t)
    {
        var nullable = S.Parameter(typeof(int?));
        var complement = S.Call(S.Not(nullable), typeof(int?).GetMethod(nameof(Nullable<int>.GetValueOrDefault), Type.EmptyTypes));
        Check(t, nullable, complement, (int?)7, -8);
        Check(t, nullable, complement, (int?)null, 0);
        Check(t, nullable, S.Property(S.Not(nullable), nameof(Nullable<int>.Value)), (int?)7, -8);
        var value = S.Parameter(typeof(int));
        var boxedType = S.Call(S.TypeAs(value, typeof(object)), typeof(object).GetMethod(nameof(object.GetType)));
        var body = S.Condition(S.Equal(boxedType, S.Constant(typeof(int))), S.Constant(1), S.Constant(0));
        Check(t, value, body, 7, 1);
        var equals = S.Call(S.TypeAs(value, typeof(object)), typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) }), S.Constant(7, typeof(object)));
        Check(t, value, S.Condition(equals, S.Constant(1), S.Constant(0)), 7, 1);
    }

    public struct Source
    {
        public int Value;
        public static explicit operator Target(Source value) => new Target { Value = value.Value + 1 };
    }
    public struct Target { public int Value; public int Read() => Value; }

    public void Conversion_operands_are_values(TestContext t)
    {
        var value = S.Parameter(typeof(int));
        Check(t, value, S.Property(S.Convert(value, typeof(int?)), nameof(Nullable<int>.Value)), 7, 7);
        var source = S.Parameter(typeof(Source));
        var converted = S.Convert(source, typeof(Target));
        Check(t, source, S.Field(converted, nameof(Target.Value)), new Source { Value = 7 }, 8);
        Check(t, source, S.Call(converted, typeof(Target).GetMethod(nameof(Target.Read))), new Source { Value = 7 }, 8);
    }

    public void Type_test_operands_are_values(TestContext t)
    {
        // Reading Boolean's instance field is a legal field-access tree. It requests an
        // address for the Boolean result, not for the operand of TypeIs/TypeEqual.
        var field = typeof(bool).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var value = S.Parameter(typeof(int?));
        foreach (var test in new E.Expression[] { S.TypeIs(value, typeof(int)), S.TypeEqual(value, typeof(int)) })
        {
            var body = S.Condition(S.Field(test, field), S.Constant(1), S.Constant(0));
            E.Expression systemOracle = null;
#if NETFRAMEWORK
            // Framework's IL compiler incorrectly builds reference equality for nullable
            // TypeEqual operands. Use equivalent HasValue controls there; FEC still compiles
            // the original TypeEqual tree, as do the modern System compiler controls.
            if (test.NodeType == E.ExpressionType.TypeEqual)
                systemOracle = S.Condition(S.Field(S.Property(value, nameof(Nullable<int>.HasValue)), field), S.Constant(1), S.Constant(0));
#endif
            Check(t, value, body, (int?)7, 1, systemOracle);
            Check(t, value, body, (int?)null, 0, systemOracle);
        }
    }

    public enum Number { Seven = 7 }
    public void Primitive_and_enum_field_receivers_keep_values(TestContext t)
    {
        var boolean = S.Parameter(typeof(bool));
        var field = typeof(bool).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var body = S.Condition(S.Field(boolean, field), S.Constant(1), S.Constant(0));
        Check(t, boolean, body, false, 0);
        Check(t, boolean, body, true, 1);
        var number = S.Parameter(typeof(Number));
        Check(t, number, S.Field(number, typeof(Number).GetField("value__")), Number.Seven, 7);
    }

    public void Coalesce_operands_are_values(TestContext t)
    {
        var value = S.Parameter(typeof(int?));
        var result = S.Coalesce(value, S.Constant((int?)1, typeof(int?)));
        var body = S.Property(result, nameof(Nullable<int>.Value));
        Check(t, value, body, (int?)7, 7);
        Check(t, value, body, (int?)null, 1);
    }
}
