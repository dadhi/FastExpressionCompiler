using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NullableArithmeticEvaluationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Both_nullable_operands_run_once_in_order(t);
        Null_left_still_propagates_right_exception(t);
        Null_ref_assignment_evaluates_right_before_writeback(t);
        Nullable_shift_evaluates_nonnullable_right(t);
    }

    public void Both_nullable_operands_run_once_in_order(TestContext t)
    {
        foreach (var operation in new[] { ExpressionType.Add, ExpressionType.Subtract, ExpressionType.Multiply,
            ExpressionType.Divide, ExpressionType.Modulo, ExpressionType.And, ExpressionType.Or, ExpressionType.ExclusiveOr })
        foreach (var left in new int?[] { null, 5 })
        foreach (var right in new int?[] { null, 3 })
        {
            var first = SysExpr.Parameter(typeof(Func<int?>));
            var second = SysExpr.Parameter(typeof(Func<int?>));
            var expression = SysExpr.Lambda<Func<Func<int?>, Func<int?>, int?>>(
                SysExpr.MakeBinary(operation, SysExpr.Invoke(first), SysExpr.Invoke(second)), first, second);
            var system = expression.Compile();
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var events = new List<string>();
            Func<int?> getLeft = () => { events.Add("left"); return left; };
            Func<int?> getRight = () => { events.Add("right"); return right; };
            var expected = system(getLeft, getRight);
            events.Clear();
            t.AreEqual(expected, fast(getLeft, getRight));
            t.AreEqual("left,right", string.Join(",", events));
        }
    }

    public void Null_left_still_propagates_right_exception(TestContext t)
    {
        var right = SysExpr.Parameter(typeof(Func<int?>));
        var expression = SysExpr.Lambda<Func<Func<int?>, int?>>(SysExpr.Add(SysExpr.Default(typeof(int?)), SysExpr.Invoke(right)), right);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        var thrown = false;
        try { fast(() => { ++calls; throw new InvalidOperationException(); }); }
        catch (InvalidOperationException) { thrown = true; }
        t.AreEqual(true, thrown);
        t.AreEqual(1, calls);
    }

    public void Null_ref_assignment_evaluates_right_before_writeback(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?).MakeByRefType());
        var right = SysExpr.Parameter(typeof(Func<int?>));
        var rhs = SysExpr.TryCatch(SysExpr.Invoke(right), SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(int?))));
        var expression = SysExpr.Lambda<CompoundAssignmentExceptionRegressionTests.RefNullableInt>(SysExpr.AddAssign(left, rhs), left, right);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        int? value = null;
        var calls = 0;
        t.AreEqual((int?)null, fast(ref value, () => { ++calls; value = 100; return 3; }));
        t.AreEqual(1, calls);
        t.AreEqual((int?)null, value);
    }

    public void Nullable_shift_evaluates_nonnullable_right(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?));
        var right = SysExpr.Parameter(typeof(Func<int>));
        foreach (var body in new[] { SysExpr.LeftShift(left, SysExpr.Invoke(right)), SysExpr.RightShift(left, SysExpr.Invoke(right)) })
        {
            var expression = SysExpr.Lambda<Func<int?, Func<int>, int?>>(body, left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var calls = 0;
            t.AreEqual((int?)null, fast(null, () => { ++calls; return 2; }));
            t.AreEqual(1, calls);
        }
    }
}
