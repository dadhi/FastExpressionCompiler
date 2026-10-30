using System;
using System.Linq.Expressions;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct AssignmentConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Ref_assignment_converts_and_captures_outer_parameter(t);
        Member_and_array_assignments_convert_before_writeback(t);
        Conversion_can_change_operator_result_type(t);
        Ignored_conversion_effects_and_exception_writeback(t);
    }

    public static int Combine(int left, int right) => left * 10 + right;
    public static long CombineNumbers(Number left, Number right) => left.Value * 10L + right.Value;
    public delegate int RefInt(ref int left, int right);
    public sealed class Box { public int Value { get; set; } }
    public sealed class Number
    {
        public readonly long Value;
        public Number(long value) => Value = value;
    }

    public void Ref_assignment_converts_and_captures_outer_parameter(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int).MakeByRefType());
        var right = SysExpr.Parameter(typeof(int));
        var value = SysExpr.Parameter(typeof(int));
        var conversion = SysExpr.Lambda<Func<int, int>>(SysExpr.Add(value, right), value);
        foreach (var operation in new[] { ExpressionType.AddAssign, ExpressionType.MultiplyAssignChecked, ExpressionType.RightShiftAssign })
        {
            var body = SysExpr.MakeBinary(operation, left, right, false,
                typeof(AssignmentConversionRegressionTests).GetMethod(nameof(Combine)), conversion);
            var fast = SysExpr.Lambda<RefInt>(body, left, right).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var number = 5;
            t.AreEqual(56, fast(ref number, 3));
            t.AreEqual(56, number);
        }
    }

    public void Member_and_array_assignments_convert_before_writeback(TestContext t)
    {
        var box = new Box { Value = 5 };
        var array = new[] { 5 };
        var value = SysExpr.Parameter(typeof(int));
        var conversion = SysExpr.Lambda<Func<int, int>>(SysExpr.Multiply(value, SysExpr.Constant(2)), value);
        System.Linq.Expressions.Expression[] targets = { SysExpr.Property(SysExpr.Constant(box), nameof(Box.Value)),
            SysExpr.ArrayAccess(SysExpr.Constant(array), SysExpr.Constant(0)) };
        foreach (var target in targets)
        {
            var body = SysExpr.AddAssign(target, SysExpr.Constant(3),
                typeof(AssignmentConversionRegressionTests).GetMethod(nameof(Combine)), conversion);
            var fast = SysExpr.Lambda<Func<int>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(106, fast());
        }
        t.AreEqual(106, box.Value);
        t.AreEqual(106, array[0]);
    }

    public void Conversion_can_change_operator_result_type(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(Number));
        var right = SysExpr.Parameter(typeof(Number));
        var value = SysExpr.Parameter(typeof(long));
        var conversion = SysExpr.Lambda<Func<long, Number>>(SysExpr.New(typeof(Number).GetConstructor(new[] { typeof(long) }), value), value);
        var body = SysExpr.AddAssign(left, right, typeof(AssignmentConversionRegressionTests).GetMethod(nameof(CombineNumbers)), conversion);
        var expression = SysExpr.Lambda<Func<Number, Number, Number>>(body, left, right);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(53L, fast(new Number(5), new Number(3)).Value);
    }

    public void Ignored_conversion_effects_and_exception_writeback(TestContext t)
    {
        var box = new Box { Value = 5 };
        var calls = 0;
        Func<int, int> convert = number => { ++calls; if (calls == 2) throw new InvalidOperationException(); return number * 2; };
        var value = SysExpr.Parameter(typeof(int));
        var conversion = SysExpr.Lambda<Func<int, int>>(SysExpr.Invoke(SysExpr.Constant(convert), value), value);
        var body = SysExpr.AddAssign(SysExpr.Property(SysExpr.Constant(box), nameof(Box.Value)), SysExpr.Constant(3),
            typeof(AssignmentConversionRegressionTests).GetMethod(nameof(Combine)), conversion);
        var fast = SysExpr.Lambda<Action>(SysExpr.Block(body, SysExpr.Empty())).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        fast();
        t.AreEqual(106, box.Value);
        try { fast(); }
        catch (InvalidOperationException) { }
        t.AreEqual(2, calls);
        t.AreEqual(106, box.Value);
    }
}
