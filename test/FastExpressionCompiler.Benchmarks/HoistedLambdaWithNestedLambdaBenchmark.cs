using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaWithNestedLambdaBenchmark
    {
        public static X CreateX(Func<A, B, X> factory, Lazy<A> a, B b) => 
            factory(a.Value, b);

        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            return () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
        }

        private static readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        [MemoryDiagnoser]
        [MarkdownExporter]
        [ClrJob, CoreJob]
        public class Compilation
        {
            [Benchmark]
            public Func<X> ExpressionCompile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public Func<X> ExpressionFastCompile() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        [MarkdownExporter]
        [ClrJob, CoreJob]
        public class Invocation
        {
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = _hoistedExpr.CompileFast();

            private readonly A _aa = new A();
            private readonly B _bb = new B();

            [Benchmark]
            public X DirectMethodCall() => 
                CreateX((a, b) => new X(a, b), new Lazy<A>(() => _aa), _bb);

            [Benchmark]
            public X CompiledLambda() => 
                _lambdaCompiled();

            [Benchmark(Baseline = true)]
            public X FastCompiledLambda() => 
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
