using BenchmarkDotNet.Attributes;
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


|       Method |        Mean |     Error |     StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------- |------------:|----------:|-----------:|------:|--------:|-------:|-------:|-------:|----------:|
| CompiledFast |    80.68 us |  1.609 us |   3.499 us |  1.00 |    0.00 | 3.9063 | 1.9531 | 0.1221 |  16.06 KB |
|     Compiled | 4,442.98 us | 88.213 us | 191.768 us | 55.15 |    3.05 |      - |      - |      - |  27.55 KB |

*/
    [MemoryDiagnoser]
    public class Deserialize_Simple
    {
        [Benchmark(Baseline = true)]
        public object CompiledFast() {
            CreateLightExpression_and_CompileFast(out var a, out var b);
            return new { a, b };
        }

        [Benchmark]
        public object Compiled() {
            CreateExpression_and_CompileSys(out var a, out var b);
            return new { a, b };
        }
    }
}
