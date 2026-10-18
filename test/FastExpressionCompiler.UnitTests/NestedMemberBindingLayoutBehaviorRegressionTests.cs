using System;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
using Lambda = FastExpressionCompiler.LightExpression.LambdaExpression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
using Lambda = System.Linq.Expressions.LambdaExpression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct NestedMemberBindingLayoutBehaviorRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Struct_member_binding_spill_preserves_runtime_result(t);
        Struct_list_binding_spill_preserves_null_receiver_exception(t);
    }

    public void Struct_member_binding_spill_preserves_runtime_result(TestContext t)
    {
        // StackSpillerTests.Spill_RefInstance_MemberInit_MemberBind's exact expression form.
        var local = E.Parameter(typeof(ValueBar));
        var initialization = E.MemberInit(
            E.New(typeof(ValueBar).GetConstructor(new[] { typeof(Baz) }), E.New(typeof(Baz))),
            E.MemberBind(typeof(ValueBar).GetProperty(nameof(ValueBar.Baz2)),
                E.Bind(typeof(Baz).GetField(nameof(Baz.Foo)), E.TryFinally(E.Constant(42), E.Empty()))));
        var expression = E.Lambda<Func<ValueBar>>(E.Block(new[] { local }, E.Assign(local, initialization), local));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var actual = fast();
        t.AreEqual(42, actual.Baz2.Foo);
        t.AreEqual(0, actual.Xs.Count);
#if !NETFRAMEWORK
        // Framework's System compiler rejects Try expressions inside value-type member initializers.
        var expected = ((Func<ValueBar>)ToSystem(expression).Compile())();
        t.AreEqual(expected.Baz2.Foo, actual.Baz2.Foo);
        t.AreEqual(expected.Xs.Count, actual.Xs.Count);
#endif
    }

    public void Struct_list_binding_spill_preserves_null_receiver_exception(TestContext t)
    {
        // The original factory constructs default(ValueBar), whose getter-only Xs is null.
        var local = E.Parameter(typeof(ValueBar));
        var initialization = E.MemberInit(E.New(typeof(ValueBar)),
            E.ListBind(typeof(ValueBar).GetProperty(nameof(ValueBar.Xs)),
                E.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add)),
                    E.TryFinally(E.Constant(42), E.Empty()))));
        var expression = E.Lambda<Func<ValueBar>>(E.Block(new[] { local }, E.Assign(local, initialization), local));
        var fast = expression.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.Throws<NullReferenceException>(() => fast());
#if !NETFRAMEWORK
        var system = (Func<ValueBar>)ToSystem(expression).Compile();
        t.Throws<NullReferenceException>(() => system());
#endif
    }

    private static System.Linq.Expressions.LambdaExpression ToSystem(Lambda expression)
    {
#if LIGHT_EXPRESSION
        return expression.ToLambdaExpression();
#else
        return expression;
#endif
    }

    public struct ValueBar
    {
        public ValueBar(Baz baz) { Baz = 0; Foo = 0; Baz2 = baz; Xs = new List<int>(); }
        public int Baz;
        public int Foo { get; set; }
        public Baz Baz2 { get; }
        public List<int> Xs { get; }
        public int this[int x] { get { return Foo = x; } }
        public void Qux(int x) => Foo = x;
    }
    public class Baz
    {
        public Baz() { Foo = 0; }
        public int Foo;
        public int this[int x] { get { return Foo = x; } }
        public void Qux(int x) => Foo = x;
    }
}
