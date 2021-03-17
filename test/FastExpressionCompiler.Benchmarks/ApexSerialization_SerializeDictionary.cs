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

*/
        [MemoryDiagnoser]
        public class Compile
        {
            private static readonly FastExpressionCompiler.LightExpression.LambdaExpression _lightExpr = 
                FastExpressionCompiler.LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();
            private static readonly System.Linq.Expressions.LambdaExpression _sysExpr = 
                FastExpressionCompiler.IssueTests.Issue261_Loop_wih_conditions_fails.CreateSerializeDictionaryExpression();

            [Benchmark(Baseline = true)]
            public object CompileFast() => LightExpression.ExpressionCompiler.CompileFast(_lightExpr);

            [Benchmark]
            public object CompileSys() => _sysExpr.Compile();
        }
    }
}
