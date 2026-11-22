using System;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#elif !FLAT_EXPRESSION
using Expr = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#else
namespace FastExpressionCompiler.FlatExpression.UnitTests;
#endif

public struct CallTests : ITestX
{
    public void Run(TestRun t)
    {
        GetType_on_int_parameter(t);
        GetType_on_local_int(t);
        GetType_on_nullable(t);
        GetType_on_enum(t);
        Nonvirtual_struct_method(t);
        Virtual_struct_method(t);
        Inherited_virtual_struct_method(t);
        Inherited_ValueType_method(t);
        Enum_HasFlag(t);
        Enum_ToString(t);
        Interface_method_on_struct(t);
        Virtual_class_method(t);
        Ignored_call_result(t);
        Call_result_as_argument(t);
        Call_result_field_access(t);
        Void_call(t);
    }

#if FLAT_EXPRESSION
    ExprTree Expr;
    public ExprTree ExprTree => Expr;
    public void ResetExpr() => Expr = default;
#else
    public void ResetExpr() { }
#endif

    public void GetType_on_int_parameter(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(int), "value");
        var expression = Expr.Lambda<Func<int, Type>>(
            Expr.Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        t.AreEqual(typeof(int), expression.CompileSys(in this)(42));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(typeof(int), fast(42));
    }

    public void GetType_on_local_int(TestContext t)
    {
        ResetExpr();
        var value = Expr.Variable(typeof(int), "a");
        var expression = Expr.Lambda<Func<Type>>(Expr.Block(new[] { value },
            Expr.Assign(value, Expr.Constant(1)), Expr.Call(value, typeof(object).GetMethod(nameof(GetType)))));

        t.AreEqual(typeof(int), expression.CompileSys(in this)());
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(typeof(int), fast());
    }

    public void GetType_on_nullable(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(int?), "value");
        var expression = Expr.Lambda<Func<int?, Type>>(
            Expr.Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        var system = expression.CompileSys(in this);
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(typeof(int), system(42));
        t.AreEqual(typeof(int), fast(42));
        t.Throws<NullReferenceException>(() => system(null));
        t.Throws<NullReferenceException>(() => fast(null));
    }

    public void GetType_on_enum(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(Bits), "value");
        var expression = Expr.Lambda<Func<Bits, Type>>(
            Expr.Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        t.AreEqual(typeof(Bits), expression.CompileSys(in this)(Bits.A));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(typeof(Bits), fast(Bits.A));
    }

    public void Nonvirtual_struct_method(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, Result>>(
            Expr.Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Expr.Constant(1)), counter);

