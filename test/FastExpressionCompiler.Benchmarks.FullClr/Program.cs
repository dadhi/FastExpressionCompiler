using BenchmarkDotNet.Running;

namespace FastExpressionCompiler.Benchmarks.FullClr
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<SimpleExpr_ParamPlusParam>();
        }
    }
}
