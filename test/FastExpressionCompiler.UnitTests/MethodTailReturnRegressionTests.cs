using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct MethodTailReturnRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reused_method_tail_target_returns_from_ignored_branch(t);
        Method_return_type_preserves_reference_covariance_and_boxing(t);
        Ignored_non_tail_target_stays_local(t);
        Typed_labels_in_an_Action_stay_local(t);
        Operand_returns_preserve_values_and_inlined_callers(t);
        Multiple_references_preserve_reused_tail_bindings(t);
        Label_defaults_and_backward_references_stay_bound(t);
        Nested_goto_values_and_finally_preserve_control_flow(t);
        Reused_method_tail_in_operand_context_is_rejected_cleanly(t);
    }

    private static string Trace;
    public static void Mark(string value) { Trace += value; }
    public static int Identity(int value) => value;
    private static System.Linq.Expressions.Expression MarkExpression(string text) =>
        S.Call(typeof(MethodTailReturnRegressionTests).GetMethod(nameof(Mark)), S.Constant(text));
    private static System.Linq.Expressions.Expression Pair(System.Linq.Expressions.LabelTarget target, int value) =>
        S.Block(S.Return(target, S.Constant(value)), S.Label(target, S.Constant(0)));

    private static System.Linq.Expressions.Expression StringBranch(System.Linq.Expressions.LabelTarget target, string value) =>
        S.Block(S.Return(target, S.Constant(value), typeof(string)), S.Label(target, S.Constant(null, typeof(string))));
    private static System.Linq.Expressions.Expression StringBody(System.Linq.Expressions.ParameterExpression input)
    {
        var target = S.Label(typeof(string));
        return S.Block(S.IfThen(S.Equal(input, S.Constant("42")), StringBranch(target, "true")), StringBranch(target, "false"));
    }
    public void Reused_method_tail_target_returns_from_ignored_branch(TestContext t)
    {
        var input = S.Parameter(typeof(string)); var source = S.Lambda<Func<string, string>>(StringBody(input), input);
        Verify(t, source, "42"); Verify(t, source, "35");
    }
    public void Method_return_type_preserves_reference_covariance_and_boxing(TestContext t)
    {
        var input = S.Parameter(typeof(string)); var source = S.Lambda<Func<string, object>>(StringBody(input), input);
        Verify(t, source, "42"); Verify(t, source, "35");
        var target = S.Label(typeof(int));
        Verify(t, S.Lambda<Func<object>>(S.Convert(Pair(target, 5), typeof(object))));
    }
    public void Ignored_non_tail_target_stays_local(TestContext t)
    {
        var ignored = S.Label(typeof(int)); var tail = S.Label(typeof(int));
        Verify(t, S.Lambda<Func<int>>(S.Block(Pair(ignored, 1), Pair(tail, 2))));
    }
    public void Typed_labels_in_an_Action_stay_local(TestContext t)
    {
        var target = S.Label(typeof(string));
        var source = S.Lambda<Action>(S.Block(StringBranch(target, "first"), MarkExpression("one;"),
            MarkExpression("two;"), StringBranch(target, "last")));
        Trace = ""; source.Compile()(); t.AreEqual("one;two;", Trace);
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            Trace = ""; fast(); t.AreEqual("one;two;", Trace);
        }
    }
    public void Operand_returns_preserve_values_and_inlined_callers(TestContext t)
    {
        var target = S.Label(typeof(int)); var value = S.Variable(typeof(int));
        var local = S.Block(new[] { value }, S.Assign(value, S.Constant(5)), S.Return(target, value), S.Label(target, S.Constant(0)));
        var source = S.Lambda<Func<int>>(S.Add(local, S.Constant(10)));
        Verify(t, source); Verify(t, S.Lambda<Func<int>>(S.Invoke(source)));
    }
    public void Multiple_references_preserve_reused_tail_bindings(TestContext t)
    {
        var input = S.Parameter(typeof(int)); var target = S.Label(typeof(string));
        var first = S.Block(S.IfThen(S.Equal(input, S.Constant(0)), S.Goto(target, S.Constant("zero"))),
            S.Return(target, S.Constant("positive")), S.Label(target, S.Constant("unused")));
        var source = S.Lambda<Func<int, string>>(S.Block(S.IfThen(S.GreaterThanOrEqual(input, S.Constant(0)), first),
            StringBranch(target, "negative")), input);
        foreach (var value in new[] { -1, 0, 1 }) Verify(t, source, value);
    }
    public void Label_defaults_and_backward_references_stay_bound(TestContext t)
    {
        var target = S.Label(typeof(int));
        Verify(t, S.Lambda<Func<int>>(S.Label(target, S.Constant(7))));
        var counter = S.Variable(typeof(int));
        var source = S.Lambda<Func<int>>(S.Block(new[] { counter }, S.Assign(counter, S.Constant(0)),
            S.Label(target, S.Constant(-1)), S.IfThen(S.LessThan(S.PreIncrementAssign(counter), S.Constant(3)),
                S.Goto(target, S.Constant(47))), counter));
        Verify(t, source);
    }
    public void Nested_goto_values_and_finally_preserve_control_flow(TestContext t)
    {
        var target = S.Label(typeof(string));
        Verify(t, S.Lambda<Func<string>>(S.Block(
            S.Return(target, S.Goto(target, S.Constant("nested"), typeof(string))), S.Label(target, S.Constant("unused")))));
        var leave = S.Parameter(typeof(bool));
        var source = S.Lambda<Func<bool, string>>(S.Block(S.TryFinally(
            S.IfThen(leave, S.Goto(target, S.Constant("try"))), MarkExpression("finally;")),
            S.Label(target, S.Constant("default"))), leave);
        Verify(t, source, true); Verify(t, source, false);
    }
    public void Reused_method_tail_in_operand_context_is_rejected_cleanly(TestContext t)
    {
        var target = S.Label(typeof(int));
        var source = S.Lambda<Func<int>>(S.Block(S.Call(typeof(MethodTailReturnRegressionTests).GetMethod(nameof(Identity)),
            Pair(target, 5)), Pair(target, 9)));
        t.AreEqual(5, source.Compile()());
        t.AreEqual(true, source.FromSysExpression().CompileFast(true) == null);
        t.Throws<NotSupportedExpressionException>(() => source.FromSysExpression().CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
    }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    private static void Verify<T>(TestContext t, System.Linq.Expressions.Expression<Func<T>> source)
    {
        Trace = ""; var expected = source.Compile()(); var trace = Trace;
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            Trace = ""; t.AreEqual(expected, fast()); t.AreEqual(trace, Trace);
        }
    }
    private static void Verify<TArg, TResult>(TestContext t, System.Linq.Expressions.Expression<Func<TArg, TResult>> source, TArg value)
    {
        Trace = ""; var expected = source.Compile()(value); var trace = Trace;
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            Trace = ""; t.AreEqual(expected, fast(value)); t.AreEqual(trace, Trace);
        }
    }
}
