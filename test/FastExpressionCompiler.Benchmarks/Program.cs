using BenchmarkDotNet.Running;

namespace FastExpressionCompiler.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<CreateExprCompile_vs_CreateExprInfoCompile>();

            //BenchmarkRunner.Run<ExpressionVsExpressionInfo>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Compile>();
            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Invoke>();
        }
    }
}
