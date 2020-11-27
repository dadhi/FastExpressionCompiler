using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

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
        public class Create_and_Compile
        {
/*
## v3-preview-03

|                 Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     SystemExpr_Compile | 171.098 us | 1.9611 us | 1.8344 us | 30.22 |    0.85 | 1.2207 | 0.4883 |      - |   5.11 KB |
| SystemExpr_CompileFast |   7.314 us | 0.1273 us | 0.1191 us |  1.29 |    0.05 | 0.4883 | 0.2441 | 0.0305 |   2.03 KB |
|  LightExpr_CompileFast |   5.647 us | 0.0813 us | 0.1217 us |  1.00 |    0.00 | 0.3815 | 0.1907 | 0.0305 |   1.59 KB |

## v3-preview-05

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


|                 Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     SystemExpr_Compile | 165.654 us | 1.7359 us | 1.4496 us | 26.72 |    0.80 | 1.2207 | 0.4883 |      - |   5.31 KB |
| SystemExpr_CompileFast |   6.680 us | 0.1275 us | 0.1192 us |  1.07 |    0.04 | 0.4959 | 0.2441 | 0.0305 |   2.04 KB |
|  LightExpr_CompileFast |   6.160 us | 0.1209 us | 0.1773 us |  1.00 |    0.00 | 0.3815 | 0.1907 | 0.0305 |   1.59 KB |
*/

            [Benchmark]
            public Func<B, X> SystemExpr_Compile() => 
                ComposeManualExprWithParams(Expression.Constant(_a, typeof(A))).Compile();

            [Benchmark]
            public Func<B, X> SystemExpr_CompileFast() => 
                ComposeManualExprWithParams(Expression.Constant(_a, typeof(A))).CompileFast(true);

            [Benchmark(Baseline = true)]
            public Func<B, X> LightExpr_CompileFast() =>
                LightExpression.ExpressionCompiler.CompileFast<Func<B, X>>(ComposeManualExprWithParams(LightExpression.Expression.Constant(_a, typeof(A))), true);
        }

        [MemoryDiagnoser]
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                                                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                                                     Compile | 176.107 us | 1.3451 us | 1.2582 us | 36.05 |    0.75 |      0.9766 |      0.4883 |           - |              4.7 KB |
                                                 CompileFast |   7.257 us | 0.0648 us | 0.0606 us |  1.49 |    0.03 |      0.4349 |      0.2136 |      0.0305 |             1.99 KB |
                            CompileFastWithPreCreatedClosure |   5.186 us | 0.0896 us | 0.0795 us |  1.06 |    0.02 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |
             CompileFastWithPreCreatedClosureLightExpression |   4.892 us | 0.0965 us | 0.0948 us |  1.00 |    0.00 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |

            ## v3-preview-02

            |                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |---------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |                     Compile | 153.405 us | 3.0500 us | 5.8762 us | 32.77 |    2.25 | 0.9766 | 0.4883 |      - |   4.59 KB |
            |                 CompileFast |   4.716 us | 0.0925 us | 0.0820 us |  1.02 |    0.03 | 0.3510 | 0.1755 | 0.0305 |   1.46 KB |
            | CompileFast_LightExpression |   4.611 us | 0.0898 us | 0.0840 us |  1.00 |    0.00 | 0.3433 | 0.1678 | 0.0305 |   1.42 KB |

            ## v3-preview-03

            |                      Method |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |---------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |                     Compile | 154.112 us | 1.7660 us | 1.4747 us | 154.045 us | 32.73 |    1.11 | 0.9766 | 0.4883 |      - |   4.59 KB |
            |                 CompileFast |   4.828 us | 0.0950 us | 0.1984 us |   4.750 us |  1.04 |    0.05 | 0.3510 | 0.1755 | 0.0305 |   1.46 KB |
            | CompileFast_LightExpression |   4.704 us | 0.0940 us | 0.1188 us |   4.708 us |  1.00 |    0.00 | 0.3433 | 0.1678 | 0.0305 |   1.42 KB |

            ## v3-preview-05

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


            |                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |---------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |                     Compile | 148.633 us | 1.4863 us | 1.2411 us | 33.20 |    1.05 | 0.9766 | 0.4883 |      - |   4.78 KB |
            |                 CompileFast |   4.498 us | 0.0887 us | 0.1022 us |  1.02 |    0.04 | 0.3510 | 0.1755 | 0.0305 |   1.46 KB |
            | CompileFast_LightExpression |   4.365 us | 0.0860 us | 0.1364 us |  1.00 |    0.00 | 0.3433 | 0.1678 | 0.0305 |   1.42 KB |
