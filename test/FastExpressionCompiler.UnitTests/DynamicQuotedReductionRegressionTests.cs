using System;
#if !LIGHT_EXPRESSION
using System.Linq.Expressions;
namespace FastExpressionCompiler.UnitTests;
public struct DynamicQuotedReductionRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Fresh_dynamic_reductions_reuse_collected_occurrences(t);
        Fresh_quote_reductions_preserve_capture_metadata(t);
    }
    public void Fresh_dynamic_reductions_reuse_collected_occurrences(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        foreach (var customKind in new[] { false, true })
        {
            var parameter = Expression.Parameter(typeof(int));
            var node = new FreshReduction(typeof(int), () => Expression.MakeDynamic(
                typeof(Func<System.Runtime.CompilerServices.CallSite, int, int>),
                new DynamicCallSiteRegressionTests.TypedBinder(), parameter), customKind);
            var expression = Expression.Lambda<Func<int, int>>(node, parameter);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(6, fast(5)); t.AreEqual(1, node.Reductions);
#if !NETFRAMEWORK
            t.AreEqual(6, expression.Compile()(5));
#endif
        }
    }
    public void Fresh_quote_reductions_preserve_capture_metadata(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var parameter = Expression.Parameter(typeof(int));
            var node = new FreshReduction(typeof(Expression<Func<int>>),
                () => Expression.Quote(Expression.Lambda<Func<int>>(parameter)));
            var expression = Expression.Lambda<Func<int, Expression<Func<int>>>>(node, parameter);
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(1, node.Reductions);
            var first = fast(17); var second = fast(29);
            t.AreEqual(17, first.Compile()()); t.AreEqual(29, second.Compile()());
            var compiled = first.CompileFast(true, flags);
            if (t.IsNotNull(compiled)) t.AreEqual(17, compiled());
        }
    }
    private sealed class FreshReduction : Expression
    {
        private readonly Func<Expression> _reduce;
        private readonly bool _customKind;
        public int Reductions;
        public FreshReduction(Type type, Func<Expression> reduce, bool customKind = false)
        { Type = type; _reduce = reduce; _customKind = customKind; }
        public override Type Type { get; }
        public override ExpressionType NodeType => _customKind ? (ExpressionType)(-1) : ExpressionType.Extension;
        public override bool CanReduce => true;
        public override Expression Reduce() { ++Reductions; return _reduce(); }
    }
}
#endif
