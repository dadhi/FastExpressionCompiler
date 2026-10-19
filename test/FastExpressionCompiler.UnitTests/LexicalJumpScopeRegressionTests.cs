using System;
using S = System.Linq.Expressions.Expression;
using E = System.Linq.Expressions;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct LexicalJumpScopeRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Operand_entry_is_rejected_for_each_jump_kind(t);
        Typed_child_block_entry_is_rejected(t);
        Unique_void_child_block_entry_is_allowed(t);
        Block_operand_barriers_are_not_erased(t);
        Constructor_operand_entry_is_rejected(t);
        Outward_jumps_remain_legal(t);
        Switch_cases_share_direct_label_scope(t);
        Reused_invocations_have_independent_scope_roots(t);
        Loop_owned_targets_remain_legal(t);
        Deep_adjacent_blocks_preserve_ancestor_and_sibling_scopes(t);
    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.ValidateLabels, CompilerFlags.NoInvocationLambdaInlining | CompilerFlags.ValidateLabels };
    private static void Reject(TestContext t, E.Expression<Func<int>> source)
    {
        t.Throws<InvalidOperationException>(() => source.Compile());
        foreach (var flags in Flags)
            t.Throws<InvalidOperationException>(() => source.FromSysExpression().CompileFast(true, flags | CompilerFlags.ValidateLabels));
    }
    private static void Verify(TestContext t, E.Expression<Func<int>> source, int expected)
    {
        t.AreEqual(expected, source.Compile()());
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(expected, fast());
        }
    }
    public void Operand_entry_is_rejected_for_each_jump_kind(TestContext t)
    {
        foreach (var kind in new[] { E.GotoExpressionKind.Goto, E.GotoExpressionKind.Break, E.GotoExpressionKind.Continue, E.GotoExpressionKind.Return })
        {
            var target = S.Label("operand");
            Reject(t, S.Lambda<Func<int>>(S.Block(S.MakeGoto(kind, target, null, typeof(void)), S.Condition(S.TypeIs(S.Label(target), typeof(int)), S.Constant(1), S.Constant(0)))));
        }
    }
    public void Typed_child_block_entry_is_rejected(TestContext t)
    {
        foreach (var kind in new[] { E.GotoExpressionKind.Goto, E.GotoExpressionKind.Break, E.GotoExpressionKind.Return })
        {
            var target = S.Label(typeof(int), "child-value");
            Reject(t, S.Lambda<Func<int>>(S.Block(S.MakeGoto(kind, target, S.Constant(2), typeof(void)), S.Block(S.Label(target, S.Constant(3))))));
        }
    }
    public void Unique_void_child_block_entry_is_allowed(TestContext t)
    {
        var target = S.Label("child");
        Verify(t, S.Lambda<Func<int>>(S.Block(S.Goto(target), S.Block(S.Throw(S.New(typeof(Exception))), S.Label(target), S.Constant(17)))), 17);
    }
    public void Block_operand_barriers_are_not_erased(TestContext t)
    {
        var target = S.Label("nested-operand");
        Reject(t, S.Lambda<Func<int>>(S.Block(S.Goto(target), S.Add(S.Block(S.Label(target), S.Constant(3)), S.Constant(4)))));
    }
    public sealed class Holder
    {
        public int Value;
        public Holder(int value) { Value = value; }
    }
    public void Constructor_operand_entry_is_rejected(TestContext t)
    {
        var target = S.Label("constructor-operand");
        var value = S.New(typeof(Holder).GetConstructor(new[] { typeof(int) }), S.Block(S.Label(target), S.Constant(3)));
        Reject(t, S.Lambda<Func<int>>(S.Block(S.Goto(target), S.Field(value, nameof(Holder.Value)))));
        Verify(t, S.Lambda<Func<int>>(S.Field(S.New(typeof(Holder).GetConstructor(new[] { typeof(int) }), S.Constant(31)), nameof(Holder.Value))), 31);
    }
    public void Outward_jumps_remain_legal(TestContext t)
    {
        var target = S.Label(typeof(int), "outward");
        Verify(t, S.Lambda<Func<int>>(S.Block(S.Block(S.Goto(target, S.Constant(23))), S.Label(target, S.Constant(9)))), 23);
    }
    public void Switch_cases_share_direct_label_scope(TestContext t)
    {
        var target = S.Label(typeof(int), "case");
        var input = S.Parameter(typeof(int));
        var body = S.Switch(typeof(int), input, S.Constant(0), null,
            S.SwitchCase(S.Block(S.Goto(target, S.Constant(29)), S.Constant(1)), S.Constant(0)),
            S.SwitchCase(S.Block(S.Label(target, S.Constant(2))), S.Constant(1)));
        Verify(t, S.Lambda<Func<int>>(S.Invoke(S.Lambda<Func<int, int>>(body, input), S.Constant(0))), 29);
    }
    public void Reused_invocations_have_independent_scope_roots(TestContext t)
    {
        var target = S.Label(typeof(int), "reused");
        var invocation = S.Invoke(S.Lambda<Func<int>>(S.Block(S.Goto(target, S.Constant(7)), S.Label(target, S.Constant(0)))));
        Verify(t, S.Lambda<Func<int>>(S.Add(invocation, invocation)), 14);
    }
    public void Loop_owned_targets_remain_legal(TestContext t)
    {
        var exit = S.Label(typeof(int), "exit");
        var next = S.Label("next");
        var value = S.Variable(typeof(int));
        Verify(t, S.Lambda<Func<int>>(S.Block(new[] { value }, S.Loop(S.Block(S.PreIncrementAssign(value), S.IfThen(S.LessThan(value, S.Constant(3)), S.Continue(next)), S.Break(exit, value)), exit, next))), 3);
    }
    public void Deep_adjacent_blocks_preserve_ancestor_and_sibling_scopes(TestContext t)
    {
        var target = S.Label(typeof(int), "ancestor");
        E.Expression outward = S.Return(target, S.Constant(29));
        for (var i = 0; i < 128; ++i) outward = S.Block(outward);
        Verify(t, S.Lambda<Func<int>>(S.Block(outward, S.Label(target, S.Constant(0)))), 29);

        // Capturing one sibling must not make the next sibling inherit its scope.
        var first = S.Label(typeof(int), "first");
        var second = S.Label(typeof(int), "second");
        E.Expression siblings = S.Block(S.Block(S.Label(first, S.Constant(1))),
            S.Goto(second, S.Constant(2)), S.Block(S.Label(second, S.Constant(3))));
        for (var i = 0; i < 128; ++i) siblings = S.Block(siblings);
        Reject(t, S.Lambda<Func<int>>(siblings));
    }
}
