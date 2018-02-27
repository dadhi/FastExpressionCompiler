using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace FastExpressionCompiler.Benchmarks
{
    [ClrJob, CoreJob, MemoryDiagnoser, DisassemblyDiagnoser(printIL: true, recursiveDepth: 20)]
    public class SimpleExpr_ParamPlusParam
    {
        private static Expression<Func<int, int, int>> CreateSumExpr()
        {
            var aExp = Expression.Parameter(typeof(int), "a");
            var bExp = Expression.Parameter(typeof(int), "b");
            return Expression.Lambda<Func<int, int, int>>(Expression.Add(aExp, bExp), aExp, bExp);
        }

        private static ExpressionInfo<Func<int, int, int>> CreateSumExprInfo()
        {
            var aExp = ExpressionInfo.Parameter(typeof(int), "a");
            var bExp = ExpressionInfo.Parameter(typeof(int), "b");
            return ExpressionInfo.Lambda<Func<int, int, int>>(
                ExpressionInfo.Add(aExp, bExp), aExp, bExp);
        }

        private static Func<int, int, int> SumExpr_Lambda = (a, b) => a + b;
        private static Func<int, int, int> SumExpr_Compiled = CreateSumExpr().Compile();
        private static Func<int, int, int> SumExpr_CompiledFast = CreateSumExpr().CompileFast();
        private static Func<int, int, int> SumExprInfo_CompiledFast = CreateSumExprInfo().CompileFast();

        private static int A = 66;
        private static int B = 34;

        [Benchmark(OperationsPerInvoke = 50, Baseline = true)]
        public int Inlined() => A + B;

        [Benchmark(OperationsPerInvoke = 50)]
        public int Lambda() => SumExpr_Lambda(A, B);

        [Benchmark(OperationsPerInvoke = 50)]
        public int Expr_Compiled() => SumExpr_Compiled(A, B);

        [Benchmark(OperationsPerInvoke = 50)]
        public int Expr_CompileFast() => SumExpr_CompiledFast(A, B);

        [Benchmark(OperationsPerInvoke = 50)]
        public int ExpressionInfo_CompileFast() => SumExprInfo_CompiledFast(A, B);
    }
}
