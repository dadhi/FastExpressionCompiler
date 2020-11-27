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

            ## v3.0-preview-02
            
            |      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 233.935 us | 1.2937 us | 1.1468 us | 47.06 |    0.97 | 0.9766 | 0.4883 |      - |   4.35 KB |
            | CompileFast |   4.995 us | 0.0994 us | 0.1184 us |  1.00 |    0.00 | 0.3815 | 0.1907 | 0.0305 |   1.57 KB |

            ## v3.0-preview-05
            
            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


            |      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 274.722 us | 5.3167 us | 5.6888 us | 47.47 |    1.67 | 0.9766 | 0.4883 |      - |   4.52 KB |
            | CompileFast |   5.790 us | 0.1118 us | 0.1197 us |  1.00 |    0.00 | 0.3815 | 0.1907 | 0.0305 |   1.57 KB |

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

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=3.1.403
            [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
            DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


            |                Method |      Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |---------------------- |----------:|----------:|----------:|------:|-------:|------:|------:|----------:|
            | DirectConstructorCall |  5.781 ns | 0.1115 ns | 0.1043 ns |  0.51 | 0.0076 |     - |     - |      32 B |
            |        CompiledLambda | 12.581 ns | 0.1318 ns | 0.1169 ns |  1.11 | 0.0076 |     - |     - |      32 B |
            |    FastCompiledLambda | 11.338 ns | 0.1075 ns | 0.1005 ns |  1.00 | 0.0076 |     - |     - |      32 B |

            ## v3.0-preview-05
            
            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


            |                Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |---------------------- |----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            | DirectConstructorCall |  7.634 ns | 0.2462 ns | 0.2303 ns |  0.54 |    0.02 | 0.0076 |     - |     - |      32 B |
            |        CompiledLambda | 15.553 ns | 0.1805 ns | 0.1600 ns |  1.09 |    0.02 | 0.0076 |     - |     - |      32 B |
            |    FastCompiledLambda | 14.241 ns | 0.2844 ns | 0.2521 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      32 B |

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
