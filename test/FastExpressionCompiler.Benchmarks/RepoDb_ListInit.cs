using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class RepoDb_ListInit
    {
/*

## V3

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


|      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
| CompileFast |   9.619 us | 0.1892 us | 0.3734 us |  1.00 |    0.00 | 0.6866 | 0.3357 | 0.0305 |   2.83 KB |
|  CompileSys | 524.028 us | 7.4477 us | 6.6022 us | 53.65 |    2.13 | 0.9766 |      - |      - |   5.99 KB |

*/
        [MemoryDiagnoser]
        public class Compile
        {
            private static readonly FastExpressionCompiler.LightExpression.LambdaExpression _lightExpr = 
                FastExpressionCompiler.LightExpression.UnitTests.ListInitTests.Get_Simple_ListInit_Expression();

            private static readonly System.Linq.Expressions.LambdaExpression _sysExpr = 
                FastExpressionCompiler.UnitTests.ListInitTests.Get_Simple_ListInit_Expression();

            [Benchmark(Baseline = true)]
            public object CompileFast() => LightExpression.ExpressionCompiler.CompileFast(_lightExpr, true);

            [Benchmark]
            public object CompileSys() => _sysExpr.Compile();
        }
    }
}
