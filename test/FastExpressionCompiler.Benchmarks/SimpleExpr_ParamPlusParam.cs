using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    /*
|                         Method |     Mean |     Error |    StdDev | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |---------:|----------:|----------:|------:|------:|------:|------:|----------:|
|                   ExprCompiled | 3.602 ns | 0.0031 ns | 0.0026 ns |  1.00 |     - |     - |     - |         - |
|                ExprCompileFast | 1.771 ns | 0.0049 ns | 0.0044 ns |  0.49 |     - |     - |     - |         - |
| ExprCompileFast_WithoutClosure | 1.789 ns | 0.0050 ns | 0.0047 ns |  0.50 |     - |     - |     - |         - |
     */

    [MemoryDiagnoser, DisassemblyDiagnoser()]
    public class SimpleExpr_ParamPlusParam
    {
        private static Expression<Func<int, int, int>> CreateSumExpr()
        {
            var aExp = Expression.Parameter(typeof(int), "a");
            var bExp = Expression.Parameter(typeof(int), "b");
            return Expression.Lambda<Func<int, int, int>>(Expression.Add(aExp, bExp), aExp, bExp);
        }

        private static Func<int, int, int> ManualEmit()
        {
            var closure = new ExpressionCompiler.ArrayClosure(Array.Empty<object>());

            var method = new DynamicMethod(Guid.NewGuid().ToString(), 
                typeof(int), new [] { closure.GetType(), typeof(int), typeof(int) }, 
                closure.GetType(), true);

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ret);

            return (Func<int, int, int>)method.CreateDelegate(typeof(Func<int, int, int>), closure);
        }

        private static readonly Func<int, int, int> _sumExprLambda = (a, b) => a + b;
        private static readonly Func<int, int, int> _sumExprCompiled = CreateSumExpr().Compile();
        private static readonly Func<int, int, int> _sumExprCompiledFast = CreateSumExpr().CompileFast(true);
        private static readonly Func<int, int, int> _sumExprCompiledFastWithoutClosure = CreateSumExpr().TryCompileWithoutClosure<Func<int, int, int>>();
        private static readonly Func<int, int, int> _manuallyEmitted = ManualEmit();

        private static readonly int _a = 66;
        private static readonly int _b = 34;

        //[Benchmark(/*OperationsPerInvoke = 50, */Baseline = true)]
        public int Inlined() => _a + _b;

        //[Benchmark(Baseline = true)]
        public int Lambda() => _sumExprLambda(_a, _b);

        //[Benchmark]
        [Benchmark(Baseline = true)]
        public int ExprCompiled() => _sumExprCompiled(_a, _b);

        [Benchmark]
        public int ExprCompileFast() => _sumExprCompiledFast(_a, _b);

        //[Benchmark]
        public int ExprCompileFast_WithoutClosure() => _sumExprCompiledFastWithoutClosure(_a, _b);

        //[Benchmark()]
        public int ManuallyEmitted() => _manuallyEmitted(_a, _b);
    }
}
