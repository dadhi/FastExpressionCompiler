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
public struct StaticMemberInitializerRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Static_field_binding_retains_constructed_reference(t);
        Mixed_static_and_instance_bindings_preserve_order(t);
        Static_struct_bindings_preserve_constructed_value(t);
        Ignored_initializers_preserve_constructor_and_binding_effects(t);
    }
    public void Static_field_binding_retains_constructed_reference(TestContext t)
    {
        var fast = Expr.Lambda<Func<ReferenceModel>>(Create(typeof(ReferenceModel), true)).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var result = fast();
        if (!t.IsNotNull(result)) return;
        t.AreEqual(3, result.Seed);
        t.AreEqual(3, result.Value);
        t.AreEqual(5, ReferenceModel.Global);
        t.AreEqual("R3;C3;R5;", Trace);
    }
    public void Mixed_static_and_instance_bindings_preserve_order(TestContext t)
    {
        var fast = Expr.Lambda<Func<ReferenceModel>>(Create(typeof(ReferenceModel), false)).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var result = fast();
        t.AreEqual(3, result.Seed);
        t.AreEqual(6, result.Value);
        t.AreEqual(5, ReferenceModel.Global);
        t.AreEqual(7, ReferenceModel.GlobalProperty);
        t.AreEqual(8, result.Number);
        t.AreEqual("R3;C3;R5;R7;P7;R8;I8;", Trace);
    }
    public void Static_struct_bindings_preserve_constructed_value(TestContext t)
    {
        var fast = Expr.Lambda<Func<ValueModel>>(Create(typeof(ValueModel), false)).CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        Trace = "";
        var result = fast();
        t.AreEqual(3, result.Seed);
        t.AreEqual(6, result.Value);
        t.AreEqual(5, ValueModel.Global);
        t.AreEqual(7, ValueModel.GlobalProperty);
        t.AreEqual(8, result.Number);
        t.AreEqual("R3;S3;R5;R7;P7;R8;I8;", Trace);
    }
    public void Ignored_initializers_preserve_constructor_and_binding_effects(TestContext t)
    {
        foreach (var type in new[] { typeof(ReferenceModel), typeof(ValueModel) })
        {
            var fast = Expr.Lambda<Action>(Expr.Block(Create(type, false), Expr.Empty())).CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            Trace = "";
            fast();
            t.AreEqual(type == typeof(ReferenceModel) ? "R3;C3;R5;R7;P7;R8;I8;" : "R3;S3;R5;R7;P7;R8;I8;", Trace);
        }
    }
    private static Expr Create(Type type, bool onlyStaticField)
    {
        var record = typeof(StaticMemberInitializerRegressionTests).GetMethod(nameof(Record));
        var constructor = Expr.New(type.GetConstructor(new[] { typeof(int) }), Expr.Call(record, Expr.Constant(3)));
        var global = Expr.Bind(type.GetField(nameof(ReferenceModel.Global)), Expr.Call(record, Expr.Constant(5)));
        if (onlyStaticField) return Expr.MemberInit(constructor, global);
        return Expr.MemberInit(constructor, new Binding[] { global,
            Expr.Bind(type.GetField(nameof(ReferenceModel.Value)), Expr.Constant(6)),
            Expr.Bind(type.GetProperty(nameof(ReferenceModel.GlobalProperty)), Expr.Call(record, Expr.Constant(7))),
            Expr.Bind(type.GetProperty(nameof(ReferenceModel.Number)), Expr.Call(record, Expr.Constant(8))) });
    }
    public static string Trace;
    public static int Record(int value) { Trace += "R" + value + ";"; return value; }
    public class ReferenceModel
    {
        public static int Global;
        private static int _globalProperty;
        public static int GlobalProperty { get => _globalProperty; set { Trace += "P" + value + ";"; _globalProperty = value; } }
        public int Seed, Value;
        private int _number;
        public int Number { get => _number; set { Trace += "I" + value + ";"; _number = value; } }
        public ReferenceModel(int seed) { Trace += "C" + seed + ";"; Seed = seed; Value = seed; }
    }
    public struct ValueModel
    {
        public static int Global;
        private static int _globalProperty;
        public static int GlobalProperty { get => _globalProperty; set { Trace += "P" + value + ";"; _globalProperty = value; } }
        public int Seed, Value;
        private int _number;
        public int Number { get => _number; set { Trace += "I" + value + ";"; _number = value; } }
        public ValueModel(int seed) { Trace += "S" + seed + ";"; Seed = seed; Value = seed; _number = 0; }
    }
}
