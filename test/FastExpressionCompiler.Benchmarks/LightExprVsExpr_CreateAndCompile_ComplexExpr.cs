using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.LightExpression.UnitTests;
using LE = FastExpressionCompiler.LightExpression.ExpressionCompiler;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class LightExprVsExpr_CreateAndCompile_ComplexExpr
    {
        /**
        ## 25.01.2019
         
        BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
        Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        Frequency=2156255 Hz, Resolution=463.7670 ns, Timer=TSC
        .NET Core SDK=2.2.100
          [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
          DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


                                        Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        -------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
         CreateLightExpression_and_CompileFast |  12.68 us | 0.0555 us | 0.0492 us |  1.00 |    0.00 |      1.4343 |      0.7172 |      0.0458 |             6.61 KB |
              CreateExpression_and_CompileFast |  19.26 us | 0.2559 us | 0.2268 us |  1.52 |    0.02 |      1.5564 |      0.7629 |      0.0305 |             7.23 KB |
                  CreateExpression_and_Compile | 260.67 us | 1.7431 us | 1.6305 us | 20.54 |    0.14 |      1.4648 |      0.4883 |           - |             7.16 KB |

        ## v2.1
                                        Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        -------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
         CreateLightExpression_and_CompileFast |  11.82 us | 0.0735 us | 0.0652 us |  1.00 |    0.00 |      1.5259 |      0.7629 |      0.0458 |             7.02 KB |
              CreateExpression_and_CompileFast |  17.41 us | 0.2181 us | 0.2040 us |  1.47 |    0.02 |      1.5259 |      0.7629 |      0.0305 |             7.11 KB |
                  CreateExpression_and_Compile | 239.20 us | 1.5860 us | 1.4835 us | 20.24 |    0.15 |      1.4648 |      0.7324 |           - |             7.16 KB |

        ## v3.0-preview-02

        |                                Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 241.97 us | 2.007 us | 1.877 us | 17.77 |    0.20 | 1.7090 | 0.7324 |      - |   7.01 KB |
        |      CreateExpression_and_CompileFast |  17.30 us | 0.207 us | 0.173 us |  1.27 |    0.02 | 1.7395 | 0.8545 | 0.0305 |   7.19 KB |
        | CreateLightExpression_and_CompileFast |  13.61 us | 0.158 us | 0.140 us |  1.00 |    0.00 | 1.6174 | 0.7935 | 0.0305 |   6.64 KB |

        ## v3.0-preview-05

        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
        Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=5.0.100
        [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
        DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

        |                                Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 244.61 us | 4.700 us | 6.111 us | 17.92 |    0.50 | 1.7090 | 0.7324 |      - |    7.2 KB |
        |      CreateExpression_and_CompileFast |  17.69 us | 0.350 us | 0.443 us |  1.31 |    0.04 | 1.8005 | 0.8850 | 0.0305 |   7.36 KB |
        | CreateLightExpression_and_CompileFast |  13.50 us | 0.152 us | 0.143 us |  1.00 |    0.00 | 1.5869 | 0.7935 | 0.0305 |   6.58 KB |

        ## v3.1

        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
        Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        .NET Core SDK=5.0.202
        [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
        DefaultJob : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT


        |                                Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 185.081 us | 3.5893 us | 3.8406 us | 21.98 |    0.62 | 0.9766 | 0.4883 |      - |    7.2 KB |
        |      CreateExpression_and_CompileFast |  11.538 us | 0.2286 us | 0.6411 us |  1.41 |    0.10 | 1.0223 | 0.5035 | 0.0458 |   6.28 KB |
        | CreateLightExpression_and_CompileFast |   8.408 us | 0.1501 us | 0.1844 us |  1.00 |    0.00 | 0.8850 | 0.4425 | 0.0458 |   5.47 KB |

        ## v3.2

        |                                Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 239.100 us | 3.4564 us | 3.2331 us | 25.78 |    0.54 | 0.9766 | 0.4883 |      - |   7.19 KB |
        |      CreateExpression_and_CompileFast |  13.480 us | 0.2612 us | 0.2443 us |  1.45 |    0.02 | 1.0071 | 0.4883 | 0.0305 |   6.28 KB |
        | CreateLightExpression_and_CompileFast |   9.278 us | 0.1465 us | 0.1370 us |  1.00 |    0.00 | 0.8698 | 0.4272 | 0.0305 |   5.41 KB |

        ## v3.3 with net6

        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
        Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        .NET Core SDK=6.0.201
        [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
        DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT

        |                                Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 287.35 us | 5.465 us | 6.712 us | 23.80 |    0.76 | 0.9766 | 0.4883 |      - |   7.25 KB |
        |      CreateExpression_and_CompileFast |  16.02 us | 0.261 us | 0.232 us |  1.32 |    0.03 | 1.0376 | 0.5188 | 0.0305 |   6.53 KB |
        | CreateLightExpression_and_CompileFast |  12.07 us | 0.228 us | 0.272 us |  1.00 |    0.00 | 0.9003 | 0.4425 | 0.0305 |   5.58 KB |

        ## v3.3.1

        |                                               Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |----------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
        |                         CreateExpression_and_Compile | 541.65 us | 16.585 us | 47.048 us | 520.79 us | 33.98 |    3.97 | 1.9531 | 0.9766 |      - |   7.26 KB |
        |                     CreateExpression_and_CompileFast |  23.51 us |  0.724 us |  2.102 us |  23.08 us |  1.47 |    0.17 | 1.2207 | 0.6104 | 0.0305 |   3.79 KB |
        |                CreateLightExpression_and_CompileFast |  16.03 us |  0.430 us |  1.227 us |  15.50 us |  1.00 |    0.00 | 0.9155 | 0.4578 | 0.0305 |   2.84 KB |
        | CreateLightExpression_and_CompileFast_with_intrinsic |  13.94 us |  0.629 us |  1.845 us |  13.37 us |  0.88 |    0.13 | 0.8545 | 0.4272 | 0.0305 |   2.64 KB |

        ## v4.0.0

        |                                               Method |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
        |----------------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
        |                         CreateExpression_and_Compile | 174.412 us | 3.4771 us | 7.5590 us | 37.06 |    2.23 | 0.9766 | 0.4883 |   7.18 KB |        2.93 |
        |                     CreateExpression_and_CompileFast |   6.395 us | 0.1265 us | 0.2314 us |  1.36 |    0.07 | 0.5341 | 0.5264 |    3.3 KB |        1.34 |
        |                CreateLightExpression_and_CompileFast |   4.703 us | 0.0931 us | 0.1336 us |  1.00 |    0.00 | 0.3967 | 0.3891 |   2.45 KB |        1.00 |
        | CreateLightExpression_and_CompileFast_with_intrinsic |   4.430 us | 0.0627 us | 0.0490 us |  0.94 |    0.03 | 0.3891 | 0.3738 |   2.38 KB |        0.97 |

        v4.0.0 - net8.0

        BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
        11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
        .NET SDK 8.0.100-rc.2.23502.2
        [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
        DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

        | Method                                               | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
        |----------------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
        | Create_SystemExpression_and_Compile                  | 159.184 us | 2.9731 us | 7.1235 us | 37.34 |    1.65 | 0.9766 | 0.4883 |    7.4 KB |        3.06 |
        | Create_SystemExpression_and_CompileFast              |   5.923 us | 0.0996 us | 0.1771 us |  1.34 |    0.05 | 0.5188 | 0.5035 |   3.27 KB |        1.35 |
        | Create_LightExpression_and_CompileFast               |   4.399 us | 0.0484 us | 0.0453 us |  1.00 |    0.00 | 0.3815 | 0.3662 |   2.42 KB |        1.00 |
        | CreateLightExpression_and_CompileFast_with_intrinsic |   4.384 us | 0.0835 us | 0.0697 us |  1.00 |    0.02 | 0.3815 | 0.3662 |   2.35 KB |        0.97 |


        ## v5.0.0 + net9.0

        BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
        Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        .NET SDK 9.0.100
        [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
        DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2


        | Method                                               | Mean       | Error     | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
        |----------------------------------------------------- |-----------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
        | Create_SystemExpression_and_Compile                  | 212.157 us | 4.2180 us | 11.4036 us | 44.77 |    3.31 | 0.9766 | 0.4883 |   7.15 KB |        2.95 |
        | Create_SystemExpression_and_CompileFast              |   6.656 us | 0.1322 us |  0.3065 us |  1.40 |    0.10 | 0.5188 | 0.4883 |   3.27 KB |        1.35 |
        | Create_LightExpression_and_CompileFast               |   4.751 us | 0.0947 us |  0.2411 us |  1.00 |    0.07 | 0.3815 | 0.3662 |   2.42 KB |        1.00 |
        | CreateLightExpression_and_CompileFast_with_intrinsic |   4.604 us | 0.0918 us |  0.1915 us |  0.97 |    0.06 | 0.3815 | 0.3662 |   2.35 KB |        0.97 |
        */

        [Benchmark]
        public object Create_SystemExpression_and_Compile() =>
            LightExpressionTests.CreateComplexExpression().Compile();

        [Benchmark]
        public object Create_SystemExpression_and_CompileFast() =>
            LightExpressionTests.CreateComplexExpression().CompileFast();

        [Benchmark(Baseline = true)]
        public object Create_LightExpression_and_CompileFast() =>
            LE.CompileFast(LightExpressionTests.CreateComplexLightExpression());

        [Benchmark]
        public object CreateLightExpression_and_CompileFast_with_intrinsic() =>
            LE.CompileFast(LightExpressionTests.CreateComplexLightExpression_with_intrinsics());
    }

    [MemoryDiagnoser]
    public class LightExprVsExpr_Create_ComplexExpr
    {
        /*
        ## V2 baseline, 25.01.2019

                       Method  |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ---------------------- |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
         CreateLightExpression |   389.5 ns | 0.9547 ns | 0.7972 ns |  1.00 |    0.00 |      0.1693 |           - |           - |               800 B |
             CreateExpression  | 3,574.7 ns | 8.0032 ns | 7.4862 ns |  9.18 |    0.02 |      0.2823 |           - |           - |              1344 B |

        ## V3-preview-01

        |                Method |       Mean |    Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
        | CreateLightExpression |   578.5 ns |  6.39 ns |   5.98 ns |  1.00 |    0.00 | 0.1678 |     - |     - |     704 B |
        |      CreateExpression | 2,805.2 ns | 55.57 ns | 107.06 ns |  4.76 |    0.32 | 0.3090 |     - |     - |    1304 B |

        # V3-preview-02

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        | CreateLightExpression |   578.4 ns |  8.07 ns |  7.54 ns |  1.00 |    0.00 | 0.1640 |     - |     - |     688 B |
        |      CreateExpression | 3,126.9 ns | 62.14 ns | 80.80 ns |  5.46 |    0.13 | 0.3052 |     - |     - |    1304 B |

        # V3-preview-03

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        | CreateLightExpression |   334.3 ns |  4.85 ns |  4.53 ns |  1.00 |    0.00 | 0.1316 |     - |     - |     552 B |
        |      CreateExpression | 3,351.7 ns | 59.81 ns | 55.94 ns | 10.03 |    0.23 | 0.3090 |     - |     - |    1304 B |

        # V3-preview-05

        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
        Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=5.0.100
        [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
        DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        | CreateLightExpression |   284.2 ns |  5.19 ns |  4.85 ns |  1.00 |    0.00 | 0.1316 |     - |     - |     552 B |
        |      CreateExpression | 2,508.2 ns | 44.12 ns | 36.84 ns |  8.83 |    0.14 | 0.3128 |     - |     - |    1312 B |

        # V3.3.1

        BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
        Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
        .NET Core SDK=6.0.201
        [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
        DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT

        |                                Method |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |-------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
        |                      CreateExpression | 4,698.0 ns | 110.77 ns | 317.81 ns | 4,623.0 ns |  7.99 |    0.85 | 0.4501 |     - |     - |    1416 B |
        |                 CreateLightExpression |   591.2 ns |  15.42 ns |  44.98 ns |   580.7 ns |  1.00 |    0.00 | 0.1574 |     - |     - |     496 B |
        | CreateLightExpression_with_intrinsics |   580.2 ns |  16.95 ns |  48.08 ns |   565.0 ns |  0.98 |    0.10 | 0.1554 |     - |     - |     488 B |

        v4.0.0 - net8.0

        BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
        11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
        .NET SDK 8.0.100-rc.2.23502.2
        [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
        DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

        | Method                                 | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |--------------------------------------- |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
        | Create_SystemExpression                | 1,039.5 ns | 20.75 ns | 45.98 ns |  8.29 |    0.50 | 0.2060 |    1304 B |        2.63 |
        | Create_LightExpression                 |   125.7 ns |  2.46 ns |  5.99 ns |  1.00 |    0.00 | 0.0789 |     496 B |        1.00 |
        | Create_LightExpression_with_intrinsics |   130.0 ns |  2.47 ns |  6.25 ns |  1.04 |    0.07 | 0.0777 |     488 B |        0.98 |


        ## v5.0.0 + net9.0

        BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
        Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        .NET SDK 9.0.100
        [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
        DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2

        | Method                                 | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
        |--------------------------------------- |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
        | Create_SystemExpression                | 1,110.9 ns | 22.19 ns | 62.23 ns | 1,086.1 ns |  7.25 |    0.56 | 0.2060 |    1304 B |        2.63 |
        | Create_LightExpression                 |   153.7 ns |  3.14 ns |  8.61 ns |   150.5 ns |  1.00 |    0.08 | 0.0789 |     496 B |        1.00 |
        | Create_LightExpression_with_intrinsics |   161.0 ns |  2.80 ns |  2.19 ns |   161.0 ns |  1.05 |    0.06 | 0.0777 |     488 B |        0.98 |

        */

        [Benchmark]
        public object Create_SystemExpression() =>
            LightExpressionTests.CreateComplexExpression();

        [Benchmark(Baseline = true)]
        public object Create_LightExpression() =>
            LightExpressionTests.CreateComplexLightExpression();

        [Benchmark]
        public object Create_LightExpression_with_intrinsics() =>
            LightExpressionTests.CreateComplexLightExpression_with_intrinsics();
    }
}
