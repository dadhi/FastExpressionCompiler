using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReferenceInstanceCallRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Non_virtual_methods_throw_for_null_receivers(t);
        Null_receiver_arguments_are_evaluated_before_throwing(t);
        Non_virtual_property_accessors_check_the_receiver(t);
    }

    public sealed class Receiver
    {
        public void Empty() { }
        public int Constant() => 42;
        public int WithArgument(int value) => value;
        public int Value { get => 42; set { } }
    }

    public void Non_virtual_methods_throw_for_null_receivers(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Receiver));
        var emptyExpression = SysExpr.Lambda<Action<Receiver>>(SysExpr.Call(parameter, typeof(Receiver).GetMethod(nameof(Receiver.Empty))), parameter);
        var valueExpression = SysExpr.Lambda<Func<Receiver, int>>(SysExpr.Call(parameter, typeof(Receiver).GetMethod(nameof(Receiver.Constant))), parameter);
        var empty = emptyExpression.FromSysExpression().CompileFast(true);
        var value = valueExpression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(empty) || !t.IsNotNull(value)) return;
        t.Throws<NullReferenceException>(() => empty(null));
        t.Throws<NullReferenceException>(() => value(null));
        t.Throws<NullReferenceException>(() => emptyExpression.Compile()(null));
        t.Throws<NullReferenceException>(() => valueExpression.Compile()(null));
        empty(new Receiver());
        t.AreEqual(42, value(new Receiver()));
    }

    public void Null_receiver_arguments_are_evaluated_before_throwing(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Receiver));
        var argument = SysExpr.Parameter(typeof(Func<int>));
        var expression = SysExpr.Lambda<Func<Receiver, Func<int>, int>>(SysExpr.Call(parameter,
            typeof(Receiver).GetMethod(nameof(Receiver.WithArgument)), SysExpr.Invoke(argument)), parameter, argument);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var calls = 0;
        t.Throws<NullReferenceException>(() => fast(null, () => { ++calls; return 42; }));
        t.AreEqual(1, calls);
        t.AreEqual(42, fast(new Receiver(), () => { ++calls; return 42; }));
        t.AreEqual(2, calls);
    }

    public void Non_virtual_property_accessors_check_the_receiver(TestContext t)
    {
        var parameter = SysExpr.Parameter(typeof(Receiver));
        var getter = SysExpr.Lambda<Func<Receiver, int>>(SysExpr.Property(parameter, nameof(Receiver.Value)), parameter).FromSysExpression().CompileFast(true);
        var setter = SysExpr.Lambda<Action<Receiver>>(SysExpr.Block(SysExpr.Assign(SysExpr.Property(parameter, nameof(Receiver.Value)), SysExpr.Constant(1)), SysExpr.Empty()), parameter).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(getter) || !t.IsNotNull(setter)) return;
        t.Throws<NullReferenceException>(() => getter(null));
        t.Throws<NullReferenceException>(() => setter(null));
        t.AreEqual(42, getter(new Receiver()));
        setter(new Receiver());
    }
}
