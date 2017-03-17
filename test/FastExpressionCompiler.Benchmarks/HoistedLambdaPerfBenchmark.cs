using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaPerfBenchmark
    {
        private static Expression<Func<object>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<object>> e = () => new X(a, b);
            return e;
        }

        private static readonly Expression<Func<object>> _hoistedExpr = GetHoistedExpr();

        private static readonly Func<object> _lambdaCompiled = _hoistedExpr.Compile();
        private static readonly Func<object> _lambdaCompiledFast = ExpressionCompiler.Compile(_hoistedExpr);

        A a = new A();
        B b = new B();

        [Benchmark(Baseline = true)]
        public object CallConstructorDirectly()
        {
            return new X(a, b);
        }

        [Benchmark]
        public object CallLambdaCompiled()
        {
            return _lambdaCompiled();
        }

        [Benchmark]
        public object CallLambdaCompiledFast()
        {
            return _lambdaCompiledFast();
        }
    }
}
