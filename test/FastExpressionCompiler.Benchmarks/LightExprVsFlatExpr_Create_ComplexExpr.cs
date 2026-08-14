using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.FlatExpression;
using FastExpressionCompiler.LightExpression.UnitTests;

namespace FastExpressionCompiler.Benchmarks
{
    /*
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

| Method                 | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Create_LightExpression | 175.0 ns |  3.29 ns |  4.17 ns |  1.00 |    0.03 |    1 | 0.0827 |     520 B |        1.00 |
| Create_FlatExpression  | 525.5 ns | 10.47 ns | 12.86 ns |  3.00 |    0.10 |    2 |      - |         - |        0.00 |

    */
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
