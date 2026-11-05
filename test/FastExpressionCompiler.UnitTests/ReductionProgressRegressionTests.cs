#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
namespace FastExpressionCompiler.UnitTests;
[RunTestX]
public struct ReductionProgressRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Self_reductions_follow_the_strict_rejection_contract(t);
        Null_reductions_follow_the_strict_rejection_contract(t);
        Emission_reuses_the_valid_collected_reduction(t);
        Valid_reduction_chains_keep_parameters(t);
        Compile_to_IL_rejects_nonprogressing_reductions(t);
    }
    public void Self_reductions_follow_the_strict_rejection_contract(TestContext t)
    {
        foreach (var reducible in new[] { false, true })
        {
            var node = new BoundedSelf(reducible);
            t.IsNull(Expression.Lambda<Func<int>>(node).CompileFast(true));
            t.AreEqual(1, node.Count);
            t.Throws<NotSupportedExpressionException>(() => Expression.Lambda<Func<int>>(new BoundedSelf(reducible))
                .CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
            t.Throws<ArgumentException>(() => Expression.Lambda<Func<int>>(new BoundedSelf(reducible)).CompileFast());
        }
    }
    public void Null_reductions_follow_the_strict_rejection_contract(TestContext t)
    {
        t.IsNull(Expression.Lambda<Func<int>>(new NullReduction()).CompileFast(true));
        t.Throws<NotSupportedExpressionException>(() => Expression.Lambda<Func<int>>(new NullReduction())
            .CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        t.Throws<ArgumentException>(() => Expression.Lambda<Func<int>>(new NullReduction()).CompileFast());
    }
    public void Emission_reuses_the_valid_collected_reduction(TestContext t)
    {
        var node = new BoundedSelf(true, firstValue: true);
        // Collection already supplied a valid tree; emission must reuse it.
        var fast = Expression.Lambda<Func<int>>(node).CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(42, fast());
        t.AreEqual(1, node.Count);
    }
    public void Valid_reduction_chains_keep_parameters(TestContext t)
    {
        var p = Expression.Parameter(typeof(int));
        var source = Expression.Lambda<Func<int, int>>(new Reduction(new Reduction(Expression.Add(p, Expression.Constant(2)))), p);
        var fast = source.CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(source.Compile()(3), fast(3));
    }
    public void Compile_to_IL_rejects_nonprogressing_reductions(TestContext t)
    {
        var method = new DynamicMethod("NoProgress", typeof(int), Type.EmptyTypes);
        t.IsFalse(Expression.Lambda<Func<int>>(new BoundedSelf(false)).CompileFastToIL(method.GetILGenerator()));
        t.IsNull(Expression.Lambda<Func<int>>(new BoundedSelf(false)).TryCompileWithoutClosure<Func<int>>());
    }
    // Bound the deliberately broken reducer so testing the unfixed compiler cannot hang.
    private sealed class BoundedSelf : Expression
    {
        private readonly bool _reducible, _firstValue;
        public int Count;
        public BoundedSelf(bool reducible, bool firstValue = false) { _reducible = reducible; _firstValue = firstValue; }
        public override Type Type => typeof(int);
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override bool CanReduce => _reducible;
        public override Expression Reduce()
        {
            if (++Count > 2) throw new InvalidOperationException("Reducer was called repeatedly without progress.");
            return _firstValue && Count == 1 ? Constant(42) : this;
        }
    }
    private sealed class NullReduction : Expression
    {
        public override Type Type => typeof(int);
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override bool CanReduce => true;
        public override Expression Reduce() => null;
    }
    private sealed class Reduction : Expression
    {
        private readonly Expression _body;
        public Reduction(Expression body) => _body = body;
        public override Type Type => _body.Type;
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override bool CanReduce => true;
        public override Expression Reduce() => _body;
    }
}
#endif
