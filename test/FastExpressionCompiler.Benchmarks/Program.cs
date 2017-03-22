using BenchmarkDotNet.Running;

namespace FastExpressionCompiler.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();
            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Compile>();
            BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Invoke>();
        }
    }
}
