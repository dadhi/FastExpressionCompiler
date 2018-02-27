using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using E = FastExpressionCompiler.ExpressionInfo;

namespace FastExpressionCompiler.Benchmarks
{
    public class ExprInfoVsExpr_TryCatchExpr
    {
        private static Expression<Func<string, int>> Expr = CreateExpr();

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

        //private static ExpressionInfo<Func<string, int>> CreateExprInfo()
        //{
        //    var aParamExpr = E.Parameter(typeof(string), "a");
        //    var exParamExpr = E.Parameter(typeof(Exception), "ex");

        //    return E.Lambda<Func<string, int>>(
        //        E.TryCatch(
        //            E.Call(typeof(int).GetTypeInfo()
        //                    .DeclaredMethods.First(m => m.Name == nameof(int.Parse)),
        //                aParamExpr),
        //            E.Catch(exParamExpr,
        //                E.Property(
        //                    E.Property(exParamExpr, typeof(Exception).GetTypeInfo()
        //                        .DeclaredProperties.First(p => p.Name == nameof(Exception.Message))),
        //                    typeof(string).GetTypeInfo()
        //                        .DeclaredProperties.First(p => p.Name == nameof(string.Length))
        //                )
        //            )
        //        ),
        //        aParamExpr
        //    );
        //}

        [MemoryDiagnoser]
        public class Compile
        {
            [Benchmark]
            public object Expr_Compile() => Expr.Compile();

            [Benchmark(Baseline = true)]
            public object Expr_CompileFast() => Expr.CompileFast();
        }

        [MemoryDiagnoser, DisassemblyDiagnoser(printIL: true)]
        public class Invoke
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
