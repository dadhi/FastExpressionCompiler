using BenchmarkDotNet.Attributes;

using FastExpressionCompiler.LightExpression.UnitTests;
using LE = FastExpressionCompiler.LightExpression.ExpressionCompiler;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    [MarkdownExporter]
    [ClrJob, CoreJob]
    public class ExprInfoVsExpr_CreateAndCompile_ComplexExpr
    {
        [Benchmark]
        public object CreateExpression_and_Compile() =>
            ExpressionInfoTests.CreateComplexExpression().Compile();

        [Benchmark]
        public object CreateExpression_and_CompileFast() =>
            ExpressionInfoTests.CreateComplexExpression().CompileFast();

        [Benchmark(Baseline = true)]
        public object CreateExpressionInfo_and_CompileFast() =>
            LE.CompileFast(ExpressionInfoTests.CreateComplexExpressionInfo());
    }

    [MemoryDiagnoser]
    [MarkdownExporter]
    [ClrJob, CoreJob]
    public class ExprInfoVsExpr_Create_ComplexExpr
    {
        [Benchmark]
        public object CreateExpression() =>
            ExpressionInfoTests.CreateComplexExpression();

        [Benchmark(Baseline = true)]
        public object CreateExpressionInfo() =>
            ExpressionInfoTests.CreateComplexExpressionInfo();
    }
}
