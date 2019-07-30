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
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 ExpressionCompile | 468.35 us | 0.7612 us | 0.6748 us | 14.86 |    0.15 |      2.4414 |      0.9766 |           - |            11.95 KB |
             ExpressionCompileFast |  31.49 us | 0.3165 us | 0.2960 us |  1.00 |    0.00 |      1.5869 |      0.7935 |      0.1221 |             7.27 KB |

            ## v2.1 with Array Closure

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 ExpressionCompile | 453.40 us | 3.3855 us | 3.1668 us | 23.51 |    0.23 |      2.4414 |      0.9766 |           - |            11.95 KB |
             ExpressionCompileFast |  19.28 us | 0.1465 us | 0.1370 us |  1.00 |    0.00 |      1.3123 |      0.6409 |      0.1221 |             6.02 KB |

            ## v3 - after fixing the nested lambda

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            ---------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                 ExpressionCompile | 471.15 us | 2.0357 us | 1.9042 us | 29.04 |    0.34 | 2.4414 | 0.9766 |      - |  11.95 KB |
             ExpressionCompileFast |  16.23 us | 0.1996 us | 0.1867 us |  1.00 |    0.00 | 1.1292 | 0.5493 | 0.0916 |    5.2 KB |

             */
            [Benchmark]
            public Func<X> ExpressionCompile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public Func<X> ExpressionCompileFast() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        public class Invocation
        {
            /*
            ## 26.01.2019: V2

                         Method |        Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------- |------------:|-----------:|-----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 CompiledLambda | 1,522.61 ns | 22.7070 ns | 21.2402 ns | 20.29 |    0.39 |      0.0553 |           - |           - |               264 B |
             FastCompiledLambda |    75.09 ns |  1.1515 ns |  0.9615 ns |  1.00 |    0.00 |      0.0474 |           - |           - |               224 B |
               DirectMethodCall |    50.57 ns |  0.1004 ns |  0.0838 ns |  0.67 |    0.01 |      0.0356 |           - |           - |               168 B |

            ## v2.1 with Array Closure

                         Method |        Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------- |------------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 CompiledLambda | 1,349.96 ns | 3.1724 ns | 2.8122 ns | 17.47 |    0.22 |      0.0553 |           - |           - |               264 B |
             FastCompiledLambda |    77.35 ns | 0.9274 ns | 0.8675 ns |  1.00 |    0.00 |      0.0542 |           - |           - |               256 B |
               DirectMethodCall |    47.21 ns | 0.0766 ns | 0.0598 ns |  0.61 |    0.01 |      0.0356 |           - |           - |               168 B |

            ## v3.0 with fixed nested lambdas

                         Method |        Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            ------------------- |------------:|-----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
                 CompiledLambda | 1,394.55 ns | 17.9330 ns | 16.7746 ns | 25.92 |    0.57 | 0.0553 |     - |     - |     264 B |
             FastCompiledLambda |    53.83 ns |  1.0364 ns |  0.9187 ns |  1.00 |    0.00 | 0.0220 |     - |     - |     104 B |
               DirectMethodCall |    51.15 ns |  0.4053 ns |  0.3593 ns |  0.95 |    0.02 | 0.0356 |     - |     - |     168 B |

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
