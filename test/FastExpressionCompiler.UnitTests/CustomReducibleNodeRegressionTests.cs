using System;
#if !LIGHT_EXPRESSION
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests;
public struct CustomReducibleNodeRegressionTests : ITestX
{
    public void Run(TestRun t) { Custom_node_type_reduces_before_emission(t); Reduction_collects_captured_constants_and_chains(t); Invalid_reductions_use_system_validation(t); }
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
