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

        [MarkdownExporter]
        public class Compile
        {
            [Benchmark]
            public Func<X> Compile_()
            {
                return _hoistedExpr.Compile();
            }

            [Benchmark]
            public Func<X> CompileFast()
            {
                return ExpressionCompiler.Compile(_hoistedExpr);
            }
        }

        [MarkdownExporter]
        public class Invoke
        {
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = ExpressionCompiler.Compile(_hoistedExpr);

            A aa = new A();
            B bb = new B();

            [Benchmark(Baseline = true)]
            public X Constructor()
            {
                return new X(aa, bb);
            }

            [Benchmark]
            public X CompiledLambda()
            {
                return _lambdaCompiled();
            }

            [Benchmark]
            public X FastCompiledLambda()
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
