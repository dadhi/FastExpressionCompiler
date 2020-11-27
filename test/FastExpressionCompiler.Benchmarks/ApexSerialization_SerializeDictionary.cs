using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class ApexSerialization_SerializeDictionary
    {
/*

## V3-preview-02

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT


|      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
|  CompileSys | 464.62 us | 4.434 us | 4.147 us | 13.14 |    0.16 | 7.3242 | 3.4180 |      - |   29.9 KB |
| CompileFast |  35.38 us | 0.531 us | 0.470 us |  1.00 |    0.00 | 1.8311 | 0.9155 | 0.0610 |   7.66 KB |


## V3-preview-05

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


|      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
|  CompileSys | 490.55 us | 4.818 us | 4.271 us | 13.93 |    0.41 | 6.8359 | 2.9297 |      - |  30.08 KB |
| CompileFast |  35.08 us | 0.675 us | 0.778 us |  1.00 |    0.00 | 1.8311 | 0.9155 | 0.0610 |   7.66 KB |

*/
        [MemoryDiagnoser]
        public class Compile
        {
            private static readonly FastExpressionCompiler.LightExpression.LambdaExpression _lightExpr = 
                FastExpressionCompiler.LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();
            private static readonly System.Linq.Expressions.LambdaExpression _sysExpr = _lightExpr.ToLambdaExpression();

            [Benchmark]
            public object CompileSys() => _sysExpr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => LightExpression.ExpressionCompiler.CompileFast(_lightExpr);
        }
    }
}
