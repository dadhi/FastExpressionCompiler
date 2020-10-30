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

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

## V3-preview-02


|      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
|  CompileSys | 464.62 us | 4.434 us | 4.147 us | 13.14 |    0.16 | 7.3242 | 3.4180 |      - |   29.9 KB |
| CompileFast |  35.38 us | 0.531 us | 0.470 us |  1.00 |    0.00 | 1.8311 | 0.9155 | 0.0610 |   7.66 KB |

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
