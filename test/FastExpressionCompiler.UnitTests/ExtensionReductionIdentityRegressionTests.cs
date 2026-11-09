using System;
using System.Reflection.Emit;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

[RunTest]
public struct ExtensionReductionIdentityRegressionTests : ITestX
{
    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };

    public void Run(TestRun t)
    {
        Fresh_blocks_and_captured_constants_share_the_collected_tree(t);
        Fresh_nested_lambdas_share_the_collected_tree(t);
        Fresh_labels_share_the_collected_tree(t);
        Shared_and_distinct_nodes_use_reference_identity(t);
        Reductions_are_local_to_each_compilation(t);
        Public_emission_paths_reduce_once(t);
        Invalid_first_reductions_remain_rejected(t);
#if LIGHT_EXPRESSION
        Intrinsic_nodes_keep_their_collection_and_emission_hooks(t);
#endif
    }

    public void Fresh_blocks_and_captured_constants_share_the_collected_tree(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var node = IntBlock(41);
            var fast = E.Lambda<Func<int>>(node).CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast());
            t.AreEqual(1, node.Reductions);
        }
    }

    public void Fresh_nested_lambdas_share_the_collected_tree(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var node = new FreshReduction(typeof(Func<int>), () =>
            {
                var local = E.Variable(typeof(int), "fresh");
                return E.Block(new[] { local }, E.Assign(local, E.Constant(41)),
                    E.Lambda<Func<int>>(E.Add(local, E.Constant(1))));
            });
            var fast = E.Lambda<Func<Func<int>>>(node).CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast()());
            t.AreEqual(1, node.Reductions);
        }
    }

    public void Fresh_labels_share_the_collected_tree(TestContext t)
    {
        FreshReduction Node() => new FreshReduction(typeof(int), () =>
        {
            var target = E.Label(typeof(int), "fresh");
            return E.Block(E.Goto(target, E.Constant(42)), E.Label(target, E.Constant(0)));
        });
        foreach (var flags in Flags)
        {
            var node = Node();
            var fast = E.Lambda<Func<int>>(node).CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast());
            t.AreEqual(1, node.Reductions);
            var shared = Node();
            var repeated = E.Lambda<Func<int>>(E.Add(shared, shared)).CompileFast(true, flags);
            if (t.IsNotNull(repeated)) t.AreEqual(84, repeated());
            t.AreEqual(1, shared.Reductions);
        }
    }

    public void Shared_and_distinct_nodes_use_reference_identity(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var shared = IntBlock(20);
            var repeated = E.Lambda<Func<int>>(E.Add(shared, shared)).CompileFast(true, flags);
            if (t.IsNotNull(repeated)) t.AreEqual(42, repeated());
            t.AreEqual(1, shared.Reductions);
            var left = IntBlock(20);
            var right = IntBlock(21);
            var distinct = E.Lambda<Func<int>>(E.Add(left, right)).CompileFast(true, flags);
            if (t.IsNotNull(distinct)) t.AreEqual(43, distinct());
            t.AreEqual(1, left.Reductions);
            t.AreEqual(1, right.Reductions);
            var condition = E.Parameter(typeof(bool));
            var branches = E.Lambda<Func<bool, int>>(E.Condition(condition, left, right), condition).CompileFast(true, flags);
            if (t.IsNotNull(branches)) { t.AreEqual(21, branches(true)); t.AreEqual(22, branches(false)); }
            t.AreEqual(2, left.Reductions);
            t.AreEqual(2, right.Reductions);
        }
    }

    public void Reductions_are_local_to_each_compilation(TestContext t)
    {
        var node = new FreshReduction(typeof(Marker), () => E.Constant(new Marker { Value = 42 }));
        var source = E.Lambda<Func<Marker>>(node);
        var first = source.CompileFast(true);
        var second = source.CompileFast(true);
        if (t.IsNotNull(first) && t.IsNotNull(second))
        {
            t.AreEqual(42, first().Value);
            t.AreEqual(42, second().Value);
            t.IsFalse(ReferenceEquals(first(), second()));
        }
        t.AreEqual(2, node.Reductions);
    }

    public void Public_emission_paths_reduce_once(TestContext t)
    {
        FreshReduction Node() => new FreshReduction(typeof(int), () =>
        {
            var local = E.Variable(typeof(int), "fresh");
            return E.Block(new[] { local }, E.Assign(local, E.Constant(42)), local);
        });
        var direct = Node();
        var fast = E.Lambda<Func<int>>(direct).TryCompileWithoutClosure<Func<int>>();
        if (t.IsNotNull(fast)) t.AreEqual(42, fast());
        t.AreEqual(1, direct.Reductions);
        var emitted = Node();
        var method = new DynamicMethod("FreshExtension", typeof(int), Type.EmptyTypes);
        if (t.IsTrue(E.Lambda<Func<int>>(emitted).CompileFastToIL(method.GetILGenerator())))
            t.AreEqual(42, ((Func<int>)method.CreateDelegate(typeof(Func<int>)))());
        t.AreEqual(1, emitted.Reductions);
    }

    public void Invalid_first_reductions_remain_rejected(TestContext t)
    {
        foreach (var self in new[] { false, true })
        {
            FreshReduction node = null;
            node = new FreshReduction(typeof(int), () => self ? node : null);
            t.IsNull(E.Lambda<Func<int>>(node).CompileFast(true));
            t.AreEqual(1, node.Reductions);
            t.IsNull(E.Lambda<Func<int>>(node).TryCompileWithoutClosure<Func<int>>());
            t.AreEqual(2, node.Reductions);
        }
    }

    private static FreshReduction IntBlock(int value) => new FreshReduction(typeof(int), () =>
    {
        var local = E.Variable(typeof(Marker), "fresh");
        return E.Block(new[] { local }, E.Assign(local, E.Constant(new Marker { Value = value })),
            E.Add(E.Field(local, nameof(Marker.Value)), E.Constant(1)));
    });

    public sealed class Marker { public int Value; }

