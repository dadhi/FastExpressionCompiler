using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class ApexSerialization_SerializeDictionary
    {
/*

## V3

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


|      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
| CompileFast |  36.76 us | 0.691 us | 1.363 us |  1.00 |    0.00 | 1.8311 | 0.9155 | 0.0610 |   7.66 KB |
|  CompileSys | 508.56 us | 6.580 us | 5.833 us | 13.96 |    0.59 | 6.8359 | 2.9297 |      - |  29.76 KB |

## V3.3.1

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=6.0.201
  [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
  DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT

|      Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
| CompileFast |  61.89 us |  1.232 us |  2.543 us |  1.00 |    0.00 | 4.8828 | 2.4414 | 0.1221 |  14.95 KB |
|  CompileSys | 572.79 us | 11.444 us | 21.212 us |  9.23 |    0.52 | 9.7656 | 4.8828 |      - |  30.28 KB |

## v3.4.0

BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.306
  [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2

|      Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| CompileFast |  22.15 us |  0.194 us |  0.181 us |  22.16 us |  1.00 |    0.00 | 1.3428 | 1.3123 |   8.32 KB |        1.00 |
|  CompileSys | 512.77 us | 10.250 us | 28.402 us | 499.13 us | 23.17 |    0.92 | 3.9063 | 2.9297 |  27.41 KB |        3.29 |

*/
        [MemoryDiagnoser]
        public class Compile
        {
            private static readonly LightExpression.LambdaExpression _lightExpr =
                LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();
            private static readonly LambdaExpression _sysExpr =
                IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();

            [Benchmark(Baseline = true)]
            public object CompileFast() => LightExpression.ExpressionCompiler.CompileFast(_lightExpr);

            [Benchmark]
            public object CompileSys() => _sysExpr.Compile();
        }
    }
}
