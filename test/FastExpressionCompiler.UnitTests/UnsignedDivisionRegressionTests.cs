using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UnsignedDivisionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Unsigned_division_uses_unsigned_operands(t);
        Lifted_unsigned_division_uses_unsigned_operands(t);
        Unsigned_division_assignment_uses_unsigned_operands(t);
    }

    public void Unsigned_division_uses_unsigned_operands(TestContext t) => Check(t, false, false);
    public void Lifted_unsigned_division_uses_unsigned_operands(TestContext t) => Check(t, true, false);
    public void Unsigned_division_assignment_uses_unsigned_operands(TestContext t) => Check(t, false, true);

    private static void Check(TestContext t, bool nullable, bool assign)
    {
        Type[] types = { typeof(uint), typeof(ulong) };
        object[][] values = { new object[] { 0u, 1u, 2u, (uint)int.MaxValue + 1, uint.MaxValue },
            new object[] { 0UL, 1UL, 2UL, (ulong)long.MaxValue + 1, ulong.MaxValue } };
        for (var i = 0; i < types.Length; ++i)
        {
            var type = nullable ? typeof(Nullable<>).MakeGenericType(types[i]) : types[i];
            var left = SysExpr.Parameter(type);
            var right = SysExpr.Parameter(type);
            var body = assign ? SysExpr.DivideAssign(left, right) : SysExpr.Divide(left, right);
            var expression = SysExpr.Lambda(body, left, right);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in values[i])
            foreach (var b in values[i]) t.AreEqual(Invoke(system, a, b), Invoke(fast, a, b));
            if (nullable)
                foreach (var value in values[i])
                {
                    t.AreEqual(Invoke(system, value, null), Invoke(fast, value, null));
                    t.AreEqual(Invoke(system, null, value), Invoke(fast, null, value));
                }
        }
    }

    private static object Invoke(Delegate function, object a, object b)
    {
        try { return function.DynamicInvoke(new[] { a, b }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
