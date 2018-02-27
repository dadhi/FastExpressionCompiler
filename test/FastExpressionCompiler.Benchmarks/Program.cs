using BenchmarkDotNet.Running;

namespace FastExpressionCompiler.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Compile>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Invoke>();

            BenchmarkRunner.Run<SimpleExpr_ParamPlusParam>();

            //BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_ComplexExpr>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_NestedLambdaExpr>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_SimpleExpr>();

            //BenchmarkRunner.Run<ExprInfoVsExpr_Create_SimpleExpr>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_Create_ComplexExpr>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.CompileWithNestedLambda>();
            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.InvokeWithNestedLambda>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Invoke>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.CompileManuallyComposed>();
            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.InvokeManuallyComposed>();
        }
    }
}
