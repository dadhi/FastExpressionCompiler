using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RefAssignmentExceptionTests : ITestX
{
    public delegate int Update(ref int value, Func<int> right);
    public delegate int? UpdateNullable(ref int? value, Func<int?> right);
    public void Run(TestRun t) { Ref_rhs_preserves_original_value(t); Nullable_null_still_evaluates_rhs(t); }
    public void Ref_rhs_preserves_original_value(TestContext t)
    {
        var left = S.Parameter(typeof(int).MakeByRefType());
        var right = S.Parameter(typeof(Func<int>));
        var rhs = S.TryFinally(S.Invoke(right), S.Empty());
        foreach (var compound in new[] { false, true })
        {
            var tree = S.Lambda<Update>(compound ? S.AddAssign(left, rhs) : S.Assign(left, rhs), left, right);
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var value = 5;
            t.AreEqual(compound ? 8 : 3, fast(ref value, () => { value = 100; return 3; }));
            t.AreEqual(compound ? 8 : 3, value);
            value = 5;
            t.Throws<InvalidOperationException>(() => fast(ref value, () => throw new InvalidOperationException()));
            t.AreEqual(5, value);
        }
    }
    public void Nullable_null_still_evaluates_rhs(TestContext t)
    {
        var left = S.Parameter(typeof(int?).MakeByRefType());
        var right = S.Parameter(typeof(Func<int?>));
        var tree = S.Lambda<UpdateNullable>(S.AddAssign(left, S.TryFinally(S.Invoke(right), S.Empty())), left, right);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        int? value = null; var calls = 0;
        t.AreEqual((int?)null, fast(ref value, () => { ++calls; value = 100; return 3; }));
        t.AreEqual(1, calls); t.AreEqual((int?)null, value);
    }
}
