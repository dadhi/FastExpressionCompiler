using System;
#if !LIGHT_EXPRESSION
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests;
public struct CustomReducibleNodeRegressionTests : ITestX
{
    public void Run(TestRun t) { Custom_node_type_reduces_before_emission(t); Reduction_collects_captured_constants_and_chains(t); }
    public void Custom_node_type_reduces_before_emission(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(new Reducible(Expression.Constant(3)));
        var fast = expression.CompileFast(true);
        if (t.IsNotNull(fast))
        {
#if NETFRAMEWORK
            // Framework's System compiler rejects reducible custom node types before reduction.
            t.AreEqual(3, fast());
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
