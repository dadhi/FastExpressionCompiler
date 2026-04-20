using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.FlatExpression;
using FastExpressionCompiler.LightExpression.UnitTests;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class LightExprVsFlatExpr_Create_ComplexExpr
    {
        // Keep the created values reachable so the construction work is not elided.
        private FastExpressionCompiler.LightExpression.Expression<System.Func<object[], object>> _lightExpr;
        private ExprTree _flatExpr;

        [Benchmark(Baseline = true)]
        public void Create_LightExpression() =>
            _lightExpr = LightExpressionTests.CreateComplexLightExpression();

        [Benchmark]
        public void Create_FlatExpression() =>
            _flatExpr = LightExpressionTests.CreateComplexFlatExpression();
    }
}
