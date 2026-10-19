using System;
using System.Reflection;
using System.Reflection.Emit;
using S = System.Linq.Expressions.Expression;
using E = System.Linq.Expressions;
#if LIGHT_EXPRESSION
using NestedLambdas = FastExpressionCompiler.LightExpression.ImTools.SmallList<FastExpressionCompiler.LightExpression.ExpressionCompiler.NestedLambdaInfo>;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using NestedLambdas = FastExpressionCompiler.ImTools.SmallList<FastExpressionCompiler.ExpressionCompiler.NestedLambdaInfo>;
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
        Single_expression_block_chains_preserve_switch_and_operand_scopes(t);
        Alternating_scope_overflow_buffers_can_be_reused(t);
        Label_free_blocks_keep_reused_inlined_invocations(t);
        Public_collection_unwinds_failed_deep_scopes_and_can_retry(t);
#if LIGHT_EXPRESSION
        Custom_intrinsic_can_recover_from_failed_child_collection(t);
#endif
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
    public void Single_expression_block_chains_preserve_switch_and_operand_scopes(TestContext t)
    {
        var result = S.Label(typeof(int), "chain-case");
        E.Expression jumpChain = S.Goto(result, S.Constant(47));
        for (var i = 0; i < 32; ++i) jumpChain = S.Block(jumpChain);
        var input = S.Parameter(typeof(int));
        var selection = S.Switch(typeof(int), input, S.Constant(0), null,
            S.SwitchCase(S.Block(jumpChain, S.Constant(1)), S.Constant(0)),
            S.SwitchCase(S.Block(S.Label(result, S.Constant(2))), S.Constant(1)));
        Verify(t, S.Lambda<Func<int>>(S.Invoke(S.Lambda<Func<int, int>>(selection, input), S.Constant(0))), 47);

        var target = S.Label("chain-operand");
        E.Expression operand = S.Block(S.Label(target), S.Constant(3));
        for (var i = 0; i < 32; ++i) operand = S.Block(operand);
        Reject(t, S.Lambda<Func<int>>(S.Block(S.Goto(target), S.Add(operand, S.Constant(4)))));
    }
    public void Alternating_scope_overflow_buffers_can_be_reused(TestContext t)
    {
        for (var repetition = 0; repetition < 3; ++repetition)
        {
            var target = S.Label(typeof(int), "pooled-ancestor");
            E.Expression body = S.Return(target, S.Constant(37));
            var test = S.Call(typeof(string).GetMethod(nameof(string.IsNullOrEmpty)), S.Constant(""));
            for (var i = 0; i < 32; ++i)
                body = S.Block(S.Condition(test, S.Block(body), S.Empty()));
            Verify(t, S.Lambda<Func<int>>(S.Block(body, S.Label(target, S.Constant(0)))), 37);

            // A failed compilation must also return its transient frame buffers.
            var inner = S.Label(typeof(int), "pooled-inner");
            E.Expression invalid = S.Block(S.Goto(inner, S.Constant(1)), S.Block(S.Label(inner, S.Constant(2))));
            for (var i = 0; i < 32; ++i)
                invalid = S.Block(S.Condition(test, invalid, S.Constant(0)));
            Reject(t, S.Lambda<Func<int>>(invalid));
        }
    }
    public void Label_free_blocks_keep_reused_inlined_invocations(TestContext t)
    {
        var invoke = S.Invoke(S.Lambda<Func<int>>(S.Constant(19)));
        var variable = S.Variable(typeof(int), "value");
        var body = S.Block(S.Block(invoke),
            S.Block(new[] { variable }, S.Assign(variable, invoke), S.Add(variable, invoke)));
        Verify(t, S.Lambda<Func<int>>(body), 38);
    }
    private static int CollectedScopeDepth(ExpressionCompiler.CompilerContext context) =>
        (int)typeof(ExpressionCompiler.CompilerContext).GetField("LabelScopeDepth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);

    private static E.Expression<Action> FailingDeepScopes()
    {
        E.Expression body = S.Rethrow();
        var test = S.Call(typeof(string).GetMethod(nameof(string.IsNullOrEmpty)), S.Constant(""));
        for (var i = 0; i < 32; ++i) body = S.Block(S.IfThen(test, body));
        return S.Lambda<Action>(body);
    }

    public void Public_collection_unwinds_failed_deep_scopes_and_can_retry(TestContext t)
    {
        for (var iteration = 0; iteration < 3; ++iteration)
        {
            var context = new ExpressionCompiler.CompilerContext(ExpressionCompiler.ClosureStatus.ToBeCollected, null, CompilerFlags.Default);
            NestedLambdas nested = default;
            var failed = FailingDeepScopes().FromSysExpression();
            t.Throws<InvalidOperationException>(() => ExpressionCompiler.TryCollectInfo(ref context, failed.Body, null, ref nested));
            t.AreEqual(0, CollectedScopeDepth(context));
            var frames = typeof(ExpressionCompiler.CompilerContext).GetField("LabelScopes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
            t.IsNull(frames.GetType().GetField("Rest").GetValue(frames));

            var target = S.Label("retry");
            var valid = S.Lambda<Action>(S.Block(S.Goto(target), S.Label(target))).FromSysExpression();
            t.IsTrue(ExpressionCompiler.TryCollectBoundConstants(ref context, valid.Body, null, ref nested));
            var method = new DynamicMethod("retry", typeof(void), Type.EmptyTypes, typeof(LexicalJumpScopeRegressionTests), true);
            var il = method.GetILGenerator();
            t.IsTrue(ExpressionCompiler.EmittingVisitor.TryEmit(valid.Body, il, ref context, ExpressionCompiler.ParentFlags.IgnoreResult));
            il.Emit(OpCodes.Ret);
            ((Action)method.CreateDelegate(typeof(Action)))();
        }
    }

#if LIGHT_EXPRESSION
    private sealed class RecoveringIntrinsic : TypedUnaryExpression
    {
        public RecoveringIntrinsic(bool block = false) : base(block ? E.ExpressionType.Block : E.ExpressionType.Convert, Expression.Constant(19), typeof(int)) { }
        public override bool IsIntrinsic => true;
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda, ref NestedLambdas rootNestedLambdas)
        {
            var depth = CollectedScopeDepth(context);
            var failed = FailingDeepScopes().ToLightExpression();
            try
            {
                ExpressionCompiler.TryCollectInfo(ref context, failed.Body, nestedLambda, ref rootNestedLambdas);
                throw new Exception("Expected invalid rethrow rejection.");
            }
            catch (InvalidOperationException) { }
            if (CollectedScopeDepth(context) != depth)
                throw new Exception("Failed child collection leaked lexical scopes into its fallback.");
            return ExpressionCompiler.TryCollectInfo(ref context, Operand, nestedLambda, ref rootNestedLambdas);
        }
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, ILGenerator il,
            ExpressionCompiler.ParentFlags parent, int byRefIndex = -1) =>
            ExpressionCompiler.EmittingVisitor.TryEmit(Operand, il, ref context, parent, byRefIndex);
    }

    public void Custom_intrinsic_can_recover_from_failed_child_collection(TestContext t)
    {
        Expression child = new RecoveringIntrinsic(block: true);
        for (var i = 0; i < 32; ++i) child = Expression.Block(child);
        var intrinsicBlock = Expression.Lambda<Func<int>>(child).CompileFast(true);
        if (t.IsNotNull(intrinsicBlock)) t.AreEqual(19, intrinsicBlock());
        var source = Expression.Lambda<Func<int>>(Expression.Block(new RecoveringIntrinsic()));
        foreach (var flags in Flags)
        {
            var compiled = source.CompileFast(true, flags);
            if (t.IsNotNull(compiled)) t.AreEqual(19, compiled());
        }
    }
#endif
}
