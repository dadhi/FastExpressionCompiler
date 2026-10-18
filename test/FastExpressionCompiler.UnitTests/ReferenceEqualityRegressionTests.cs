using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReferenceEqualityRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_comparisons_do_not_call_value_equality(t);
        Boxed_values_compared_through_interfaces_use_identity(t);
        Negating_reference_equality_preserves_identity(t);
        Complex_reference_operands_are_evaluated_once_in_order(t);
    }

    public sealed class EqualValue
    {
        public static int Calls;
        public override bool Equals(object obj) { ++Calls; return obj is EqualValue; }
        public override int GetHashCode() => 0;
        public static bool operator ==(EqualValue left, EqualValue right) { ++Calls; return true; }
        public static bool operator !=(EqualValue left, EqualValue right) { ++Calls; return false; }
    }

    public void Reference_comparisons_do_not_call_value_equality(TestContext t)
    {
        foreach (var type in new[] { typeof(EqualValue), typeof(object) })
        {
            var left = SysExpr.Parameter(type);
            var right = SysExpr.Parameter(type);
            var same = new EqualValue();
            foreach (var notEqual in new[] { false, true })
            {
                var body = notEqual ? SysExpr.ReferenceNotEqual(left, right) : SysExpr.ReferenceEqual(left, right);
                var fast = SysExpr.Lambda(body, left, right).FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                EqualValue.Calls = 0;
                t.AreEqual(notEqual, fast.DynamicInvoke(same, new EqualValue()));
                t.AreEqual(!notEqual, fast.DynamicInvoke(same, same));
                t.AreEqual(!notEqual, fast.DynamicInvoke(null, null));
                t.AreEqual(notEqual, fast.DynamicInvoke(same, null));
                t.AreEqual(0, EqualValue.Calls);
#if LIGHT_EXPRESSION
                var lp = Expression.Parameter(type);
                var rp = Expression.Parameter(type);
                var direct = notEqual ? Expression.ReferenceNotEqual(lp, rp) : Expression.ReferenceEqual(lp, rp);
                var directLambda = Expression.Lambda(direct, lp, rp);
                t.AreEqual(notEqual, directLambda.CompileFast(true).DynamicInvoke(same, new EqualValue()));
                t.AreEqual(notEqual, directLambda.ToLambdaExpression().Compile().DynamicInvoke(same, new EqualValue()));
                t.AreEqual(0, EqualValue.Calls);
#endif
            }
        }

        // Ordinary Equal still selects the overloaded operator.
        var p = SysExpr.Parameter(typeof(EqualValue));
        var equal = SysExpr.Lambda<Func<EqualValue, bool>>(SysExpr.Equal(p, SysExpr.Constant(new EqualValue())), p).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(equal)) return;
        EqualValue.Calls = 0;
        t.AreEqual(true, equal(new EqualValue()));
        t.AreEqual(1, EqualValue.Calls);
    }

    public void Boxed_values_compared_through_interfaces_use_identity(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(IComparable));
        var right = SysExpr.Parameter(typeof(IComparable));
        foreach (var notEqual in new[] { false, true })
        {
            var body = notEqual ? SysExpr.ReferenceNotEqual(left, right) : SysExpr.ReferenceEqual(left, right);
            var fast = SysExpr.Lambda<Func<IComparable, IComparable, bool>>(body, left, right).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            IComparable same = 42;
            t.AreEqual(notEqual, fast(same, 42));
            t.AreEqual(!notEqual, fast(same, same));
        }
    }

    public void Negating_reference_equality_preserves_identity(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(EqualValue));
        var right = SysExpr.Parameter(typeof(EqualValue));
        var body = SysExpr.Not(SysExpr.ReferenceEqual(left, right));
        var fast = SysExpr.Lambda<Func<EqualValue, EqualValue, bool>>(body, left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        EqualValue.Calls = 0;
        var same = new EqualValue();
        t.AreEqual(true, fast(same, new EqualValue()));
        t.AreEqual(false, fast(same, same));
        t.AreEqual(0, EqualValue.Calls);
    }

    public void Complex_reference_operands_are_evaluated_once_in_order(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(Func<object>));
        var right = SysExpr.Parameter(typeof(Func<object>));
        var body = SysExpr.ReferenceEqual(SysExpr.Invoke(left), SysExpr.TryFinally(SysExpr.Invoke(right), SysExpr.Empty()));
        var fast = SysExpr.Lambda<Func<Func<object>, Func<object>, bool>>(body, left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var order = 0;
        var same = new object();
        t.AreEqual(true, fast(() => { t.AreEqual(0, order++); return same; }, () => { t.AreEqual(1, order++); return same; }));
        t.AreEqual(2, order);
    }
}
