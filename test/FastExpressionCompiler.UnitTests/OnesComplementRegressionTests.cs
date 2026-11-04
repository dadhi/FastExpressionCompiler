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

public struct OnesComplementRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nullable_integer_complement_matches_system(t);
        Nullable_local_result_supports_member_access(t);
        Ignored_complements_evaluate_operands_once(t);
#if !LIGHT_EXPRESSION
        Explicit_operator_methods_are_called(t);
#endif
    }

    public void Nullable_integer_complement_matches_system(TestContext t)
    {
        Type[] types = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong) };
        foreach (var type in types)
        foreach (var ones in new[] { false, true })
        {
            var nullable = typeof(Nullable<>).MakeGenericType(type);
            var parameter = SysExpr.Parameter(nullable);
            var expression = SysExpr.Lambda(ones ? SysExpr.OnesComplement(parameter) : SysExpr.Not(parameter), parameter);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new[] { null, Convert.ChangeType(0, type), Convert.ChangeType(1, type), type.GetField("MaxValue").GetValue(null) })
                t.AreEqual(Invoke(system, value), Invoke(fast, value));
        }
    }

    public void Nullable_local_result_supports_member_access(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(int?));
        var local = Expr.Variable(typeof(int?));
        var body = Expr.Block(new[] { local }, Expr.Assign(local, parameter), Expr.Property(Expr.OnesComplement(local), "Value"));
        var fast = Expr.Lambda<Func<int?, int>>(body, parameter).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(~42, fast(42));
        t.Throws<InvalidOperationException>(() => fast(null));
    }

    public void Ignored_complements_evaluate_operands_once(TestContext t)
    {
        var getValue = Expr.Parameter(typeof(Func<int?>));
        var fast = Expr.Lambda<Action<Func<int?>>>(Expr.Block(Expr.OnesComplement(Expr.Invoke(getValue)), Expr.Empty()), getValue).CompileFast(true);
        var getInt = Expr.Parameter(typeof(Func<int>));
        var ignored = Expr.Lambda<Action<Func<int>>>(Expr.Block(Expr.OnesComplement(Expr.Invoke(getInt)), Expr.Empty()), getInt).CompileFast(true);
        if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
        var calls = 0;
        fast(() => { ++calls; return null; });
        fast(() => { ++calls; return 42; });
        ignored(() => { ++calls; return 42; });
        t.AreEqual(3, calls);
    }

#if !LIGHT_EXPRESSION
    public void Explicit_operator_methods_are_called(TestContext t)
    {
        var parameter = Expr.Parameter(typeof(Complementary));
        foreach (var body in new[] { Expr.OnesComplement(parameter), Expr.OnesComplement(parameter, typeof(Complementary).GetMethod("Complement")) })
        {
            var fast = Expr.Lambda<Func<Complementary, Complementary>>(body, parameter).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(~43, fast(new Complementary { Value = 43 }).Value);
        }
    }

    public struct Complementary
    {
        public int Value;
        public static Complementary operator ~(Complementary value) => Complement(value);
        public static Complementary Complement(Complementary value) => new Complementary { Value = ~value.Value };
    }
#endif

    private static object Invoke(Delegate function, object value)
    {
        try { return function.DynamicInvoke(new[] { value }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
