using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaWithNestedLambdaBenchmark
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

        private static readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        [MarkdownExporter, MemoryDiagnoser]
        public class Compile
        {
            [Benchmark]
            public Func<X> Expr()
            {
                return _hoistedExpr.Compile();
            }

            [Benchmark(Baseline = true)]
            public Func<X> Fast()
            {
                return ExpressionCompiler.Compile(_hoistedExpr);
            }
        }

        [MarkdownExporter, MemoryDiagnoser]
        public class Invoke
        {
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = ExpressionCompiler.Compile(_hoistedExpr);

            A aa = new A();
            B bb = new B();

            [Benchmark(Baseline = true)]
            public X Method()
            {
                return CreateX((a, b) => new X(a, b), new Lazy<A>(() => aa), bb);
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
