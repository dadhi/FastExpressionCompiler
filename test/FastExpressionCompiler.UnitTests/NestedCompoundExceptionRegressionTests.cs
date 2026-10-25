using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NestedCompoundExceptionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Member_and_index_rhs_can_nest_try_in_arithmetic(t);
        Ref_parameter_rhs_can_nest_try_in_arithmetic(t);
    }

    public static int Identity(int value) => value;
    public static int? NullableIdentity(int? value) => value;

    public void Member_and_index_rhs_can_nest_try_in_arithmetic(TestContext t)
    {
        foreach (var index in new[] { false, true })
        foreach (var left in new int?[] { null, 5 })
        {
            var box = new CompoundAssignmentExceptionRegressionTests.Box { Stored = left };
            var calls = 0;
            Func<int?> right = () => { ++calls; return 3; };
            var first = SysExpr.TryCatch(SysExpr.Invoke(SysExpr.Constant(right)),
                SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(int?))));
            // Collecting this ordinary call resets HasComplexExpression after the earlier Try.
            var second = SysExpr.Call(typeof(NestedCompoundExceptionRegressionTests).GetMethod(nameof(NullableIdentity)),
                SysExpr.Constant((int?)1, typeof(int?)));
            var rhs = SysExpr.Add(first, second);
            System.Linq.Expressions.Expression target = index
                ? SysExpr.MakeIndex(SysExpr.Constant(box), box.GetType().GetProperty("Item"), new[] { SysExpr.Constant(0) })
                : SysExpr.Property(SysExpr.Constant(box), nameof(box.Value));
            var expression = SysExpr.Lambda<Func<int?>>(SysExpr.AddAssign(target, rhs));
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(left + 4, fast());
            t.AreEqual(left + 4, box.Stored);
            t.AreEqual(1, calls);
            t.AreEqual(1, box.Gets);
            t.AreEqual(1, box.Sets);
        }
    }

    public void Ref_parameter_rhs_can_nest_try_in_arithmetic(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int).MakeByRefType());
        var right = SysExpr.Parameter(typeof(Func<int>));
        var first = SysExpr.TryCatch(SysExpr.Invoke(right), SysExpr.Catch(typeof(Exception), SysExpr.Constant(0)));
        var second = SysExpr.Call(typeof(NestedCompoundExceptionRegressionTests).GetMethod(nameof(Identity)), SysExpr.Constant(1));
        var expression = SysExpr.Lambda<CompoundAssignmentExceptionRegressionTests.RefInt>(
            SysExpr.AddAssign(left, SysExpr.Add(first, second)), left, right);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var value = 5;
        t.AreEqual(9, fast(ref value, () => { value = 100; return 3; }));
        t.AreEqual(9, value);
    }
}
