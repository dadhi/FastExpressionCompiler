using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaBenchmark_LogicalOps
    {
        private static Expression<Func<bool>> Get_and_with_or_Expr()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        private static Func<bool> Get_and_with_or_Lambda()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        private static readonly Expression<Func<bool>> _and_with_or_expr = Get_and_with_or_Expr();

        [MemoryDiagnoser]
        public class Compile
        {
            [Benchmark]
            public object Compile_()
            {
                return _and_with_or_expr.Compile();
            }

            [Benchmark]
            public object CompileFast()
            {
                return _and_with_or_expr.CompileFast();
            }
        }

        [MemoryDiagnoser, DisassemblyDiagnoser()]
        public class Invoke
        {
            private static readonly Func<bool> _lambda = Get_and_with_or_Lambda();
            private static readonly Func<bool> _expr_Compiled = _and_with_or_expr.Compile();
            private static readonly Func<bool> _expr_CompiledFast = _and_with_or_expr.CompileFast();

            [Benchmark(Baseline = true)]
            public object Lambda() => _lambda();

            [Benchmark]
            public object Expr_Compiled() => _expr_Compiled();

            [Benchmark]
            public object Expr_CompiledFast() => _expr_CompiledFast();
        }
    }
}
