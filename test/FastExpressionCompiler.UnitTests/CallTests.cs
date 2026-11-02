using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public class CallTests : ITest
{
    public int Run()
    {
        GetType_on_int_parameter();
        GetType_on_local_int();
        GetType_on_nullable();
        GetType_on_enum();
        Nonvirtual_struct_method();
        Virtual_struct_method();
        Inherited_virtual_struct_method();
        Inherited_ValueType_method();
        Enum_HasFlag();
        Enum_ToString();
        Interface_method_on_struct();
        Virtual_class_method();
        Ignored_call_result();
        Call_result_as_argument();
        Call_result_field_access();
        Void_call();
        return 16;
    }

    public void GetType_on_int_parameter()
    {
        var value = Parameter(typeof(int), "value");
        var expression = Lambda<Func<int, Type>>(
            Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        Asserts.AreEqual(typeof(int), expression.CompileSys()(42));
        Asserts.AreEqual(typeof(int), expression.CompileFast(true)(42));
    }

    public void GetType_on_local_int()
    {
        var value = Variable(typeof(int), "a");
        var expression = Lambda<Func<Type>>(Block(new[] { value },
            Assign(value, Constant(1)), Call(value, typeof(object).GetMethod(nameof(GetType)))));

        Asserts.AreEqual(typeof(int), expression.CompileSys()());
        Asserts.AreEqual(typeof(int), expression.CompileFast(true)());
    }

    public void GetType_on_nullable()
    {
        var value = Parameter(typeof(int?), "value");
        var expression = Lambda<Func<int?, Type>>(
            Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        var system = expression.CompileSys();
        var fast = expression.CompileFast(true);
        Asserts.AreEqual(typeof(int), system(42));
        Asserts.AreEqual(typeof(int), fast(42));
        Asserts.Throws<NullReferenceException>(() => system(null));
        Asserts.Throws<NullReferenceException>(() => fast(null));
    }

    public void GetType_on_enum()
    {
        var value = Parameter(typeof(Bits), "value");
        var expression = Lambda<Func<Bits, Type>>(
            Call(value, typeof(object).GetMethod(nameof(GetType))), value);

        Asserts.AreEqual(typeof(Bits), expression.CompileSys()(Bits.A));
        Asserts.AreEqual(typeof(Bits), expression.CompileFast(true)(Bits.A));
    }

    public void Nonvirtual_struct_method()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, Result>>(
            Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Constant(1)), counter);

        Asserts.AreEqual(43, expression.CompileSys()(new Counter { Value = 42 }).Value);
        Asserts.AreEqual(43, expression.CompileFast(true)(new Counter { Value = 42 }).Value);
    }

    public void Virtual_struct_method()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, string>>(
            Call(counter, typeof(Counter).GetMethod(nameof(ToString))), counter);

        Asserts.AreEqual("counter", expression.CompileSys()(new Counter()));
        Asserts.AreEqual("counter", expression.CompileFast(true)(new Counter()));
    }

    public void Inherited_virtual_struct_method()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, string>>(
            Call(counter, typeof(object).GetMethod(nameof(ToString))), counter);

        Asserts.AreEqual("counter", expression.CompileSys()(new Counter()));
        Asserts.AreEqual("counter", expression.CompileFast(true)(new Counter()));
    }

    public void Inherited_ValueType_method()
    {
        var value = Parameter(typeof(PlainValue), "value");
        var expression = Lambda<Func<PlainValue, string>>(
            Call(value, typeof(ValueType).GetMethod(nameof(ToString))), value);

        Asserts.AreEqual(typeof(PlainValue).FullName, expression.CompileSys()(new PlainValue()));
        Asserts.AreEqual(typeof(PlainValue).FullName, expression.CompileFast(true)(new PlainValue()));
    }

    public void Enum_HasFlag()
    {
        var value = Parameter(typeof(Bits), "value");
        var expression = Lambda<Func<Bits, bool>>(Call(value,
            typeof(Enum).GetMethod(nameof(Enum.HasFlag)), Constant(Bits.B, typeof(Enum))), value);

        var system = expression.CompileSys();
        var fast = expression.CompileFast(true);
        Asserts.IsTrue(system(Bits.A | Bits.B));
        Asserts.IsTrue(fast(Bits.A | Bits.B));
        Asserts.IsFalse(system(Bits.A));
        Asserts.IsFalse(fast(Bits.A));
    }

    public void Enum_ToString()
    {
        var value = Parameter(typeof(Bits), "value");
        var expression = Lambda<Func<Bits, string>>(
            Call(value, typeof(Enum).GetMethod(nameof(ToString), Type.EmptyTypes)), value);

        Asserts.AreEqual("A, B", expression.CompileSys()(Bits.A | Bits.B));
        Asserts.AreEqual("A, B", expression.CompileFast(true)(Bits.A | Bits.B));
    }

    public void Interface_method_on_struct()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, Result>>(
            Call(counter, typeof(IAdder).GetMethod(nameof(IAdder.Add)), Constant(1)), counter);

        Asserts.AreEqual(143, expression.CompileSys()(new Counter { Value = 42 }).Value);
        Asserts.AreEqual(143, expression.CompileFast(true)(new Counter { Value = 42 }).Value);
    }

    public void Virtual_class_method()
    {
        var target = Parameter(typeof(Target), "target");
        var expression = Lambda<Func<Target, int>>(
            Call(target, typeof(Target).GetMethod(nameof(Target.GetValue))), target);

        Asserts.AreEqual(2, expression.CompileSys()(new DerivedTarget()));
        Asserts.AreEqual(2, expression.CompileFast(true)(new DerivedTarget()));
    }

    public void Ignored_call_result()
    {
        var counter = Variable(typeof(Counter), "counter");
        var expression = Lambda<Func<int>>(Block(new[] { counter },
            Assign(counter, Default(typeof(Counter))),
            Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Constant(1)),
            Field(counter, nameof(Counter.Value))));

        Asserts.AreEqual(1, expression.CompileSys()());
        Asserts.AreEqual(1, expression.CompileFast(true)());
    }

    public void Call_result_as_argument()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, int>>(
            Call(typeof(CallTests).GetMethod(nameof(ReadResult)),
                Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Constant(1))), counter);

        Asserts.AreEqual(43, expression.CompileSys()(new Counter { Value = 42 }));
        Asserts.AreEqual(43, expression.CompileFast(true)(new Counter { Value = 42 }));
    }

    public void Call_result_field_access()
    {
        var counter = Parameter(typeof(Counter), "counter");
        var expression = Lambda<Func<Counter, int>>(
            Field(Call(counter, typeof(Counter).GetMethod(nameof(Counter.Add)), Constant(1)),
                nameof(Result.Value)), counter);

        Asserts.AreEqual(43, expression.CompileSys()(new Counter { Value = 42 }));
        Asserts.AreEqual(43, expression.CompileFast(true)(new Counter { Value = 42 }));
    }

    public void Void_call()
    {
        var counter = Variable(typeof(Counter), "counter");
        var expression = Lambda<Func<int>>(Block(new[] { counter },
            Assign(counter, Default(typeof(Counter))),
            Call(counter, typeof(Counter).GetMethod(nameof(Counter.Increment))),
            Field(counter, nameof(Counter.Value))));

        Asserts.AreEqual(1, expression.CompileSys()());
        Asserts.AreEqual(1, expression.CompileFast(true)());
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
