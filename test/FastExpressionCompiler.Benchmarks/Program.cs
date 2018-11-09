using BenchmarkDotNet.Running;

namespace FastExpressionCompiler.Benchmarks
{
    
    public class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<NestedLambdaOverhead>();

            //BenchmarkRunner.Run<FEC_vs_ManualEmit_vs_Activator>();

            //BenchmarkRunner.Run<MultipleNestedLambdaExprVsExprSharing>();

            //BenchmarkRunner.Run<ObjectExecutor_SyncMethod_Compile>();
            //BenchmarkRunner.Run<ObjectExecutor_SyncMethod_Execute>();

            //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_CreateExecutor>();
            //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_ExecuteAsync>();
            //BenchmarkRunner.Run<ObjectExecutor_AsyncMethod_Execute>();
            //BenchmarkRunner.Run<StaticTypeOfSwitch>();

            //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Compilation>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_TryCatchExpr.Invocation>();

            //BenchmarkRunner.Run<SimpleExpr_ParamPlusParam>();

            //BenchmarkRunner.Run<ExprInfoVsExpr_Create_ComplexExpr>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_ComplexExpr>();
            //BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_NestedLambdaExpr>();
            BenchmarkRunner.Run<ExprInfoVsExpr_CreateAndCompile_SimpleExpr>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Compilation>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invocation>();

            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Compilation>();
            //BenchmarkRunner.Run<HoistedLambdaWithNestedLambdaBenchmark.Invocation>();

            //BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Invoke>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark_LogicalOps.Compile>();
            //BenchmarkRunner.Run<HoistedLambdaBenchmark.Invoke>();

            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.Compilation>();
            //BenchmarkRunner.Run<ManuallyComposedLambdaBenchmark.InvokeManuallyComposed>();
        }
    }
}
