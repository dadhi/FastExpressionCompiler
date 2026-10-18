using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NestedJumpArgumentRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Method_arguments_spill_across_nested_invocation_jump(t);
        Constructor_arguments_spill_across_nested_invocation_jump(t);
        Direct_jump_arguments_spill(t);
        Byref_alias_survives_nested_jump_spilling(t);
        Exception_argument_order_and_writeback_remain_valid(t);
        Earlier_jump_in_argument_block_survives_later_call_collection(t);
    }

    private static string Trace;
    public static int First() { Trace += "first;"; return 2; }
    public static Func<int, int> Receiver() { Trace += "receiver;"; return Pass; }
    public static int Pass(int value) { Trace += "pass;"; return value; }
    public static int Combine(int first, int second) { Trace += "combine;"; return first + second; }
    public static int Write(ref int first, int second) { Trace += "write;"; return first += second; }
    public static void Finally() { Trace += "finally;"; }
    public sealed class Pair
    {
        public Pair(int first, int second) { Trace += "ctor;"; }
    }

    private static System.Linq.Expressions.MethodCallExpression Call(string name, params System.Linq.Expressions.Expression[] args) =>
        S.Call(typeof(NestedJumpArgumentRegressionTests).GetMethod(name), args);
    private static System.Linq.Expressions.Expression Argument(System.Linq.Expressions.ParameterExpression leave, System.Linq.Expressions.LabelTarget target) =>
        S.Condition(leave, S.Goto(target, S.Constant(7), typeof(int)), S.Constant(3));
    private static System.Linq.Expressions.Expression InvokeArgument(System.Linq.Expressions.ParameterExpression leave, System.Linq.Expressions.LabelTarget target) =>
        S.Invoke(Call(nameof(Receiver)), Argument(leave, target));
    private static System.Linq.Expressions.Expression<Func<bool, int>> Tree(System.Linq.Expressions.ParameterExpression leave,
        System.Linq.Expressions.LabelTarget target, System.Linq.Expressions.Expression body) =>
        S.Lambda<Func<bool, int>>(S.Block(body, S.Label(target, S.Constant(8))), leave);

    public void Method_arguments_spill_across_nested_invocation_jump(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int));
        Verify(t, Tree(leave, target, Call(nameof(Combine), Call(nameof(First)), InvokeArgument(leave, target))));
    }
    public void Constructor_arguments_spill_across_nested_invocation_jump(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int));
        Verify(t, Tree(leave, target, S.New(typeof(Pair).GetConstructor(new[] { typeof(int), typeof(int) }),
            Call(nameof(First)), InvokeArgument(leave, target))));
    }
    public void Direct_jump_arguments_spill(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int));
        var argument = Argument(leave, target);
        Verify(t, Tree(leave, target, Call(nameof(Combine), Call(nameof(First)), argument)));
        Verify(t, Tree(leave, target, S.New(typeof(Pair).GetConstructor(new[] { typeof(int), typeof(int) }),
            Call(nameof(First)), argument)));
    }
    public void Byref_alias_survives_nested_jump_spilling(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int)); var value = S.Variable(typeof(int));
        var tree = S.Lambda<Func<bool, int>>(S.Block(new[] { value }, S.Assign(value, S.Constant(5)),
            Call(nameof(Write), value, InvokeArgument(leave, target)), S.Label(target, value)), leave);
        Verify(t, tree);
    }
    public void Exception_argument_order_and_writeback_remain_valid(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int)); var value = S.Variable(typeof(int));
        var argument = S.TryFinally(InvokeArgument(leave, target), Call(nameof(Finally)));
        var tree = S.Lambda<Func<bool, int>>(S.Block(new[] { value }, S.Assign(value, S.Constant(5)),
            Call(nameof(Write), value, argument), S.Label(target, value)), leave);
        Verify(t, tree);
    }
    public void Earlier_jump_in_argument_block_survives_later_call_collection(TestContext t)
    {
        var leave = S.Parameter(typeof(bool)); var target = S.Label(typeof(int));
        var argument = S.Block(Argument(leave, target), Call(nameof(Pass), S.Constant(3)));
        Verify(t, Tree(leave, target, Call(nameof(Combine), Call(nameof(First)), argument)));
        Verify(t, Tree(leave, target, S.New(typeof(Pair).GetConstructor(new[] { typeof(int), typeof(int) }),
            Call(nameof(First)), argument)));
    }
    private static void Verify(TestContext t, System.Linq.Expressions.Expression<Func<bool, int>> tree)
    {
        var system = tree.Compile();
        foreach (var leave in new[] { true, false })
        {
            Trace = ""; var expected = system(leave); var trace = Trace;
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
                CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
            {
                var fast = tree.FromSysExpression().CompileFast(true, flags);
                if (!t.IsNotNull(fast)) return;
                Trace = ""; t.AreEqual(expected, fast(leave)); t.AreEqual(trace, Trace);
            }
        }
    }
}
