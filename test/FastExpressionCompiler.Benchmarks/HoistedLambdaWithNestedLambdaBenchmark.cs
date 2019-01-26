using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

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
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             ExpressionFastCompile |  31.49 us | 0.3165 us | 0.2960 us |  1.00 |    0.00 |      1.5869 |      0.7935 |      0.1221 |             7.27 KB |
                 ExpressionCompile | 468.35 us | 0.7612 us | 0.6748 us | 14.86 |    0.15 |      2.4414 |      0.9766 |           - |            11.95 KB |

             */
            [Benchmark]
            public Func<X> ExpressionCompile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public Func<X> ExpressionFastCompile() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Invocation
        {
            /*
            ## 26.01.2019: V2

                         Method |        Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------- |------------:|-----------:|-----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
               DirectMethodCall |    50.57 ns |  0.1004 ns |  0.0838 ns |  0.67 |    0.01 |      0.0356 |           - |           - |               168 B |
             FastCompiledLambda |    75.09 ns |  1.1515 ns |  0.9615 ns |  1.00 |    0.00 |      0.0474 |           - |           - |               224 B |
                 CompiledLambda | 1,522.61 ns | 22.7070 ns | 21.2402 ns | 20.29 |    0.39 |      0.0553 |           - |           - |               264 B |

             */
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
