using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace FastExpressionCompiler.Benchmarks
{
    public class ManuallyComposedLambdaBenchmark
    {
        private static Expression<Func<B, X>> ComposeManualExprWithParams(Expression aConstExpr)
        {
            var bParamExpr = Expression.Parameter(typeof(B), "b");
            return Expression.Lambda<Func<B, X>>(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(), aConstExpr, bParamExpr),
                bParamExpr);
        }

        private static LightExpression.Expression<Func<B, X>> ComposeManualExprWithParams(LightExpression.Expression aConstExpr)
        {
            var bParamExpr = LightExpression.Expression.Parameter(typeof(B), "b");
            return LightExpression.Expression.Lambda<Func<B, X>>(
                LightExpression.Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(), aConstExpr, bParamExpr),
                bParamExpr);
        }

        public class A { }
        public class B { }

        public class X
        {
            public A A { get; }
            public B B { get; }

            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }

        private static readonly A _a = new A();

        private static readonly ConstantExpression _aConstExpr = Expression.Constant(_a, typeof(A));
        private static readonly Expression<Func<B, X>> _expr = ComposeManualExprWithParams(_aConstExpr);

        private static readonly LightExpression.ConstantExpression _aConstLEExpr = LightExpression.Expression.Constant(_a, typeof(A));
        private static readonly FastExpressionCompiler.LightExpression.Expression<Func<B, X>> _leExpr = ComposeManualExprWithParams(_aConstLEExpr);

        [MemoryDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                                                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             CompileFastWithPreCreatedClosureLightExpression |   4.892 us | 0.0965 us | 0.0948 us |  1.00 |    0.00 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |
                            CompileFastWithPreCreatedClosure |   5.186 us | 0.0896 us | 0.0795 us |  1.06 |    0.02 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |
                                                 CompileFast |   7.257 us | 0.0648 us | 0.0606 us |  1.49 |    0.03 |      0.4349 |      0.2136 |      0.0305 |             1.99 KB |
                                                     Compile | 176.107 us | 1.3451 us | 1.2582 us | 36.05 |    0.75 |      0.9766 |      0.4883 |           - |              4.7 KB |

            ## v2.1

                                                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             CompileFastWithPreCreatedClosureLightExpression |   4.500 us | 0.0166 us | 0.0155 us |  1.00 |    0.00 |      0.3204 |      0.1602 |      0.0305 |             1.47 KB |
                            CompileFastWithPreCreatedClosure |   5.103 us | 0.0402 us | 0.0356 us |  1.13 |    0.01 |      0.3204 |      0.1602 |      0.0305 |             1.47 KB |
                                                 CompileFast |   7.122 us | 0.0401 us | 0.0375 us |  1.58 |    0.01 |      0.4349 |      0.2136 |      0.0305 |             1.99 KB |
                                                     Compile | 152.286 us | 0.6110 us | 0.5715 us | 33.84 |    0.20 |      0.9766 |      0.4883 |           - |              4.7 KB |
             */
            [Benchmark]
            public Func<B, X> Compile() => 
                _expr.Compile();

            [Benchmark]
            public Func<B, X> CompileFast() => 
                _expr.CompileFast();

            [Benchmark]
            public Func<B, X> CompileFastWithPreCreatedClosure() => 
                _expr.TryCompileWithPreCreatedClosure<Func<B, X>>(_aConstExpr)
                ?? _expr.Compile();

            [Benchmark(Baseline = true)]
            public Func<B, X> CompileFastWithPreCreatedClosureLightExpression() =>
                LightExpression.ExpressionCompiler.TryCompileWithPreCreatedClosure<Func<B, X>>(
                    _leExpr, _aConstLEExpr)
                ?? LightExpression.ExpressionCompiler.CompileSys(_leExpr);
        }

        [MemoryDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Invocation
        {
            /*
            ## 26.01.2019: V2

                                                Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------------------------------ |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
             FastCompiledLambdaWithPreCreatedClosureLE | 10.64 ns | 0.0404 ns | 0.0358 ns |  1.00 |      0.0068 |           - |           - |                32 B |
                                      DirectLambdaCall | 10.65 ns | 0.0601 ns | 0.0533 ns |  1.00 |      0.0068 |           - |           - |                32 B |
                                    FastCompiledLambda | 10.98 ns | 0.0434 ns | 0.0406 ns |  1.03 |      0.0068 |           - |           - |                32 B |
               FastCompiledLambdaWithPreCreatedClosure | 11.10 ns | 0.0369 ns | 0.0345 ns |  1.04 |      0.0068 |           - |           - |                32 B |
                                        CompiledLambda | 11.13 ns | 0.0620 ns | 0.0518 ns |  1.05 |      0.0068 |           - |           - |                32 B |

             */
            private static readonly Func<B, X> _lambdaCompiled = _expr.Compile();
            private static readonly Func<B, X> _lambdaCompiledFast = _expr.CompileFast();

            private static readonly Func<B, X> _lambdaCompiledFastWithClosure =
                _expr.TryCompileWithPreCreatedClosure<Func<B, X>>(_aConstExpr);

            private static readonly Func<B, X> _lambdaCompiledFastWithClosureLE =
                LightExpression.ExpressionCompiler.TryCompileWithPreCreatedClosure<Func<B, X>>(
                    _leExpr, _aConstLEExpr);

            private static readonly A _aa = new A();
            private static readonly B _bb = new B();
            private static readonly Func<B, X> _lambda = b => new X(_aa, b);

            [Benchmark]
            public X DirectLambdaCall() => _lambda(_bb);

            [Benchmark]
            public X CompiledLambda() => _lambdaCompiled(_bb);

            [Benchmark]
            public X FastCompiledLambda() => _lambdaCompiledFast(_bb);

            [Benchmark]
            public X FastCompiledLambdaWithPreCreatedClosure() => _lambdaCompiledFastWithClosure(_bb);

            [Benchmark(Baseline = true)]
            public X FastCompiledLambdaWithPreCreatedClosureLE() => _lambdaCompiledFastWithClosureLE(_bb);
        }
    }
}
