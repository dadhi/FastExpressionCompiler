using System;
#if !LIGHT_EXPRESSION
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests;
public struct CustomReducibleNodeRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Custom_node_type_reduces_before_emission(t);
        Reduction_collects_captured_constants_and_chains(t);
        Invalid_reductions_use_system_validation(t);
        Fresh_block_locals_and_constants_are_shared_between_passes(t);
        Fresh_nested_lambdas_are_shared_between_passes(t);
        Shared_nodes_and_distinct_nodes_preserve_reference_identity(t);
        Reduction_cache_is_local_to_each_compilation(t);
        Emission_without_collection_can_reduce_custom_nodes(t);
    }
    public void Custom_node_type_reduces_before_emission(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(new Reducible(Expression.Constant(3)));
        var fast = expression.CompileFast(true);
        if (t.IsNotNull(fast))
        {
#if NETFRAMEWORK
            t.AreEqual(3, fast()); // Framework System does not reduce unknown node kinds.
#else
            t.AreEqual(expression.Compile()(), fast());
#endif
        }
    }
    public void Reduction_collects_captured_constants_and_chains(TestContext t)
    {
        var marker = new object();
        var expression = Expression.Lambda<Func<object>>(new Reducible(new Reducible(Expression.Constant(marker))));
        var fast = expression.CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(true, ReferenceEquals(marker, fast()));
    }
    public void Invalid_reductions_use_system_validation(TestContext t)
    {
        var source = Expression.Lambda<Func<int>>(new InvalidReduction());
        t.Throws<ArgumentException>(() => source.CompileFast(true));
    }
    private static readonly CompilerFlags[] Flags =
    {
        CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression
    };
    public void Fresh_block_locals_and_constants_are_shared_between_passes(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var node = new FreshBlock(41);
            var fast = Expression.Lambda<Func<int>>(node).CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast());
            t.AreEqual(1, node.Reductions);
        }
    }
    public void Fresh_nested_lambdas_are_shared_between_passes(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var node = new FreshNestedLambda();
            var fast = Expression.Lambda<Func<Func<int>>>(node).CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast()());
            t.AreEqual(1, node.Reductions);
        }
    }
    public void Shared_nodes_and_distinct_nodes_preserve_reference_identity(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var shared = new FreshBlock(20);
            var same = Expression.Lambda<Func<int>>(Expression.Add(shared, shared)).CompileFast(true, flags);
            if (t.IsNotNull(same)) t.AreEqual(42, same());
            t.AreEqual(1, shared.Reductions);
            // User equality must not make two different custom nodes share a reduction.
            var left = new FreshBlock(20); var right = new FreshBlock(21);
            var distinct = Expression.Lambda<Func<int>>(Expression.Add(left, right)).CompileFast(true, flags);
            if (t.IsNotNull(distinct)) t.AreEqual(43, distinct());
            t.AreEqual(1, left.Reductions); t.AreEqual(1, right.Reductions);
        }
    }
    public void Reduction_cache_is_local_to_each_compilation(TestContext t)
    {
        var node = new FreshBlock(41);
        var source = Expression.Lambda<Func<int>>(node);
        for (var i = 1; i <= 2; ++i)
        {
            var fast = source.CompileFast(true);
            if (t.IsNotNull(fast)) t.AreEqual(42, fast());
            t.AreEqual(i, node.Reductions);
        }
    }
    public void Emission_without_collection_can_reduce_custom_nodes(TestContext t)
    {
        var source = Expression.Lambda<Func<int>>(new Reducible(Expression.Constant(42)));
        var fast = source.TryCompileWithoutClosure<Func<int>>();
        if (t.IsNotNull(fast)) t.AreEqual(42, fast());
    }
    public sealed class Marker { public int Value; }
    private sealed class FreshBlock : Expression
    {
        private readonly int _value;
        public int Reductions;
        public FreshBlock(int value) => _value = value;
        public override Type Type => typeof(int);
        public override ExpressionType NodeType => (ExpressionType)(-1);
        public override bool CanReduce => true;
        public override Expression Reduce()
        {
            ++Reductions;
            var local = Expression.Variable(typeof(Marker), "fresh");
            return Expression.Block(new[] { local },
                Expression.Assign(local, Expression.Constant(new Marker { Value = _value })),
                Expression.Add(Expression.Field(local, nameof(Marker.Value)), Expression.Constant(1)));
        }
        public override bool Equals(object obj) => obj is FreshBlock;
        public override int GetHashCode() => 0;
    }
    private sealed class FreshNestedLambda : Expression
    {
        public int Reductions;
        public override Type Type => typeof(Func<int>);
        public override ExpressionType NodeType => (ExpressionType)(-1);
        public override bool CanReduce => true;
        public override Expression Reduce()
        {
            ++Reductions;
            var local = Expression.Variable(typeof(int), "fresh");
            return Expression.Block(new[] { local }, Expression.Assign(local, Expression.Constant(41)),
                Expression.Lambda<Func<int>>(Expression.Add(local, Expression.Constant(1))));
        }
    }
    private sealed class InvalidReduction : Expression
    {
        public override Type Type => typeof(int);
        public override ExpressionType NodeType => (ExpressionType)(-1);
        public override bool CanReduce => true;
        public override Expression Reduce() => Expression.Constant("wrong type");
    }
    public class Reducible : Expression
    {
        private readonly Expression _reduced;
        public Reducible(Expression reduced) => _reduced = reduced;
        public override Type Type => _reduced.Type;
        public override ExpressionType NodeType => (ExpressionType)(-1);
        public override bool CanReduce => true;
        public override Expression Reduce() => _reduced;
    }
}
#endif
