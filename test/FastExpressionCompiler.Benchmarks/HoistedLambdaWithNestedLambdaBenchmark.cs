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
                 Compile | 481.33 us | 0.6025 us | 0.5031 us | 29.47 |    0.09 | 2.4414 | 0.9766 |      - |  11.95 KB |
             CompileFast |  16.33 us | 0.0555 us | 0.0492 us |  1.00 |    0.00 | 1.0986 | 0.5493 | 0.0916 |   5.13 KB |

            ## v3.0-preview-02

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=3.1.403
            [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
            DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

            |      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 460.63 us | 5.937 us | 5.263 us | 27.47 |    0.67 | 2.4414 | 0.9766 |      - |  11.65 KB |
            | CompileFast |  16.77 us | 0.324 us | 0.485 us |  1.00 |    0.00 | 1.1902 | 0.5493 | 0.0916 |   4.86 KB |

            ## v3.0-preview-05

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

            |      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 479.87 us | 5.039 us | 4.208 us | 31.98 |    0.59 | 2.9297 | 1.4648 |      - |  12.17 KB |
            | CompileFast |  15.00 us | 0.291 us | 0.298 us |  1.00 |    0.00 | 1.1902 | 0.5493 | 0.0916 |   4.86 KB |

            */
            [Benchmark]
            public Func<X> Compile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public Func<X> CompileFast() => _hoistedExpr.CompileFast();
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

            ## v3.0-preview-02

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=3.1.403
            [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
            DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

            |              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |    DirectMethodCall |    53.90 ns |  0.982 ns |  0.918 ns |  1.06 |    0.02 | 0.0401 |     - |     - |     168 B |
            |     Invoke_Compiled | 1,452.80 ns | 16.283 ns | 15.232 ns | 28.44 |    0.37 | 0.0629 |     - |     - |     264 B |
            | Invoke_CompiledFast |    51.11 ns |  0.935 ns |  0.829 ns |  1.00 |    0.00 | 0.0249 |     - |     - |     104 B |

            ## v3.0-preview-05

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


            |              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |    DirectMethodCall |    53.24 ns |  0.721 ns |  0.674 ns |  1.06 |    0.02 | 0.0401 |     - |     - |     168 B |
            |     Invoke_Compiled | 1,486.71 ns | 13.620 ns | 12.741 ns | 29.64 |    0.25 | 0.0629 |     - |     - |     264 B |
            | Invoke_CompiledFast |    50.20 ns |  0.484 ns |  0.404 ns |  1.00 |    0.00 | 0.0248 |     - |     - |     104 B |

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
