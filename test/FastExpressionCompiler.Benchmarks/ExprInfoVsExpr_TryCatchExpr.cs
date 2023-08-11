using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using LE = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    public class ExprInfoVsExpr_TryCatchExpr
    {
        private static Expression<Func<string, int>> Expr = CreateExpr();
        private static LightExpression.Expression<Func<string, int>> LightExpr = CreateLightExpr();

        // Test expression
        // (string a) => {
        //      try { return int.Parse(a); }
        //      catch (Exception ex) { return ex.Message.Length; }
        // }

        private static Expression<Func<string, int>> CreateExpr()
        {
            var aParamExpr = Parameter(typeof(string), "a");
            var exParamExpr = Parameter(typeof(Exception), "ex");

            return Lambda<Func<string, int>>(
                TryCatch(
                    Call(typeof(int).GetTypeInfo()
                            .DeclaredMethods.First(m => m.Name == nameof(int.Parse)),
                        aParamExpr
                    ),
                    Catch(exParamExpr,
                        Property(
                            Property(exParamExpr, typeof(Exception).GetTypeInfo()
                                .DeclaredProperties.First(p => p.Name == nameof(Exception.Message))),
                            typeof(string).GetTypeInfo()
                                .DeclaredProperties.First(p => p.Name == nameof(string.Length))
                        )
                    )
                ),
                aParamExpr
            );
        }

        private static LightExpression.Expression<Func<string, int>> CreateLightExpr()
        {
            var aParamExpr = LE.Parameter(typeof(string), "a");
            var exParamExpr = LE.Parameter(typeof(Exception), "ex");

            return LE.Lambda<Func<string, int>>(
                LE.TryCatch(
                    LE.Call(typeof(int).GetTypeInfo()
                            .DeclaredMethods.First(m => m.Name == nameof(int.Parse)),
                        aParamExpr
                    ),
                    LE.Catch(exParamExpr,
                        LE.Property(
                            LE.Property(exParamExpr, typeof(Exception).GetTypeInfo()
                                .DeclaredProperties.First(p => p.Name == nameof(Exception.Message))),
                            typeof(string).GetTypeInfo()
                                .DeclaredProperties.First(p => p.Name == nameof(string.Length))
                        )
                    )
                ),
                aParamExpr
            );
        }
/*
## v3.4.0 wut?

BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.306
  [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2


|                Method |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|---------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|          Expr_Compile | 241.008 us | 3.5712 us | 2.7881 us | 59.23 |    1.06 | 0.9766 | 0.4883 |   6.74 KB |        3.37 |
|      Expr_CompileFast |   4.274 us | 0.0847 us | 0.1187 us |  1.06 |    0.04 | 0.3204 | 0.3128 |      2 KB |        1.00 |
| LightExpr_CompileFast |   4.069 us | 0.0665 us | 0.0683 us |  1.00 |    0.00 | 0.3204 | 0.3128 |      2 KB |        1.00 |

## TBD measure Create+Compile
*/
        [MemoryDiagnoser]
        public class Compilation
        {
            [Benchmark]
            public object Expr_Compile() => Expr.Compile();

            // [Benchmark(Baseline = true)]
            [Benchmark]
            public object Expr_CompileFast() => Expr.CompileFast();

            [Benchmark(Baseline = true)]
            public object LightExpr_CompileFast() => LightExpression.ExpressionCompiler.CompileFast(LightExpr);
        }

        [MemoryDiagnoser, DisassemblyDiagnoser()]
        public class Invocation
        {
            private static Func<string, int> _compiled = Expr.Compile();
            private static Func<string, int> _compiledFast = Expr.CompileFast();

            [Benchmark]
            public object Invoke_Compiled() => _compiled.Invoke("123");

            [Benchmark(Baseline = true)]
            public object Invoke_CompiledFast() => _compiledFast.Invoke("123");
        }
    }
}