        t.AreEqual(43, expression.CompileSys(in this)(new Counter { Value = 42 }).Value);
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(43, fast(new Counter { Value = 42 }).Value);
    }

    public void Virtual_struct_method(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, string>>(
            Expr.Call(counter, typeof(Counter).GetMethod(nameof(ToString))), counter);

        t.AreEqual("counter", expression.CompileSys(in this)(new Counter()));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual("counter", fast(new Counter()));
    }

    public void Inherited_virtual_struct_method(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, string>>(
            Expr.Call(counter, typeof(object).GetMethod(nameof(ToString))), counter);

        t.AreEqual("counter", expression.CompileSys(in this)(new Counter()));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual("counter", fast(new Counter()));
    }

    public void Inherited_ValueType_method(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(PlainValue), "value");
        var expression = Expr.Lambda<Func<PlainValue, string>>(
            Expr.Call(value, typeof(ValueType).GetMethod(nameof(ToString))), value);

        t.AreEqual(typeof(PlainValue).FullName, expression.CompileSys(in this)(new PlainValue()));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(typeof(PlainValue).FullName, fast(new PlainValue()));
    }

    public void Enum_HasFlag(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(Bits), "value");
        var expression = Expr.Lambda<Func<Bits, bool>>(Expr.Call(value,
            typeof(Enum).GetMethod(nameof(Enum.HasFlag)), Expr.Constant(Bits.B, typeof(Enum))), value);

        var system = expression.CompileSys(in this);
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.IsTrue(system(Bits.A | Bits.B));
        t.IsTrue(fast(Bits.A | Bits.B));
        t.IsFalse(system(Bits.A));
        t.IsFalse(fast(Bits.A));
    }

    public void Enum_ToString(TestContext t)
    {
        ResetExpr();
        var value = Expr.Parameter(typeof(Bits), "value");
        var expression = Expr.Lambda<Func<Bits, string>>(
            Expr.Call(value, typeof(Enum).GetMethod(nameof(ToString), Type.EmptyTypes)), value);

        t.AreEqual("A, B", expression.CompileSys(in this)(Bits.A | Bits.B));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual("A, B", fast(Bits.A | Bits.B));
    }

    public void Interface_method_on_struct(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, Result>>(
            Expr.Call(counter, typeof(IAdder).GetMethod(nameof(IAdder.Add)), Expr.Constant(1)), counter);

        t.AreEqual(143, expression.CompileSys(in this)(new Counter { Value = 42 }).Value);
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(143, fast(new Counter { Value = 42 }).Value);
    }

    public void Virtual_class_method(TestContext t)
    {
        ResetExpr();
        var target = Expr.Parameter(typeof(Target), "target");
        var expression = Expr.Lambda<Func<Target, int>>(
            Expr.Call(target, typeof(Target).GetMethod(nameof(Target.GetValue))), target);

        t.AreEqual(2, expression.CompileSys(in this)(new DerivedTarget()));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(2, fast(new DerivedTarget()));
    }

    public void Ignored_call_result(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Variable(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<int>>(Expr.Block(new[] { counter },
            Expr.Assign(counter, Expr.Default(typeof(Counter))),
            Expr.Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Expr.Constant(1)),
            Expr.Field(counter, typeof(Counter).GetField(nameof(Counter.Value)))));

        t.AreEqual(1, expression.CompileSys(in this)());
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(1, fast());
    }

    public void Call_result_as_argument(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, int>>(
            Expr.Call(typeof(CallTests).GetMethod(nameof(ReadResult)),
                Expr.Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Expr.Constant(1))), counter);

        t.AreEqual(43, expression.CompileSys(in this)(new Counter { Value = 42 }));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(43, fast(new Counter { Value = 42 }));
    }

    public void Call_result_field_access(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Parameter(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<Counter, int>>(
            Expr.Field(Expr.Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Expr.Constant(1)),
                typeof(Result).GetField(nameof(Result.Value))), counter);

        t.AreEqual(43, expression.CompileSys(in this)(new Counter { Value = 42 }));
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(43, fast(new Counter { Value = 42 }));
    }

    public void Void_call(TestContext t)
    {
        ResetExpr();
        var counter = Expr.Variable(typeof(Counter), "counter");
        var expression = Expr.Lambda<Func<int>>(Expr.Block(new[] { counter },
            Expr.Assign(counter, Expr.Default(typeof(Counter))),
            Expr.Call(counter, typeof(Counter).GetMethod(nameof(Counter.Increment))),
            Expr.Field(counter, typeof(Counter).GetField(nameof(Counter.Value)))));

        t.AreEqual(1, expression.CompileSys(in this)());
        var fast = expression.CompileFast(true, in this);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(1, fast());
    }

    [Flags]
    public enum Bits { A = 1, B = 2 }
    public struct PlainValue { }
    public struct Result { public int Value; }
    public interface IAdder { Result Add(int amount); }

    public struct Counter : IAdder
    {
        public int Value;
        public Result Add(int amount) => new Result { Value = Value += amount };
        Result IAdder.Add(int amount) => new Result { Value = Value += amount + 100 };
        public void Increment() => ++Value;
        public override string ToString() => "counter";
    }

    public class Target { public virtual int GetValue() => 1; }
    public class DerivedTarget : Target { public override int GetValue() => 2; }
    public static int ReadResult(Result result) => result.Value;
}
