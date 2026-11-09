using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct StaticFieldAddressRegressionTests : ITestX
{
    public void Run(TestRun t) { Static_fields_supply_typed_references(t); Static_struct_receivers_respect_readonly(t); Addresses_survive_exception_arguments(t); }
    public static int Number;
    public static string Text;
    public static readonly int ReadonlyNumber = 3;
    public struct Mutable { public int Value; public int Increment() => ++Value; public int Add(int amount) => Value += amount; }
    public static Mutable Value;
    public static readonly Mutable ReadonlyValue = new Mutable { Value = 3 };
    public static int Add(ref int value) => ++value;
    public static int Replace(ref string value) { value = "replaced"; return value.Length; }
    public void Static_fields_supply_typed_references(TestContext t)
    {
        foreach (var name in new[] { nameof(Number), nameof(Text), nameof(ReadonlyNumber) })
        {
            var method = typeof(StaticFieldAddressRegressionTests).GetMethod(name == nameof(Text) ? nameof(Replace) : nameof(Add));
            var tree = S.Lambda<Func<int>>(S.Call(method, S.Field(null, typeof(StaticFieldAddressRegressionTests), name)));
            var system = tree.Compile();
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Number = 3; Text = "original";
            var expected = system(); var number = Number; var text = Text;
            Number = 3; Text = "original";
            t.AreEqual(expected, fast()); t.AreEqual(number, Number); t.AreEqual(text, Text); t.AreEqual(3, ReadonlyNumber);
        }
    }
    public void Static_struct_receivers_respect_readonly(TestContext t)
    {
        foreach (var name in new[] { nameof(Value), nameof(ReadonlyValue) })
        {
            var tree = S.Lambda<Func<int>>(S.Call(S.Field(null, typeof(StaticFieldAddressRegressionTests), name), typeof(Mutable).GetMethod(nameof(Mutable.Increment))));
            var system = tree.Compile(); var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Value = new Mutable { Value = 3 }; var expected = system(); var changed = Value.Value;
            Value = new Mutable { Value = 3 }; t.AreEqual(expected, fast()); t.AreEqual(changed, Value.Value); t.AreEqual(3, ReadonlyValue.Value);
        }
    }
    public void Addresses_survive_exception_arguments(TestContext t)
    {
        var rhs = S.TryFinally(S.Constant(3), S.Empty());
        var tree = S.Lambda<Func<int>>(S.Call(S.Field(null, typeof(StaticFieldAddressRegressionTests), nameof(Value)), typeof(Mutable).GetMethod(nameof(Mutable.Add)), rhs));
        var fast = tree.FromSysExpression().CompileFast(true); if (!t.IsNotNull(fast)) return;
        Value = new Mutable { Value = 4 }; t.AreEqual(7, fast()); t.AreEqual(7, Value.Value);
    }
}
