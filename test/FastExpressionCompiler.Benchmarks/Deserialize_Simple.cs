using BenchmarkDotNet.Attributes;
using F = FastExpressionCompiler.IssueTests;
using FL = FastExpressionCompiler.LightExpression.IssueTests;
using static FastExpressionCompiler.IssueTests.Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown;
using static FastExpressionCompiler.LightExpression.IssueTests.Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown;

namespace FastExpressionCompiler.Benchmarks
{
    /*
    ## Initial results

    BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18363
    Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
    .NET Core SDK=3.1.302
      [Host]     : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT
      DefaultJob : .NET Core 3.1.6 (CoreCLR 4.700.20.26901, CoreFX 4.700.20.31603), X64 RyuJIT

    ### Expression Creation and Compilation

    |       Method |        Mean |     Error |     StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
    |------------- |------------:|----------:|-----------:|------:|--------:|-------:|-------:|-------:|----------:|
    | CompiledFast |    80.68 us |  1.609 us |   3.499 us |  1.00 |    0.00 | 3.9063 | 1.9531 | 0.1221 |  16.06 KB |
    |     Compiled | 4,442.98 us | 88.213 us | 191.768 us | 55.15 |    3.05 |      - |      - |      - |  27.55 KB |

    ### Invocation

    |              Method |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |-------------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|------:|------:|----------:|
    | Invoke_CompiledFast | 1.113 us | 0.0210 us | 0.0384 us | 1.132 us |  1.00 |    0.00 | 0.0954 |     - |     - |     400 B |
    |     Invoke_Compiled | 1.097 us | 0.0221 us | 0.0535 us | 1.090 us |  0.99 |    0.06 | 0.0992 |     - |     - |     424 B |

    ## V3 final score
    
    ### Expression Creation and Compilation

    |       Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
    |------------- |------------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
    | CompiledFast |    55.62 us |  0.758 us |  0.672 us |  1.00 |    0.00 | 3.9063 | 1.9531 | 0.1221 |  16.03 KB |
    |     Compiled | 3,143.27 us | 40.633 us | 36.020 us | 56.52 |    0.88 | 3.9063 |      - |      - |  27.75 KB |

    |       Method |        Mean |     Error |     StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
    |------------- |------------:|----------:|-----------:|------:|--------:|-------:|-------:|-------:|----------:|
    | CompiledFast |    77.90 us |  1.551 us |   4.401 us |  1.00 |    0.00 | 3.7842 | 1.8311 | 0.1221 |  15.61 KB |
    |     Compiled | 4,409.87 us | 87.777 us | 162.701 us | 56.96 |    3.73 |      - |      - |      - |  27.72 KB |

    ### Invocation

    |              Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
    |-------------------- |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
    | Invoke_CompiledFast | 825.9 ns | 15.47 ns | 15.19 ns |  1.00 |    0.00 | 0.0954 |     - |     - |     400 B |
    |     Invoke_Compiled | 825.8 ns | 16.29 ns | 18.11 ns |  1.00 |    0.03 | 0.1011 |     - |     - |     424 B |

    */
    [MemoryDiagnoser]
    public class Deserialize_Simple
    {
        [Benchmark(Baseline = true)]
        public object CompiledFast()
        {
            CreateLightExpression_and_CompileFast(out var a, out var b);
            return new { a, b };
        }

        [Benchmark]
        public object Compiled()
        {
            CreateExpression_and_CompileSys(out var a, out var b);
            return new { a, b };
        }

        static FL.DeserializerDlg<FL.Word> _desWord;
        static FL.DeserializerDlg<FL.Simple> _desSimple;
        static F.DeserializerDlg<F.Word> _desWordS;
        static F.DeserializerDlg<F.Simple> _desSimpleS;
        static Deserialize_Simple()
        {
            CreateLightExpression_and_CompileFast(out _desWord, out _desSimple);
            CreateExpression_and_CompileSys(out _desWordS, out _desSimpleS);
        }

        // [Benchmark(Baseline = true)]
        public bool Invoke_CompiledFast() => RunDeserializer(_desWord, _desSimple);

        // [Benchmark]
        public object Invoke_Compiled() => RunDeserializer(_desWordS, _desSimpleS);
    }
}
