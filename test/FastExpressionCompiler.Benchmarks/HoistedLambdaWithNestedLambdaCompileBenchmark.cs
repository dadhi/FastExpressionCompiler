using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaWithNestedLambdaCompileBenchmark
    {
        public static X CreateX(Func<A, B, X> factory, Lazy<A> a, B b)
        {
            return factory(a.Value, b);
        }

        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
            return getXExpr;
        }

        private readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        [Benchmark]
        public Func<X> Compile()
        {
            return _hoistedExpr.Compile();
        }

        [Benchmark]
        public Func<X> CompileFast()
        {
            return ExpressionCompiler.Compile(_hoistedExpr);
        }
    }
}