*/

            [Benchmark]
            public Func<B, X> Compile() => 
                _expr.Compile();

            [Benchmark]
            public Func<B, X> CompileFast() => 
                _expr.CompileFast(true);

            [Benchmark(Baseline = true)]
            public Func<B, X> CompileFast_LightExpression() =>
                LightExpression.ExpressionCompiler.CompileFast<Func<B, X>>(_leExpr, true);

            // [Benchmark]
            public Func<B, X> CompileFastWithPreCreatedClosure() => 
                _expr.TryCompileWithPreCreatedClosure<Func<B, X>>(_aConstExpr)
                ?? _expr.Compile();

            // [Benchmark]
            public Func<B, X> CompileFastWithPreCreatedClosureLightExpression() =>
                LightExpression.ExpressionCompiler.TryCompileWithPreCreatedClosure<Func<B, X>>(
                    _leExpr, _aConstLEExpr)
                ?? LightExpression.ExpressionCompiler.CompileSys(_leExpr);
        }

        [MemoryDiagnoser]
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

            ## V3 baseline
                                                Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            ------------------------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
                                      DirectLambdaCall | 11.35 ns | 0.0491 ns | 0.0460 ns |  1.01 | 0.0068 |     - |     - |      32 B |
                                        CompiledLambda | 11.68 ns | 0.0409 ns | 0.0342 ns |  1.04 | 0.0068 |     - |     - |      32 B |
                                    FastCompiledLambda | 11.73 ns | 0.0905 ns | 0.0802 ns |  1.04 | 0.0068 |     - |     - |      32 B |
               FastCompiledLambdaWithPreCreatedClosure | 11.26 ns | 0.0414 ns | 0.0387 ns |  1.00 | 0.0068 |     - |     - |      32 B |
             FastCompiledLambdaWithPreCreatedClosureLE | 11.27 ns | 0.0594 ns | 0.0556 ns |  1.00 | 0.0068 |     - |     - |      32 B |

            ## V3-preview-02

            |                             Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |----------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
            |                   DirectLambdaCall | 11.07 ns | 0.183 ns | 0.171 ns |  1.02 |    0.02 | 0.0076 |     - |     - |      32 B |
            |                     CompiledLambda | 12.31 ns | 0.101 ns | 0.090 ns |  1.13 |    0.01 | 0.0076 |     - |     - |      32 B |
            |                 FastCompiledLambda | 10.80 ns | 0.146 ns | 0.137 ns |  1.00 |    0.01 | 0.0076 |     - |     - |      32 B |
            | FastCompiledLambda_LightExpression | 10.86 ns | 0.109 ns | 0.096 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      32 B |

            ## V3-preview-05

            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
            Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
            .NET Core SDK=5.0.100
            [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
            DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


            |                             Method |     Mean |    Error |   StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |----------------------------------- |---------:|---------:|---------:|------:|-------:|------:|------:|----------:|
            |                   DirectLambdaCall | 11.86 ns | 0.140 ns | 0.131 ns |  1.00 | 0.0076 |     - |     - |      32 B |
            |                     CompiledLambda | 13.44 ns | 0.115 ns | 0.096 ns |  1.13 | 0.0076 |     - |     - |      32 B |
            |                 FastCompiledLambda | 12.43 ns | 0.173 ns | 0.154 ns |  1.05 | 0.0076 |     - |     - |      32 B |
            | FastCompiledLambda_LightExpression | 11.87 ns | 0.121 ns | 0.101 ns |  1.00 | 0.0076 |     - |     - |      32 B |

            */
            private static readonly Func<B, X> _lambdaCompiled = _expr.Compile();
            private static readonly Func<B, X> _lambdaCompiledFast = _expr.CompileFast(true);
            private static readonly Func<B, X> _lambdaCompiledFast_LightExpession = _expr.CompileFast<Func<B, X>>(true);

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

            [Benchmark(Baseline = true)]
            public X FastCompiledLambda_LightExpression() => _lambdaCompiledFast_LightExpession(_bb);

            // [Benchmark]
            public X FastCompiledLambdaWithPreCreatedClosure() => _lambdaCompiledFastWithClosure(_bb);

            // [Benchmark]
            public X FastCompiledLambdaWithPreCreatedClosureLE() => _lambdaCompiledFastWithClosureLE(_bb);
        }
    }
}
