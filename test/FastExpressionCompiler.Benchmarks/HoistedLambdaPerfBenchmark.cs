using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaPerfBenchmark
    {
        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<X>> e = () => new X(a, b);
            return e;
        }

        private static readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
        private static readonly Func<X> _lambdaCompiledFast = ExpressionCompiler.Compile(_hoistedExpr);

        A a = new A();
        B b = new B();

        [Benchmark(Baseline = true)]
        public X CallConstructorDirectly()
        {
            return new X(a, b);
        }

        [Benchmark]
        public X CallLambdaCompiled()
        {
            return _lambdaCompiled();
        }

        [Benchmark]
        public X CallLambdaCompiledFast()
        {
            return _lambdaCompiledFast();
        }
    }
}
