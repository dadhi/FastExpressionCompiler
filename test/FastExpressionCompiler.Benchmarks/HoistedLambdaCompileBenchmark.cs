using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaCompileBenchmark
    {
        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<X>> e = () => new X(a, b);
            return e;
        }

        private readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        private readonly Expression<Func<object>> _manualExpr = GetManualExpr();

        private static Expression<Func<object>> GetManualExpr()
        {
            var a = new A();
            var b = new B();
            var e = Expression.Lambda<Func<object>>(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                Expression.Constant(a, typeof(A)),
                Expression.Constant(b, typeof(B))));
            return e;
        }

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
