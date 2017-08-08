using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaBenchmark
    {
        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<X>> e = () => new X(a, b);
            return e;
        }

        private static readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        [MemoryDiagnoser, MarkdownExporter]
        public class Compile
        {
            [Benchmark]
            public object ExpressionCompile()
            {
                return _hoistedExpr.Compile();
            }

            [Benchmark(Baseline = true)]
            public object ExpressionCompileFast()
            {
                return _hoistedExpr.CompileFast();
            }
        }

        [MemoryDiagnoser, MarkdownExporter]
        public class Invoke
        {
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = _hoistedExpr.CompileFast();

            A aa = new A();
            B bb = new B();

            [Benchmark]
            public object DirectConstructorCall()
            {
                return new X(aa, bb);
            }

            [Benchmark]
            public object CompiledLambda()
            {
                return _lambdaCompiled();
            }

            [Benchmark(Baseline = true)]
            public object FastCompiledLambda()
            {
                return _lambdaCompiledFast();
            }
        }

        #region SUT

        public class A { }
        public class B { }

        public class X
        {
            public A A { get; }
            public B B { get; }

            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }

        #endregion
    }
}
