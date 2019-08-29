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
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 Compile | 242.974 us | 1.4929 us | 1.3964 us | 30.39 |    0.26 |      0.7324 |      0.2441 |           - |             4.45 KB |
             CompileFast |   7.996 us | 0.0638 us | 0.0565 us |  1.00 |    0.00 |      0.4883 |      0.2441 |      0.0305 |             2.26 KB |

            ## v2.1 With ArrayClosure object

                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 Compile | 219.809 us | 0.9782 us | 0.9150 us | 46.35 |    0.23 |      0.7324 |      0.2441 |           - |             4.45 KB |
             CompileFast |   4.743 us | 0.0212 us | 0.0188 us |  1.00 |    0.00 |      0.3815 |      0.1907 |      0.0305 |             1.77 KB |

            ## v2.1 With typed Closure object

                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 Compile | 221.281 us | 0.9760 us | 0.8652 us | 30.16 |    0.25 |      0.7324 |      0.2441 |           - |             4.45 KB |
             CompileFast |   7.337 us | 0.0628 us | 0.0557 us |  1.00 |    0.00 |      0.4883 |      0.2441 |      0.0305 |             2.26 KB |

            ## v2.1 with LiveCountArray for Constants

                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                 Compile | 218.716 us | 1.1565 us | 1.0252 us | 30.12 |    0.17 |      0.7324 |      0.2441 |           - |             4.45 KB |
             CompileFast |   7.255 us | 0.0383 us | 0.0359 us |  1.00 |    0.00 |      0.4883 |      0.2441 |      0.0381 |             2.23 KB |

            ## v3.0
            
                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            ------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                 Compile | 246.296 us | 0.5909 us | 0.5527 us | 52.89 |    0.30 | 0.7324 | 0.2441 |      - |   4.45 KB |
             CompileFast |   4.657 us | 0.0273 us | 0.0242 us |  1.00 |    0.00 | 0.3357 | 0.1678 | 0.0229 |   1.55 KB |
             */

            [Benchmark]
            public object Compile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        public class Invocation
        {
            /*
            ## 26.01.2019: V2

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             DirectConstructorCall |  6.203 ns | 0.1898 ns | 0.3470 ns |  0.76 |    0.06 |      0.0068 |           - |           - |                32 B |
                    CompiledLambda | 12.313 ns | 0.1124 ns | 0.1052 ns |  1.57 |    0.04 |      0.0068 |           - |           - |                32 B |
                FastCompiledLambda |  7.840 ns | 0.2010 ns | 0.1881 ns |  1.00 |    0.00 |      0.0068 |           - |           - |                32 B |

            ## v2.1 With ArrayClosure object

                            Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
             DirectConstructorCall | 4.983 ns | 0.0459 ns | 0.0429 ns |  0.68 |      0.0068 |           - |           - |                32 B |
                    CompiledLambda | 9.818 ns | 0.0425 ns | 0.0397 ns |  1.34 |      0.0068 |           - |           - |                32 B |
                FastCompiledLambda | 7.351 ns | 0.0277 ns | 0.0259 ns |  1.00 |      0.0068 |           - |           - |                32 B |

            ## v2.1 With typed Closure object

                            Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
             DirectConstructorCall | 5.012 ns | 0.0330 ns | 0.0308 ns |  0.82 |      0.0068 |           - |           - |                32 B |
                    CompiledLambda | 9.632 ns | 0.0411 ns | 0.0384 ns |  1.57 |      0.0068 |           - |           - |                32 B |
                FastCompiledLambda | 6.131 ns | 0.0348 ns | 0.0308 ns |  1.00 |      0.0068 |           - |           - |                32 B |

            ## v3.0
            
                            Method |      Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            ---------------------- |----------:|----------:|----------:|------:|-------:|------:|------:|----------:|
             DirectConstructorCall |  5.939 ns | 0.0134 ns | 0.0112 ns |  0.77 | 0.0068 |     - |     - |      32 B |
                    CompiledLambda | 11.880 ns | 0.0291 ns | 0.0243 ns |  1.54 | 0.0068 |     - |     - |      32 B |
                FastCompiledLambda |  7.723 ns | 0.0255 ns | 0.0239 ns |  1.00 | 0.0068 |     - |     - |      32 B |

             */

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