#if LIGHT_EXPRESSION
    private sealed class FreshReduction : UnaryExpression
#else
    private sealed class FreshReduction : E
#endif
    {
        private readonly Func<E> _reduce;
        public int Reductions;
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
#if LIGHT_EXPRESSION
        public FreshReduction(Type type, Func<E> reduce) : base(E.Default(type)) => _reduce = reduce;
#else
        public override Type Type { get; }
        public override bool CanReduce => true;
        public FreshReduction(Type type, Func<E> reduce) { Type = type; _reduce = reduce; }
#endif
        public override E Reduce() { ++Reductions; return _reduce(); }
        public override bool Equals(object obj) => obj is FreshReduction;
        public override int GetHashCode() => 0;
    }

#if LIGHT_EXPRESSION
    public void Intrinsic_nodes_keep_their_collection_and_emission_hooks(TestContext t)
    {
        var node = new Intrinsic();
        var fast = E.Lambda<Func<int>>(node).CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(42, fast());
        t.AreEqual(1, node.Collections);
        t.AreEqual(1, node.Emissions);
    }

    private sealed class Intrinsic : UnaryExpression
    {
        public int Collections, Emissions;
        public Intrinsic() : base(E.Default(typeof(int))) { }
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override bool IsIntrinsic => true;
        public override E Reduce() => throw new InvalidOperationException("Intrinsic reduction should not be used.");
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda,
            ref ImTools.SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas, short scopeIdx)
        {
            ++Collections;
            return ExpressionCompiler.Result.OK;
        }
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, ILGenerator il, ExpressionCompiler.ParentFlags parent, int byRefIndex = -1)
        {
            ++Emissions;
            il.Emit(OpCodes.Ldc_I4, 42);
            return true;
        }
    }
#endif
}
