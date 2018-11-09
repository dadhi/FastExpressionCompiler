using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

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

        [MemoryDiagnoser]
        [MarkdownExporter]
        [CoreJob]
        public class Compilation
        {
            [Benchmark]
            public object Compile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        [MarkdownExporter]
        [CoreJob]
        public class Invocation
        {
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = _hoistedExpr.CompileFast();

            private readonly A _aa = new A();
            private readonly B _bb = new B();

            [Benchmark]
            public object DirectConstructorCall() => 
                new X(_aa, _bb);

            [Benchmark]
            public object CompiledLambda() => 
                _lambdaCompiled();

            [Benchmark(Baseline = true)]
            public object FastCompiledLambda() => 
                _lambdaCompiledFast();
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
