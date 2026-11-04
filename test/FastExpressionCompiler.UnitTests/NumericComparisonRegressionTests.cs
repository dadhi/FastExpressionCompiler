using System;
using SysExpr = System.Linq.Expressions.Expression;
using Node = System.Linq.Expressions.ExpressionType;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NumericComparisonRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Numeric_parameter_comparisons_match_system(t);
        Nullable_constant_comparisons_match_system(t);
        Lifted_results_and_operand_effects_are_preserved(t);
    }

    public void Numeric_parameter_comparisons_match_system(TestContext t) => Check(t, false);
    public void Nullable_constant_comparisons_match_system(TestContext t) => Check(t, true);

    private static void Check(TestContext t, bool constant)
    {
        Type[] types = { typeof(int), typeof(uint), typeof(ulong), typeof(double), typeof(decimal) };
        object[][] inputs = {
            new object[] { -1, 0, int.MaxValue }, new object[] { 0u, 1u, (uint)int.MaxValue + 1, uint.MaxValue },
            new object[] { 0UL, 1UL, (ulong)long.MaxValue + 1, ulong.MaxValue },
            new object[] { -1d, 0d, double.NaN, double.NegativeInfinity, double.PositiveInfinity },
            new object[] { -1m, 0m, decimal.MaxValue }
        };
        for (var i = 0; i < types.Length; ++i)
        foreach (var nullable in constant ? new[] { true } : new[] { false, true })
        foreach (var lift in nullable ? new[] { false, true } : new[] { false })
        foreach (var operation in new[] { Node.Equal, Node.NotEqual, Node.LessThan, Node.GreaterThan, Node.LessThanOrEqual, Node.GreaterThanOrEqual })
        {
            var type = nullable ? typeof(Nullable<>).MakeGenericType(types[i]) : types[i];
            var values = nullable ? new object[inputs[i].Length + 1] : inputs[i];
            if (nullable) Array.Copy(inputs[i], 0, values, 1, inputs[i].Length);
            var left = SysExpr.Parameter(type);
            var right = SysExpr.Parameter(type);
            var expression = SysExpr.Lambda(SysExpr.MakeBinary(operation, left, right, lift, null), left, right);
            var system = constant ? null : expression.Compile();
            var fast = constant ? null : expression.FromSysExpression().CompileFast(true);
            if (!constant && !t.IsNotNull(fast)) return;
            foreach (var a in values)
            foreach (var b in values)
            {
                if (!constant) t.AreEqual(system.DynamicInvoke(a, b), fast.DynamicInvoke(a, b));
                else
                {
                    var c = SysExpr.Lambda(SysExpr.MakeBinary(operation, SysExpr.Constant(a, type), SysExpr.Constant(b, type), lift, null));
                    var compiled = c.FromSysExpression().CompileFast(true);
                    if (!t.IsNotNull(compiled)) return;
                    t.AreEqual(c.Compile().DynamicInvoke(), compiled.DynamicInvoke());
                }
            }
        }
    }

    public void Lifted_results_and_operand_effects_are_preserved(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(Func<int?>));
        var right = SysExpr.Parameter(typeof(Func<int?>));
        var comparison = SysExpr.MakeBinary(Node.GreaterThan, SysExpr.Invoke(left), SysExpr.Invoke(right), true, null);
        var fast = SysExpr.Lambda<Func<Func<int?>, Func<int?>, bool?>>(comparison, left, right).FromSysExpression().CompileFast(true);
        var member = SysExpr.Lambda<Func<Func<int?>, Func<int?>, bool>>(SysExpr.Property(comparison, "HasValue"), left, right).FromSysExpression().CompileFast(true);
        var ignored = SysExpr.Lambda<Action<Func<int?>, Func<int?>>>(SysExpr.Block(comparison, SysExpr.Empty()), left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast) || !t.IsNotNull(member) || !t.IsNotNull(ignored)) return;
        var calls = 0;
        Func<int?> a = () => { ++calls; return null; };
        Func<int?> b = () => { ++calls; return 2; };
        t.IsNull(fast(a, b));
        t.IsFalse(member(a, b));
        ignored(a, b);
        t.AreEqual(6, calls);
    }
}
