using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ExplicitBinaryMethodRegressionTests : ITestX
{
    private static readonly ExpressionType[] Operations = { ExpressionType.Add, ExpressionType.AddChecked,
        ExpressionType.Subtract, ExpressionType.SubtractChecked, ExpressionType.Multiply, ExpressionType.MultiplyChecked,
        ExpressionType.Divide, ExpressionType.Modulo, ExpressionType.Power, ExpressionType.And, ExpressionType.Or,
        ExpressionType.ExclusiveOr, ExpressionType.LeftShift, ExpressionType.RightShift };

    public void Run(TestRun t)
    {
        Binary_operators_honor_explicit_method(t);
        Compound_assignment_honors_explicit_method_for_ref_parameters(t);
        Compound_assignment_honors_explicit_method_for_members_and_arrays(t);
        Lifted_and_nullable_accepting_methods_preserve_null_semantics(t);
    }

    public static int Combine(int left, int right) => left * 10 + right;
    public static int? CombineNullable(int? left, int? right) => (left ?? 7) * 10 + (right ?? 9);
    public delegate int ByRefInt(ref int left, int right);
    public sealed class Box { public int Value { get; set; } }

    private static ExpressionType Assignment(ExpressionType operation) => operation switch
    {
        ExpressionType.Add => ExpressionType.AddAssign,
        ExpressionType.AddChecked => ExpressionType.AddAssignChecked,
        ExpressionType.Subtract => ExpressionType.SubtractAssign,
        ExpressionType.SubtractChecked => ExpressionType.SubtractAssignChecked,
        ExpressionType.Multiply => ExpressionType.MultiplyAssign,
        ExpressionType.MultiplyChecked => ExpressionType.MultiplyAssignChecked,
        ExpressionType.Divide => ExpressionType.DivideAssign,
        ExpressionType.Modulo => ExpressionType.ModuloAssign,
        ExpressionType.Power => ExpressionType.PowerAssign,
        ExpressionType.And => ExpressionType.AndAssign,
        ExpressionType.Or => ExpressionType.OrAssign,
        ExpressionType.ExclusiveOr => ExpressionType.ExclusiveOrAssign,
        ExpressionType.LeftShift => ExpressionType.LeftShiftAssign,
        _ => ExpressionType.RightShiftAssign
    };

    public void Binary_operators_honor_explicit_method(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int));
        var right = SysExpr.Parameter(typeof(int));
        var method = typeof(ExplicitBinaryMethodRegressionTests).GetMethod(nameof(Combine));
        foreach (var operation in Operations)
        {
            var body = SysExpr.MakeBinary(operation, left, right, false, method);
            var fast = SysExpr.Lambda<Func<int, int, int>>(body, left, right).FromSysExpression().CompileFast(true);
            var constant = SysExpr.Lambda<Func<int>>(SysExpr.MakeBinary(operation, SysExpr.Constant(5), SysExpr.Constant(3), false, method))
                .FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(constant)) return;
            t.AreEqual(53, fast(5, 3));
            t.AreEqual(53, constant());
        }
    }

    public void Compound_assignment_honors_explicit_method_for_ref_parameters(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int).MakeByRefType());
        var right = SysExpr.Parameter(typeof(int));
        var method = typeof(ExplicitBinaryMethodRegressionTests).GetMethod(nameof(Combine));
        foreach (var operation in Operations)
        {
            var body = SysExpr.MakeBinary(Assignment(operation), left, right, false, method);
            var fast = SysExpr.Lambda<ByRefInt>(body, left, right).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var value = 5;
            t.AreEqual(53, fast(ref value, 3));
            t.AreEqual(53, value);
        }
    }

    public void Compound_assignment_honors_explicit_method_for_members_and_arrays(TestContext t)
    {
        var method = typeof(ExplicitBinaryMethodRegressionTests).GetMethod(nameof(Combine));
        foreach (var operation in Operations)
        {
            var box = new Box { Value = 5 };
            var array = new[] { 5 };
            System.Linq.Expressions.Expression[] targets = { SysExpr.Property(SysExpr.Constant(box), nameof(Box.Value)),
                SysExpr.ArrayAccess(SysExpr.Constant(array), SysExpr.Constant(0)) };
            foreach (var target in targets)
            {
                var body = SysExpr.MakeBinary(Assignment(operation), target, SysExpr.Constant(3), false, method);
                var fast = SysExpr.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
                if (!t.IsNotNull(fast)) return;
                t.AreEqual(53, fast());
            }
            t.AreEqual(53, box.Value);
            t.AreEqual(53, array[0]);
        }
    }

    public void Lifted_and_nullable_accepting_methods_preserve_null_semantics(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int?));
        var right = SysExpr.Parameter(typeof(int?));
        foreach (var name in new[] { nameof(Combine), nameof(CombineNullable) })
        {
            var method = typeof(ExplicitBinaryMethodRegressionTests).GetMethod(name);
            var expression = SysExpr.Lambda<Func<int?, int?, int?>>(SysExpr.Add(left, right, method), left, right);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var a in new int?[] { null, 5 })
            foreach (var b in new int?[] { null, 3 })
                t.AreEqual(expression.Compile()(a, b), fast(a, b));
        }
    }
}
