#if !LIGHT_EXPRESSION
using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace FastExpressionCompiler.UnitTests;

public struct ExtensionReductionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Irreducible_extensions_follow_unsupported_contract(t);
        Null_reductions_follow_unsupported_contract(t);
        Reducible_extensions_keep_parameters(t);
        Emission_rejects_a_nonprogressing_reduction(t);
        Compile_to_IL_rejects_irreducible_extensions(t);
    }

    public void Irreducible_extensions_follow_unsupported_contract(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(new Irreducible());
        t.IsNull(expression.CompileFast(true));
        t.Throws<NotSupportedExpressionException>(() =>
            expression.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        // The normal fallback remains responsible for the system validation exception.
        t.Throws<ArgumentException>(() => expression.CompileFast());
    }

    public void Null_reductions_follow_unsupported_contract(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(new NullReduction());
        t.IsNull(expression.CompileFast(true));
        t.Throws<NotSupportedExpressionException>(() =>
            expression.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        t.Throws<ArgumentException>(() => expression.CompileFast());
    }

    public void Reducible_extensions_keep_parameters(TestContext t)
    {
        var parameter = Expression.Parameter(typeof(int));
        var body = new Reduction(new Reduction(Expression.Add(parameter, Expression.Constant(2))));
        var expression = Expression.Lambda<Func<int, int>>(body, parameter);
        var fast = expression.CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(5, fast(3));
    }

    public void Emission_rejects_a_nonprogressing_reduction(TestContext t)
    {
        var body = new ReducesOnce();
        t.IsNull(Expression.Lambda<Func<int>>(body).CompileFast(true));
        t.AreEqual(2, body.ReductionCount);
    }

    public void Compile_to_IL_rejects_irreducible_extensions(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(new Irreducible());
        var method = new DynamicMethod("Irreducible", typeof(int), Type.EmptyTypes);
        t.IsFalse(expression.CompileFastToIL(method.GetILGenerator()));
    }

    private sealed class Irreducible : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => typeof(int);
    }

    private sealed class NullReduction : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => typeof(int);
        public override bool CanReduce => true;
        public override Expression Reduce() => null;
    }

    private sealed class Reduction : Expression
    {
        private readonly Expression body;
        public Reduction(Expression body) => this.body = body;
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => body.Type;
        public override bool CanReduce => true;
        public override Expression Reduce() => body;
    }

    private sealed class ReducesOnce : Expression
    {
        public int ReductionCount;
        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => typeof(int);
        public override bool CanReduce => true;
        public override Expression Reduce() => ++ReductionCount == 1 ? Expression.Constant(42) : this;
    }
}
#endif
