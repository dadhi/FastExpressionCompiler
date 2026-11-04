using System;
using System.Collections.Generic;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CompoundAssignmentExceptionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Nullable_targets_accept_exception_block_rhs(t);
        Target_getter_rhs_conversion_and_setter_keep_order(t);
        Ref_parameter_reads_before_exception_block_rhs(t);
        Throwing_rhs_runs_finally_without_writeback(t);
    }

    public sealed class Box
    {
        public int? Stored;
        public int Gets, Sets;
        public List<string> Events;
        public int? Value { get { ++Gets; Events?.Add("get"); return Stored; } set { ++Sets; Events?.Add("set"); Stored = value; } }
        public int? this[int index] { get { ++Gets; Events?.Add("get"); return Stored; } set { ++Sets; Events?.Add("set"); Stored = value; } }
    }
    public delegate int RefInt(ref int value, Func<int> right);
    public delegate int? RefNullableInt(ref int? value, Func<int?> right);
    public static int? AddNullable(int? left, int? right) => left + right;

    public void Nullable_targets_accept_exception_block_rhs(TestContext t)
    {
        foreach (var kind in new[] { "field", "property", "index", "array", "matrix" })
        foreach (var left in new int?[] { null, 5 })
        foreach (var right in new int?[] { null, 3 })
        {
            var box = new Box { Stored = left };
            var array = new[] { left };
            var matrix = new int?[,] { { left } };
            var receivers = 0;
            var indexes = 0;
            var rightCalls = 0;
            Func<Box> receiver = () => { ++receivers; return box; };
            Func<int> index = () => { ++indexes; return 0; };
            Func<int?> rhs = () => { ++rightCalls; return right; };
            var obj = SysExpr.Invoke(SysExpr.Constant(receiver));
            var idx = SysExpr.Invoke(SysExpr.Constant(index));
            System.Linq.Expressions.Expression target = kind switch
            {
                "field" => SysExpr.Field(obj, nameof(Box.Stored)),
                "property" => SysExpr.Property(obj, nameof(Box.Value)),
                "index" => SysExpr.MakeIndex(obj, typeof(Box).GetProperty("Item"), new[] { idx }),
                "array" => SysExpr.ArrayAccess(SysExpr.Constant(array), idx),
                _ => SysExpr.ArrayAccess(SysExpr.Constant(matrix), idx, SysExpr.Constant(0))
            };
            var body = SysExpr.AddAssign(target, SysExpr.TryCatch(SysExpr.Invoke(SysExpr.Constant(rhs)),
                SysExpr.Catch(typeof(InvalidOperationException), SysExpr.Default(typeof(int?)))));
            var fast = SysExpr.Lambda<Func<int?>>(body).FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(left + right, fast());
            t.AreEqual(left + right, kind == "array" ? array[0] : kind == "matrix" ? matrix[0, 0] : box.Stored);
            t.AreEqual(1, rightCalls);
            t.AreEqual(kind == "array" || kind == "matrix" ? 0 : 1, receivers);
            t.AreEqual(kind == "field" || kind == "property" ? 0 : 1, indexes);
            if (kind == "property" || kind == "index") { t.AreEqual(1, box.Gets); t.AreEqual(1, box.Sets); }
        }
    }

    public void Target_getter_rhs_conversion_and_setter_keep_order(TestContext t)
    {
        var events = new List<string>();
        var box = new Box { Stored = 5, Events = events };
        Func<Box> receiver = () => { events.Add("receiver"); return box; };
        Func<int> index = () => { events.Add("index"); return 0; };
        Func<int?> right = () => { events.Add("right"); box.Stored = 100; return 3; };
        Func<int?, int?> convert = n => { events.Add("convert"); return n * 2; };
        var value = SysExpr.Parameter(typeof(int?));
        var target = SysExpr.MakeIndex(SysExpr.Invoke(SysExpr.Constant(receiver)), typeof(Box).GetProperty("Item"),
            new[] { SysExpr.Invoke(SysExpr.Constant(index)) });
        var rhs = SysExpr.TryCatch(SysExpr.Invoke(SysExpr.Constant(right)),
            SysExpr.Catch(typeof(Exception), SysExpr.Default(typeof(int?))));
        var conversion = SysExpr.Lambda<Func<int?, int?>>(SysExpr.Invoke(SysExpr.Constant(convert), value), value);
        var body = SysExpr.AddAssign(target, rhs, typeof(CompoundAssignmentExceptionRegressionTests).GetMethod(nameof(AddNullable)), conversion);
        var fast = SysExpr.Lambda<Func<int?>>(body).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual((int?)16, fast());
        t.AreEqual((int?)16, box.Stored);
        t.AreEqual("receiver,index,get,right,convert,set", string.Join(",", events));
    }

    public void Ref_parameter_reads_before_exception_block_rhs(TestContext t)
    {
        var left = SysExpr.Parameter(typeof(int).MakeByRefType());
        var right = SysExpr.Parameter(typeof(Func<int>));
        var rhs = SysExpr.TryCatch(SysExpr.Invoke(right), SysExpr.Catch(typeof(Exception), SysExpr.Constant(0)));
        var fast = SysExpr.Lambda<RefInt>(SysExpr.AddAssign(left, rhs), left, right).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var value = 5;
        t.AreEqual(8, fast(ref value, () => { value = 100; return 3; }));
        t.AreEqual(8, value);
    }

    public void Throwing_rhs_runs_finally_without_writeback(TestContext t)
    {
        var box = new Box { Stored = 5 };
        var finalizers = 0;
        Action finalize = () => ++finalizers;
        var rhs = SysExpr.TryFinally(SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(int?)),
            SysExpr.Invoke(SysExpr.Constant(finalize)));
        var body = SysExpr.AddAssign(SysExpr.Property(SysExpr.Constant(box), nameof(Box.Value)), rhs);
        var fast = SysExpr.Lambda<Action>(SysExpr.Block(body, SysExpr.Empty())).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        try { fast(); }
        catch (InvalidOperationException) { }
        t.AreEqual(1, box.Gets);
        t.AreEqual(0, box.Sets);
        t.AreEqual((int?)5, box.Stored);
        t.AreEqual(1, finalizers);
    }
}
