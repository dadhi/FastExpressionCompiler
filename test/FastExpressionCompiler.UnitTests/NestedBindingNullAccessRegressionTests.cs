using System;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
using Binding = FastExpressionCompiler.LightExpression.MemberBinding;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
using Binding = System.Linq.Expressions.MemberBinding;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NestedBindingNullAccessRegressionTests : ITestX
{
    public static string Trace;
    public class Node { public int Value { get => 0; set => Trace += "set;"; } }
    public class Host
    {
        public Host() { Trace += "new;"; }
        public Node NullNode { get { Trace += "node;"; return null; } }
        public List<int> NullList { get { Trace += "list;"; return null; } }
    }
    public static int Read(int value) { Trace += "arg" + value + ";"; return value; }
    public static void Finally() { Trace += "finally;"; }

    public void Run(TestRun t) => Null_receivers_evaluate_the_value_before_throwing(t);

    public void Null_receivers_evaluate_the_value_before_throwing(TestContext t)
    {
        foreach (var collection in new[] { false, true })
        foreach (var spill in new[] { false, true })
        {
            E value = E.Call(typeof(NestedBindingNullAccessRegressionTests).GetMethod(nameof(Read)), E.Constant(1));
            if (spill) value = E.TryFinally(value, E.Call(typeof(NestedBindingNullAccessRegressionTests).GetMethod(nameof(Finally))));
            var init = collection
                ? E.ListBind(typeof(Host).GetProperty(nameof(Host.NullList)),
                    E.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add)), value),
                    E.ElementInit(typeof(List<int>).GetMethod(nameof(List<int>.Add)),
                        E.Call(typeof(NestedBindingNullAccessRegressionTests).GetMethod(nameof(Read)), E.Constant(2))))
                : (Binding)E.MemberBind(typeof(Host).GetProperty(nameof(Host.NullNode)),
                    E.Bind(typeof(Node).GetProperty(nameof(Node.Value)), value));
            var tree = E.Lambda<Action>(E.MemberInit(E.New(typeof(Host)), init));
#if LIGHT_EXPRESSION
            var expected = (Action)tree.ToLambdaExpression().Compile();
#else
            var expected = tree.Compile();
#endif
            var actual = tree.CompileFast(true);
            if (!t.IsNotNull(actual)) return;
            Trace = "";
            t.Throws<NullReferenceException>(() => expected());
            var expectedTrace = Trace;
            Trace = "";
            t.Throws<NullReferenceException>(() => actual());
            t.AreEqual(expectedTrace, Trace);
            t.AreEqual("new;" + (collection ? "list;" : "node;") + "arg1;" + (spill ? "finally;" : ""), Trace);
        }
    }
}
