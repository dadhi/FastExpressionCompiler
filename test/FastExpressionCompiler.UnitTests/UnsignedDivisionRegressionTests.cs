using System;
using System.Reflection;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UnsignedDivisionRegressionTests : ITest
{
    public int Run()
    {
        Unsigned_division_uses_unsigned_operands();
        Lifted_unsigned_division_uses_unsigned_operands();
        Unsigned_division_assignment_uses_unsigned_operands();
        return 3;
    }

    public void Unsigned_division_uses_unsigned_operands() => Check(false, false);
    public void Lifted_unsigned_division_uses_unsigned_operands() => Check(true, false);
    public void Unsigned_division_assignment_uses_unsigned_operands() => Check(false, true);

    private static void Check(bool nullable, bool assign)
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
            Asserts.IsNotNull(fast);
            foreach (var a in values[i])
            foreach (var b in values[i]) Asserts.AreEqual(Invoke(system, a, b), Invoke(fast, a, b));
            if (nullable)
                foreach (var value in values[i])
                {
                    Asserts.AreEqual(Invoke(system, value, null), Invoke(fast, value, null));
                    Asserts.AreEqual(Invoke(system, null, value), Invoke(fast, null, value));
                }
        }
    }

    private static object Invoke(Delegate function, object a, object b)
    {
        try { return function.DynamicInvoke(new[] { a, b }); }
        catch (TargetInvocationException e) { return e.InnerException.GetType(); }
    }
}
