using System;
#if LIGHT_EXPRESSION
using Expr = FastExpressionCompiler.LightExpression.Expression;
using Binding = FastExpressionCompiler.LightExpression.MemberBinding;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using Expr = System.Linq.Expressions.Expression;
using Binding = System.Linq.Expressions.MemberBinding;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct MemberInitializerSpillRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_bindings_spill_rhs_and_preserve_order(t);
        Struct_field_and_property_bindings_spill_rhs(t);
        Constructor_arguments_with_finally_preserve_order(t);
        Static_and_ignored_bindings_preserve_finally_effects(t);
        Binding_exception_runs_finally_and_stops_later_bindings(t);
    }
    public void Reference_bindings_spill_rhs_and_preserve_order(TestContext t)
    {
        for (var spill = 0; spill != 3; ++spill)
        {
            var bindings = new Binding[3];
            for (var i = 0; i != 3; ++i)
            {
                Expr value = Expr.Call(typeof(MemberInitializerSpillRegressionTests).GetMethod(nameof(Record)), Expr.Constant(i + 2));
                if (i == spill) value = Spill(value, "F" + i);
                bindings[i] = Expr.Bind(typeof(ReferenceModel).GetProperty(i == 0 ? "A" : i == 1 ? "B" : "C"), value);
            }
            var local = Expr.Variable(typeof(ReferenceModel));
            var init = Expr.MemberInit(Expr.New(typeof(ReferenceModel)), bindings);
            var body = Expr.Block(new[] { local }, Expr.Assign(local, init),
                Expr.Multiply(Expr.Add(Expr.Property(local, "A"), Expr.Property(local, "B")), Expr.Property(local, "C")));
            var fast = Expr.Lambda<Func<int>>(body).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace = "";
            t.AreEqual(20, fast());
            var expected = "C;";
            for (var i = 0; i != 3; ++i) expected += "R" + (i + 2) + ";" + (i == spill ? "F" + i + ";" : "") + (i == 0 ? "A" : i == 1 ? "B" : "C") + (i + 2) + ";";
            t.AreEqual(expected, Trace);
        }
    }
    public void Struct_field_and_property_bindings_spill_rhs(TestContext t)
    {
        foreach (var property in new[] { false, true })
        {
            var local = Expr.Variable(typeof(ValueModel));
            var member = property ? (System.Reflection.MemberInfo)typeof(ValueModel).GetProperty(nameof(ValueModel.Number)) : typeof(ValueModel).GetField(nameof(ValueModel.Value));
            var init = Expr.MemberInit(Expr.New(typeof(ValueModel)), Expr.Bind(member, Spill(Expr.Constant(42), "F")));
            var expression = Expr.Lambda<Func<ValueModel>>(Expr.Block(new[] { local }, Expr.Assign(local, init), local));
            var fast = expression.CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace = "";
            var result = fast();
            t.AreEqual(42, property ? result.Number : result.Value);
            t.AreEqual(property ? "F;I42;" : "F;", Trace);
        }
    }
    public void Constructor_arguments_with_finally_preserve_order(TestContext t)
    {
        var record = typeof(MemberInitializerSpillRegressionTests).GetMethod(nameof(Record));
        var constructor = Expr.New(typeof(ReferenceModel).GetConstructor(new[] { typeof(int), typeof(int) }),
            Expr.Call(record, Expr.Constant(2)), Spill(Expr.Call(record, Expr.Constant(3)), "Ctor"));
        var init = Expr.MemberInit(constructor, Expr.Bind(typeof(ReferenceModel).GetProperty(nameof(ReferenceModel.Number)), Spill(Expr.Constant(7), "Bind")));
        var fast = Expr.Lambda<Func<ReferenceModel>>(init).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var result = fast();
        t.AreEqual(2, result.Seed); t.AreEqual(3, result.Other); t.AreEqual(7, result.Number);
        t.AreEqual("R2;R3;Ctor;C2,3;Bind;I7;", Trace);
    }
    public void Static_and_ignored_bindings_preserve_finally_effects(TestContext t)
    {
        foreach (var type in new[] { typeof(ReferenceModel), typeof(ValueModel) })
        {
            var init = Expr.MemberInit(Expr.New(type), new Binding[] {
                Expr.Bind(type.GetField("Global"), Spill(Expr.Constant(5), "G")),
                Expr.Bind(type.GetField("Value"), Spill(Expr.Constant(6), "V")),
                Expr.Bind(type.GetProperty("Number"), Spill(Expr.Constant(7), "N")),
                Expr.Bind(type.GetProperty("GlobalProperty"), Spill(Expr.Constant(8), "P")) });
            var fast = Expr.Lambda<Action>(Expr.Block(init, Expr.Empty())).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace = "";
            fast();
            t.AreEqual((type == typeof(ReferenceModel) ? "C;" : "") + "G;V;N;I7;P;P8;", Trace);
            t.AreEqual(5, type.GetField("Global").GetValue(null));
            t.AreEqual(8, type.GetProperty("GlobalProperty").GetValue(null));
        }
    }
    public void Binding_exception_runs_finally_and_stops_later_bindings(TestContext t)
    {
        var init = Expr.MemberInit(Expr.New(typeof(ReferenceModel)),
            Expr.Bind(typeof(ReferenceModel).GetField("Value"), Spill(Expr.Call(typeof(MemberInitializerSpillRegressionTests).GetMethod(nameof(ThrowValue))), "F")),
            Expr.Bind(typeof(ReferenceModel).GetProperty("Number"), Expr.Constant(9)));
        var fast = Expr.Lambda<Func<ReferenceModel>>(init).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        t.Throws<InvalidOperationException>(() => fast());
        t.AreEqual("C;F;", Trace);
    }
    private static Expr Spill(Expr value, string label) => Expr.TryFinally(value, Expr.Call(typeof(MemberInitializerSpillRegressionTests).GetMethod(nameof(Finally)), Expr.Constant(label)));
    public static string Trace;
    public static int Record(int value) { Trace += "R" + value + ";"; return value; }
    public static void Finally(string label) => Trace += label + ";";
    public static int ThrowValue() => throw new InvalidOperationException();
    public class ReferenceModel
    {
        public int Seed, Other, Value;
        public static int Global;
        private static int _globalProperty;
        public static int GlobalProperty { get => _globalProperty; set { Trace += "P" + value + ";"; _globalProperty = value; } }
        private int _number, _a, _b, _c;
        public int Number { get => _number; set { Trace += "I" + value + ";"; _number = value; } }
        public int A { get => _a; set { Trace += "A" + value + ";"; _a = value; } }
        public int B { get => _b; set { Trace += "B" + value + ";"; _b = value; } }
        public int C { get => _c; set { Trace += "C" + value + ";"; _c = value; } }
        public ReferenceModel() { Trace += "C;"; }
        public ReferenceModel(int seed, int other) { Seed = seed; Other = other; Trace += "C" + seed + "," + other + ";"; }
    }
    public struct ValueModel
    {
        public int Value;
        public static int Global;
        private static int _globalProperty;
        public static int GlobalProperty { get => _globalProperty; set { Trace += "P" + value + ";"; _globalProperty = value; } }
        private int _number;
        public int Number { get => _number; set { Trace += "I" + value + ";"; _number = value; } }
    }
}
