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

            ## v3.0 - after fixing the nested lambda

                             Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            ----------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                 Expression_Compile | 481.33 us | 0.6025 us | 0.5031 us | 29.47 |    0.09 | 2.4414 | 0.9766 |      - |  11.95 KB |
             Expression_CompileFast |  16.33 us | 0.0555 us | 0.0492 us |  1.00 |    0.00 | 1.0986 | 0.5493 | 0.0916 |   5.13 KB |

            ## v3.0

                             Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            ----------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                 Expression_Compile | 470.00 us | 1.3111 us | 1.1622 us | 29.65 |    0.26 | 2.4414 | 0.9766 |      - |  11.95 KB |
             Expression_CompileFast |  15.86 us | 0.1507 us | 0.1410 us |  1.00 |    0.00 | 1.0376 | 0.5188 | 0.0305 |   4.77 KB |

             */
            [Benchmark]
            public Func<X> Expression_Compile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public Func<X> Expression_CompileFast() => _hoistedExpr.CompileFast();
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

            |              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |    DirectMethodCall |    50.78 ns | 0.1651 ns | 0.1544 ns |  0.92 |    0.01 | 0.0356 |     - |     - |     168 B |
            |     Invoke_Compiled | 1,385.24 ns | 2.8196 ns | 2.6375 ns | 25.10 |    0.33 | 0.0553 |     - |     - |     264 B |
            | Invoke_CompiledFast |    55.20 ns | 0.8883 ns | 0.7875 ns |  1.00 |    0.00 | 0.0220 |     - |     - |     104 B |

            ## v3.0

            |              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |    DirectMethodCall |    49.95 ns | 0.0873 ns | 0.0817 ns |  1.06 |    0.00 | 0.0356 |     - |     - |     168 B |
            |     Invoke_Compiled | 1,355.80 ns | 4.6952 ns | 4.3919 ns | 28.75 |    0.10 | 0.0553 |     - |     - |     264 B |
            | Invoke_CompiledFast |    47.17 ns | 0.0356 ns | 0.0316 ns |  1.00 |    0.00 | 0.0220 |     - |     - |     104 B |

            */
            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = _hoistedExpr.CompileFast();

            private readonly A _aa = new A();
            private readonly B _bb = new B();

            [Benchmark]
            public X DirectMethodCall() => 
                CreateX((a, b) => new X(a, b), new Lazy<A>(() => _aa), _bb);

            [Benchmark]
            public X Invoke_Compiled() => 
                _lambdaCompiled();

            [Benchmark(Baseline = true)]
            public X Invoke_CompiledFast() => 
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
