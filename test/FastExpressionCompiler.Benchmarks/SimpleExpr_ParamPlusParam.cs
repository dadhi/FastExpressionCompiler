using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Security;
using BenchmarkDotNet.Attributes;

[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser, DisassemblyDiagnoser(printIL: true)]
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
        private static readonly Func<int, int, int> _sumExprCompiledFast = CreateSumExpr().CompileFast();
        private static readonly Func<int, int, int> _manuallyEmitted = ManualEmit();

        private static readonly int _a = 66;
        private static readonly int _b = 34;

        //[Benchmark(/*OperationsPerInvoke = 50, */Baseline = true)]
        public int Inlined() => _a + _b;

        //[Benchmark(/*OperationsPerInvoke = 50*/)]
        public int Lambda() => _sumExprLambda(_a, _b);

        [Benchmark(/*OperationsPerInvoke = 50*/Baseline = true)]
        public int ExprCompiled() => _sumExprCompiled(_a, _b);

        [Benchmark(/*OperationsPerInvoke = 50*/)]
        public int ExprCompileFast() => _sumExprCompiledFast(_a, _b);

        [Benchmark(/*OperationsPerInvoke = 50*/)]
        public int ManuallyEmitted() => _manuallyEmitted(_a, _b);
    }
}
