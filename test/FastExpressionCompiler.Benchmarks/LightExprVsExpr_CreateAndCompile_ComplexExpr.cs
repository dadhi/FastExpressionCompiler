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

        ## v3.0

                                        Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        -------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                  CreateExpression_and_Compile | 262.44 us | 0.5543 us | 0.4914 us | 20.93 |    0.08 | 1.4648 | 0.4883 |      - |   7.16 KB |
              CreateExpression_and_CompileFast |  18.61 us | 0.2010 us | 0.1782 us |  1.48 |    0.02 | 1.5259 | 0.7629 | 0.0305 |   7.09 KB |
         CreateLightExpression_and_CompileFast |  12.54 us | 0.0459 us | 0.0430 us |  1.00 |    0.00 | 1.4038 | 0.7019 | 0.0458 |   6.48 KB |

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
         */
        [Benchmark]
        public object CreateExpression() =>
            LightExpressionTests.CreateComplexExpression();

        [Benchmark(Baseline = true)]
        public object CreateLightExpression() =>
            LightExpressionTests.CreateComplexLightExpression();
    }
}
