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

            ## v3.3

            |      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 487.48 us | 8.819 us | 8.250 us | 30.60 |    0.70 | 1.9531 | 0.9766 |      - |  12.05 KB |
            | CompileFast |  15.94 us | 0.237 us | 0.210 us |  1.00 |    0.00 | 0.8850 | 0.4272 | 0.0916 |   5.45 KB |

            ## v3.3.1

            |      Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |     Compile | 641.72 us | 12.785 us | 26.117 us | 28.87 |    1.78 | 3.9063 | 1.9531 |      - |  12.05 KB |
            | CompileFast |  22.31 us |  0.444 us |  0.876 us |  1.00 |    0.00 | 1.7700 | 0.8850 | 0.1221 |   5.45 KB |

            ## v4.0.0

            BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.2134/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 7.0.307
            [Host]     : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2
            DefaultJob : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2

            |      Method |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            |     Compile | 401.40 us | 7.509 us | 5.863 us | 400.79 us | 35.56 |    1.14 | 1.9531 | 0.9766 |  12.11 KB |        2.66 |
            | CompileFast |  11.47 us | 0.228 us | 0.509 us |  11.28 us |  1.00 |    0.00 | 0.7324 | 0.7172 |   4.55 KB |        1.00 |

            ## v4.0.0 release + net 8.0

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.100-rc.2.23502.2
            [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

            | Method      | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile     | 442.02 us | 8.768 us | 21.998 us | 40.00 |    2.34 | 1.9531 | 0.9766 |  12.04 KB |        2.61 |
            | CompileFast |  11.06 us | 0.221 us |  0.441 us |  1.00 |    0.00 | 0.7324 | 0.7019 |   4.62 KB |        1.00 |


            ## v5.0.0 + net 9.0

            BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
            Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            .NET SDK 9.0.100
            [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
            DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

            | Method      | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile     | 421.09 us | 8.382 us | 18.221 us | 413.02 us | 36.29 |    2.09 | 1.9531 | 0.9766 |  12.04 KB |        2.61 |
            | CompileFast |  11.62 us | 0.230 us |  0.464 us |  11.42 us |  1.00 |    0.06 | 0.7324 | 0.7019 |   4.62 KB |        1.00 |


            ## v5.3.0 ILGenerator pooling

            | Method      | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile     | 410.18 us | 6.928 us | 5.785 us | 36.97 |    0.92 | 1.9531 | 1.4648 |  12.04 KB |        2.74 |
            | CompileFast |  11.10 us | 0.214 us | 0.237 us |  1.00 |    0.03 | 0.7019 | 0.6714 |    4.4 KB |        1.00 |

            ## v5.3.0 ILGenerator pooling for the nested lambdas too

            | Method      | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile     | 413.38 us | 5.859 us | 5.480 us | 39.90 |    0.88 | 1.9531 | 1.4648 |  12.04 KB |        3.06 |
            | CompileFast |  10.36 us | 0.195 us | 0.191 us |  1.00 |    0.03 | 0.6409 | 0.6104 |   3.93 KB |        1.00 |

            ## v5.3.0 ILGenerator+SignaturHelper pooling for the nested lambdas too

            | Method      | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |------------ |----------:|---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile     | 437.16 us | 8.631 us | 20.004 us | 438.94 us | 39.26 |    3.33 | 1.9531 | 0.9766 |  12.04 KB |        3.19 |
            | CompileFast |  11.20 us | 0.329 us |  0.896 us |  10.93 us |  1.01 |    0.11 | 0.6104 | 0.5951 |   3.77 KB |        1.00 |

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

            ## v3.3.1

            |              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |    DirectMethodCall |    67.15 ns |  1.401 ns |  1.965 ns |  1.06 |    0.05 | 0.0535 |     - |     - |     168 B |
            |     Invoke_Compiled | 1,889.47 ns | 37.145 ns | 53.272 ns | 29.75 |    1.44 | 0.0839 |     - |     - |     264 B |
            | Invoke_CompiledFast |    63.21 ns |  1.239 ns |  2.203 ns |  1.00 |    0.00 | 0.0331 |     - |     - |     104 B |

            ## v4.0.0

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.100-rc.2.23502.2
            [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

            | Method              | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |-------------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
            | DirectMethodCall    |    35.51 ns |  0.783 ns |  1.308 ns |  0.86 |    0.08 | 0.0267 |     168 B |        1.62 |
            | Invoke_Compiled     | 1,096.15 ns | 21.507 ns | 41.437 ns | 27.15 |    2.75 | 0.0420 |     264 B |        2.54 |
            | Invoke_CompiledFast |    37.65 ns |  1.466 ns |  4.299 ns |  1.00 |    0.00 | 0.0166 |     104 B |        1.00 |


            ## v5.0.0 + net 9.0

            BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
            Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            .NET SDK 9.0.100
            [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
            DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

            | Method              | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |-------------------- |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
            | DirectMethodCall    |    43.45 ns |  0.922 ns |  1.905 ns |    44.13 ns |  1.09 |    0.08 | 0.0268 |     168 B |        1.62 |
            | Invoke_Compiled     | 1,181.25 ns | 23.664 ns | 56.240 ns | 1,161.87 ns | 29.66 |    2.24 | 0.0420 |     264 B |        2.54 |
            | Invoke_CompiledFast |    39.96 ns |  0.856 ns |  2.442 ns |    38.96 ns |  1.00 |    0.08 | 0.0166 |     104 B |        1.00 |

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
