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

        ## v3.0 RTM

        |                                Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |-------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
        |          CreateExpression_and_Compile | 241.97 us | 2.007 us | 1.877 us | 17.77 |    0.20 | 1.7090 | 0.7324 |      - |   7.01 KB |
        |      CreateExpression_and_CompileFast |  17.30 us | 0.207 us | 0.173 us |  1.27 |    0.02 | 1.7395 | 0.8545 | 0.0305 |   7.19 KB |
        | CreateLightExpression_and_CompileFast |  13.61 us | 0.158 us | 0.140 us |  1.00 |    0.00 | 1.6174 | 0.7935 | 0.0305 |   6.64 KB |

         */

        [Benchmark]
        public object CreateExpression_and_Compile() =>
            LightExpressionTests.CreateComplexExpression().Compile();

        [Benchmark]
        public object CreateExpression_and_CompileFast() =>
            LightExpressionTests.CreateComplexExpression().CompileFast();

        [Benchmark(Baseline = true)]
        public object CreateLightExpression_and_CompileFast() =>
            LE.CompileFast(LightExpressionTests.CreateComplexLightExpression());
    }

    [MemoryDiagnoser]
    public class LightExprVsExpr_Create_ComplexExpr
    {
        /*
        ## 25.01.2019

                       Method  |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ---------------------- |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
         CreateLightExpression |   389.5 ns | 0.9547 ns | 0.7972 ns |  1.00 |    0.00 |      0.1693 |           - |           - |               800 B |
             CreateExpression  | 3,574.7 ns | 8.0032 ns | 7.4862 ns |  9.18 |    0.02 |      0.2823 |           - |           - |              1344 B |

        ## V3-preview-01

        |                Method |       Mean |    Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
        |      CreateExpression | 2,805.2 ns | 55.57 ns | 107.06 ns |  4.76 |    0.32 | 0.3090 |     - |     - |    1304 B |
        | CreateLightExpression |   578.5 ns |  6.39 ns |   5.98 ns |  1.00 |    0.00 | 0.1678 |     - |     - |     704 B |

        ## V3

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        |      CreateExpression | 2,857.4 ns | 28.03 ns | 34.43 ns |  4.89 |    0.04 | 0.3090 |     - |     - |    1304 B |
        | CreateLightExpression |   583.3 ns |  7.29 ns |  6.46 ns |  1.00 |    0.00 | 0.1659 |     - |     - |     696 B |

        # V3 - without TypeCode for Convert

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        |      CreateExpression | 2,854.9 ns | 54.10 ns | 50.60 ns |  5.10 |    0.10 | 0.3090 |     - |     - |    1304 B |
        | CreateLightExpression |   560.1 ns |  7.83 ns |  6.94 ns |  1.00 |    0.00 | 0.1678 |     - |     - |     704 B |


        # V3 - without TypeCode for Parameter

        |                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        |---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
        |      CreateExpression | 3,126.9 ns | 62.14 ns | 80.80 ns |  5.46 |    0.13 | 0.3052 |     - |     - |    1304 B |
        | CreateLightExpression |   578.4 ns |  8.07 ns |  7.54 ns |  1.00 |    0.00 | 0.1640 |     - |     - |     688 B |

        */
        [Benchmark]
        public object CreateExpression() =>
            LightExpressionTests.CreateComplexExpression();

        [Benchmark(Baseline = true)]
        public object CreateLightExpression() =>
            LightExpressionTests.CreateComplexLightExpression();
    }
}
