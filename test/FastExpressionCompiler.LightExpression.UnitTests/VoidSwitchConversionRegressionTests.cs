using System;
using System.Collections.Generic;
using S = System.Linq.Expressions;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests;

public struct VoidSwitchConversionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Typed_gotos_keep_void_branch_types_and_shared_labels(t);
        Discarded_values_preserve_selected_branch_effects(t);
        Void_and_value_switches_keep_existing_body_nodes(t);
    }

    public void Typed_gotos_keep_void_branch_types_and_shared_labels(TestContext t)
    {
        var p = Parameter(typeof(int));
        var target = Label(typeof(string));
        var body = Switch(typeof(void), p, Return(target, Constant("bar")), null,
            SwitchCase(Return(target, Constant("foo")), Constant(1), Constant(2)));
        var tree = Lambda<Func<int, string>>(Block(body, Label(target, Constant(""))), p);
        var converted = tree.ToLambdaExpression();
        var block = (S.BlockExpression)converted.Body;
        var sw = (S.SwitchExpression)block.Expressions[0];
        var label = (S.LabelExpression)block.Expressions[1];
        t.AreEqual(typeof(void), sw.Type);
        t.AreEqual(typeof(void), sw.DefaultBody.Type);
        t.AreEqual(typeof(void), sw.Cases[0].Body.Type);
        var fallback = (S.GotoExpression)((S.BlockExpression)sw.DefaultBody).Expressions[0];
        var selected = (S.GotoExpression)((S.BlockExpression)sw.Cases[0].Body).Expressions[0];
        t.AreEqual(true, ReferenceEquals(label.Target, fallback.Target));
        t.AreEqual(true, ReferenceEquals(label.Target, selected.Target));
        var system = converted.Compile();
        var fast = tree.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var input in new[] { 1, 2, 42 })
        {
            var expected = input == 42 ? "bar" : "foo";
            t.AreEqual(expected, system(input));
            t.AreEqual(expected, fast(input));
        }
    }

    public void Discarded_values_preserve_selected_branch_effects(TestContext t)
    {
        var trace = new List<int>();
        Func<int> first = () => { trace.Add(1); return 123; };
        Func<string> fallback = () => { trace.Add(2); return "discard"; };
        var p = Parameter(typeof(int));
        var body = Switch(typeof(void), p, Invoke(Constant(fallback)), null,
            SwitchCase(Invoke(Constant(first)), Constant(1)));
        var tree = Lambda<Action<int>>(body, p);
        var converted = tree.ToLambdaExpression();
        var sw = (S.SwitchExpression)converted.Body;
        t.AreEqual(typeof(void), sw.DefaultBody.Type);
        t.AreEqual(typeof(void), sw.Cases[0].Body.Type);
        var system = converted.Compile();
        var fast = tree.CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        system(1); system(42); fast(1); fast(42);
        t.AreEqual("1,2,1,2", string.Join(",", trace));
    }

    public void Void_and_value_switches_keep_existing_body_nodes(TestContext t)
    {
        var p = Parameter(typeof(int));
        var empty = Empty();
        var alreadyVoid = (S.SwitchExpression)Switch(typeof(void), p, empty, null,
            SwitchCase(empty, Constant(1))).ToExpression();
        t.AreEqual(S.ExpressionType.Default, alreadyVoid.DefaultBody.NodeType);
        t.AreEqual(true, ReferenceEquals(alreadyVoid.DefaultBody, alreadyVoid.Cases[0].Body));
        var text = Constant("value");
        var value = (S.SwitchExpression)Switch(typeof(string), p, text, null,
            SwitchCase(text, Constant(1))).ToExpression();
        t.AreEqual(typeof(string), value.Type);
        t.AreEqual(S.ExpressionType.Constant, value.DefaultBody.NodeType);
        t.AreEqual(true, ReferenceEquals(value.DefaultBody, value.Cases[0].Body));
        var noDefault = (S.SwitchExpression)Switch(typeof(void), p, null, null,
            SwitchCase(Constant(123), Constant(1))).ToExpression();
        t.AreEqual(true, noDefault.DefaultBody == null);
        t.AreEqual(typeof(void), noDefault.Cases[0].Body.Type);
    }
}
