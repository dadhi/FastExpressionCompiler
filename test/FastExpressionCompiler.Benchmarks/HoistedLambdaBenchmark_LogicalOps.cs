using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaBenchmark_LogicalOps
    {
        private static Expression<Func<bool>> And_with_or()
        {
            var x = 1;
            var s = "Test";
            return () => x == 1 && (s.Contains("S") || s.Contains("s"));
        }

        private static readonly Expression<Func<bool>> _hoistedExpr = And_with_or();

        [MarkdownExporter]
        public class Compile
        {
            [Benchmark]
            public object Compile_()
            {
                return _hoistedExpr.Compile();
            }

            [Benchmark]
            public object CompileFast()
            {
                return ExpressionCompiler.Compile(_hoistedExpr);
            }
        }

        [MarkdownExporter]
        public class Invoke
        {
            private static readonly Func<bool> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<bool> _lambdaCompiledFast = ExpressionCompiler.Compile(_hoistedExpr);

            [Benchmark]
            public object CompiledLambda()
            {
                return _lambdaCompiled();
            }

            [Benchmark]
            public object FastCompiledLambda()
            {
                return _lambdaCompiledFast();
            }
        }
    }
}
