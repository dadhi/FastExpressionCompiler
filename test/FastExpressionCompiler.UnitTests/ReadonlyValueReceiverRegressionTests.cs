using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReadonlyValueReceiverRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Readonly_fields_use_defensive_receiver_copies(t);
        Static_readonly_fields_use_defensive_receiver_copies(t);
        Nested_readonly_field_receivers_use_copies(t);
        Assignment_receiver_is_evaluated_once(t);
    }

    public struct Counter
    {
        public int Value;
        public int Next() => Value++;
    }
    public struct Nested { public Counter Counter; }
    public sealed class Box
    {
        public Counter Writable;
        public readonly Counter Readonly;
        public readonly Nested Nested;
        public static readonly Counter StaticReadonly;
        public Box() { Readonly = default; Nested = default; }
        static Box() { StaticReadonly = default; }
    }

    public void Readonly_fields_use_defensive_receiver_copies(TestContext t)
    {
        foreach (var readonlyField in new[] { false, true })
        {
            var parameter = SysExpr.Parameter(typeof(Box));
            var member = SysExpr.Field(parameter, readonlyField ? nameof(Box.Readonly) : nameof(Box.Writable));
            var expression = SysExpr.Lambda<Func<Box, int>>(SysExpr.Call(member, typeof(Counter).GetMethod(nameof(Counter.Next))), parameter);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var system = expression.Compile();
            var expectedBox = new Box();
            var actualBox = new Box();
            for (var i = 0; i < 3; ++i)
            {
                var actual = fast(actualBox);
                t.AreEqual(readonlyField ? 0 : i, actual);
                t.AreEqual(system(expectedBox), actual);
            }
            t.AreEqual(0, actualBox.Readonly.Value);
            t.AreEqual(readonlyField ? 0 : 3, actualBox.Writable.Value);
        }
    }

    public void Static_readonly_fields_use_defensive_receiver_copies(TestContext t)
    {
        var member = SysExpr.Field(null, typeof(Box).GetField(nameof(Box.StaticReadonly)));
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.Call(member, typeof(Counter).GetMethod(nameof(Counter.Next))));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var expected = expression.Compile();
        t.AreEqual(0, fast());
        t.AreEqual(0, fast());
        t.AreEqual(expected(), fast());
        t.AreEqual(0, Box.StaticReadonly.Value);
    }

    public void Nested_readonly_field_receivers_use_copies(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Box));
        var member = SysExpr.Field(SysExpr.Field(parameter, nameof(Box.Nested)), nameof(Nested.Counter));
        var expression = SysExpr.Lambda<Func<Box, int>>(SysExpr.Call(member, typeof(Counter).GetMethod(nameof(Counter.Next))), parameter);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var box = new Box();
        var expected = expression.Compile();
        t.AreEqual(0, fast(box));
        t.AreEqual(0, fast(box));
        t.AreEqual(expected(box), fast(box));
        t.AreEqual(0, box.Nested.Counter.Value);
    }

    public void Assignment_receiver_is_evaluated_once(TestContext t)
    {
        var factory = SysExpr.Parameter(typeof(Func<Box>));
        var local = SysExpr.Variable(typeof(Box));
        var receiver = SysExpr.Block(new[] { local }, SysExpr.Assign(local, SysExpr.Invoke(factory)), local);
        var expression = SysExpr.Lambda<Func<Func<Box>, int>>(SysExpr.Call(SysExpr.Field(receiver, nameof(Box.Readonly)),
            typeof(Counter).GetMethod(nameof(Counter.Next))), factory);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        var box = new Box();
        t.AreEqual(0, fast(() => { ++calls; return box; }));
        t.AreEqual(1, calls);
        t.AreEqual(0, box.Readonly.Value);
        t.AreEqual(expression.Compile()(() => box), fast(() => { ++calls; return box; }));
        t.AreEqual(2, calls);
    }
}
