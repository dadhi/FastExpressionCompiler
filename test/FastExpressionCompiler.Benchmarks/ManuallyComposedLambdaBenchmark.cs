using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class ManuallyComposedLambdaBenchmark
    {
        private static readonly ConstructorInfo _ctorX = typeof(X).GetTypeInfo().DeclaredConstructors.First();

        private static Expression<Func<B, X>> CreateManualExprWithParams()
        {
            var bParamExpr = Expression.Parameter(typeof(B), "b");
            return Expression.Lambda<Func<B, X>>(
                Expression.New(_ctorX, Expression.Constant(_a, typeof(A)), bParamExpr),
                bParamExpr);
        }

        private static LightExpression.Expression<Func<B, X>> CreateManualLightExprWithParams()
        {
            var bParamExpr = LightExpression.Expression.ParameterOf<B>("b");
            return LightExpression.Expression.Lambda<Func<B, X>>(
                LightExpression.Expression.New(_ctorX, LightExpression.Expression.ConstantOf(_a), bParamExpr),
                bParamExpr,
                typeof(X));
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

        private static readonly Expression<Func<B, X>> _expr = CreateManualExprWithParams();
        private static readonly LightExpression.Expression<Func<B, X>> _leExpr = CreateManualLightExprWithParams();

        [MemoryDiagnoser]
        public class Create
        {
            /*
            ## v4.0.0

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.100-rc.2.23502.2
            [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

            | Method     | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |----------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
            | SystemExpr | 314.19 ns | 6.975 ns | 19.094 ns |  6.42 |    0.87 | 0.0782 |     496 B |        3.88 |
            | LightExpr  |  48.67 ns | 2.300 ns |  6.745 ns |  1.00 |    0.00 | 0.0204 |     128 B |        1.00 |

            # v4.0.0 - with providing return type when constructing the lambda

            | Method             | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |------------------- |----------:|---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
            | SystemExpression   | 328.47 ns | 9.000 ns | 25.677 ns | 317.90 ns | 14.16 |    1.45 | 0.0782 |     496 B |        3.88 |
            | FECLightExpression |  23.37 ns | 0.695 ns |  1.948 ns |  22.86 ns |  1.00 |    0.00 | 0.0204 |     128 B |        1.00 |

            # v4.0.0 - with ParameterOf

            | Method             | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |------------------- |----------:|---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
            | SystemExpression   | 322.29 ns | 8.488 ns | 24.078 ns | 315.39 ns | 17.51 |    3.46 | 0.0782 |     496 B |        4.13 |
            | FECLightExpression |  19.09 ns | 1.064 ns |  3.139 ns |  19.07 ns |  1.00 |    0.00 | 0.0191 |     120 B |        1.00 |
            */

            [Benchmark]
            public LambdaExpression Create_SystemExpression() =>
                CreateManualExprWithParams();

            [Benchmark(Baseline = true)]
            public LightExpression.LambdaExpression Create_LightExpression() =>
                CreateManualLightExprWithParams();
        }

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

            ## v4.0.0

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.100-rc.2.23502.2
            [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2


            | Method                 | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | SystemExpr_Compile     | 89.873 us | 1.5941 us | 2.1821 us | 24.68 |    1.50 | 0.7324 | 0.4883 |   5.25 KB |        3.40 |
            | SystemExpr_CompileFast |  3.814 us | 0.0694 us | 0.0852 us |  1.04 |    0.07 | 0.3052 | 0.2899 |   1.96 KB |        1.27 |
            | LightExpr_CompileFast  |  3.682 us | 0.0872 us | 0.2401 us |  1.00 |    0.00 | 0.2518 | 0.2365 |   1.55 KB |        1.00 |

            ## v4.0.0 - after bm cleanup and consistent creation for all bms

            | Method                                   | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Create_SystemExpression_Then_Compile     | 84.437 us | 1.6599 us | 1.9760 us | 20.04 |    1.42 | 0.7324 | 0.6104 |   5.22 KB |        3.46 |
            | Create_SystemExpression_Then_CompileFast |  3.720 us | 0.0715 us | 0.0734 us |  0.89 |    0.06 | 0.3052 | 0.2899 |   1.93 KB |        1.28 |
            | Create_LightExpression_Then_CompileFast  |  3.917 us | 0.2579 us | 0.7440 us |  1.00 |    0.00 | 0.2441 | 0.2365 |   1.51 KB |        1.00 |

            */

            [Benchmark]
            public Func<B, X> Create_SystemExpression_Then_Compile() =>
                CreateManualExprWithParams().Compile();

            [Benchmark]
            public Func<B, X> Create_SystemExpression_Then_CompileFast() =>
                CreateManualExprWithParams().CompileFast(true);

            [Benchmark(Baseline = true)]
            public Func<B, X> Create_LightExpression_Then_CompileFast() =>
                LightExpression.ExpressionCompiler.CompileFast(CreateManualLightExprWithParams(), true);
        }

        [MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
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

            ## v3.3.1

            |                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
            |---------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
            |                     Compile | 179.266 us | 3.5687 us | 7.2089 us | 39.11 |    2.15 | 1.4648 | 0.7324 |      - |   4.74 KB |
            |                 CompileFast |   4.791 us | 0.0955 us | 0.2307 us |  1.04 |    0.06 | 0.4578 | 0.2289 | 0.0305 |   1.41 KB |
            | CompileFast_LightExpression |   4.636 us | 0.0916 us | 0.1531 us |  1.00 |    0.00 | 0.4425 | 0.2213 | 0.0305 |   1.38 KB |

            ## v4.0.0

            BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
            11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
            .NET SDK 8.0.100-rc.2.23502.2
            [Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
            DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2

            | Method                      | Mean       | Error     | StdDev     | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |---------------------------- |-----------:|----------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile                     | 109.937 us | 4.5259 us | 12.9855 us | 108.150 us | 30.88 |    4.71 | 0.7324 | 0.4883 |   4.74 KB |        3.41 |
            | CompileFast                 |   3.902 us | 0.2889 us |  0.8244 us |   3.470 us |  1.09 |    0.24 | 0.2136 | 0.1984 |   1.39 KB |        1.00 |
            | CompileFast_LightExpression |   3.591 us | 0.1249 us |  0.3642 us |   3.407 us |  1.00 |    0.00 | 0.2136 | 0.1984 |   1.39 KB |        1.00 |

            ## v4.0.0 - after bm cleanup and consistent creation for all bms

            | Method                       | Mean      | Error     | StdDev     | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------------- |----------:|----------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
            | Compile_SystemExpression     | 94.596 us | 4.4636 us | 13.0205 us | 89.418 us | 29.16 |    3.55 | 0.4883 |      - |   4.73 KB |        3.41 |
            | CompileFast_SystemExpression |  3.047 us | 0.0607 us |  0.1183 us |  3.010 us |  0.97 |    0.06 | 0.2213 | 0.2136 |   1.39 KB |        1.00 |
            | CompileFast_LightExpression  |  3.151 us | 0.0628 us |  0.1117 us |  3.130 us |  1.00 |    0.00 | 0.2213 | 0.2136 |   1.39 KB |        1.00 |

            # v5.3.0 - Pooling the ILGenerator

            BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4061)
            Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            .NET SDK 9.0.203
            [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
            DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

            | Method                       | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
            | CompileFast_SystemExpression |   3.219 us | 0.0380 us | 0.0337 us |  0.98 |    0.01 |    1 | 0.1793 | 0.1755 |   1.12 KB |        1.00 |
            | CompileFast_LightExpression  |   3.292 us | 0.0407 us | 0.0381 us |  1.00 |    0.02 |    1 | 0.1793 | 0.1755 |   1.12 KB |        1.00 |
            | Compile_SystemExpression     | 102.515 us | 1.4959 us | 2.2390 us | 31.14 |    0.75 |    2 | 0.7324 | 0.4883 |   4.74 KB |        4.24 |

            # v5.3.0 - Pooling the ILGenerator+SignatureHelper

            | Method                       | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
            |----------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
            | CompileFast_SystemExpression |   3.190 us | 0.0519 us | 0.0485 us |  0.97 |    0.02 |    1 | 0.1755 | 0.1678 |   1.08 KB |        1.00 |
            | CompileFast_LightExpression  |   3.300 us | 0.0633 us | 0.0650 us |  1.00 |    0.03 |    1 | 0.1755 | 0.1678 |   1.08 KB |        1.00 |
            | Compile_SystemExpression     | 106.339 us | 2.1083 us | 4.1120 us | 32.24 |    1.38 |    2 | 0.7324 | 0.6104 |   4.74 KB |        4.40 |

            */

            [Benchmark]
            public Func<B, X> Compile_SystemExpression() =>
                _expr.Compile();

            [Benchmark(Baseline = true)]
            public Func<B, X> CompileFast_SystemExpression() =>
                _expr.CompileFast(true);

            [Benchmark]
            public Func<B, X> CompileFast_LightExpression() =>
                LightExpression.ExpressionCompiler.CompileFast(_leExpr, true);
        }

        [MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
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

            ## V3.3.1

            |                             Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |----------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
            |                   DirectLambdaCall | 13.72 ns | 0.274 ns | 0.500 ns | 13.62 ns |  1.05 |    0.06 | 0.0102 |     - |     - |      32 B |
            |                     CompiledLambda | 17.12 ns | 1.006 ns | 2.950 ns | 15.78 ns |  1.24 |    0.15 | 0.0102 |     - |     - |      32 B |
            |                 FastCompiledLambda | 12.87 ns | 0.164 ns | 0.128 ns | 12.88 ns |  0.97 |    0.03 | 0.0102 |     - |     - |      32 B |
            | FastCompiledLambda_LightExpression | 13.11 ns | 0.258 ns | 0.471 ns | 13.01 ns |  1.00 |    0.00 | 0.0102 |     - |     - |      32 B |

            ## v4.0.0

            | Method                        | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
            |------------------------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
            | DirectCall                    | 8.388 ns | 0.2655 ns | 0.7575 ns | 8.092 ns |  1.00 |    0.07 | 0.0051 |      32 B |        1.00 |
            | Compiled_SystemExpression     | 9.474 ns | 0.1870 ns | 0.4105 ns | 9.381 ns |  1.10 |    0.05 | 0.0051 |      32 B |        1.00 |
            | CompiledFast_SystemExpression | 8.575 ns | 0.1624 ns | 0.1440 ns | 8.517 ns |  1.00 |    0.02 | 0.0051 |      32 B |        1.00 |
            | CompiledFast_LightExpression  | 8.584 ns | 0.0776 ns | 0.0862 ns | 8.594 ns |  1.00 |    0.00 | 0.0051 |      32 B |        1.00 |

            */
            private static readonly Func<B, X> _lambdaCompiled = _expr.Compile();
            private static readonly Func<B, X> _lambdaCompiledFast = _expr.CompileFast(true);
            private static readonly Func<B, X> _lambdaCompiledFast_LightExpession = LightExpression.ExpressionCompiler.CompileFast(_leExpr, true);

            private static readonly A _aa = new A();
            private static readonly B _bb = new B();
            private static readonly Func<B, X> _lambda = b => new X(_aa, b);

            [Benchmark(Baseline = true)]
            public X DirectCall() => _lambda(_bb);

            [Benchmark]
            public X Compiled_SystemExpression() => _lambdaCompiled(_bb);

            [Benchmark]
            public X CompiledFast_SystemExpression() => _lambdaCompiledFast(_bb);

            [Benchmark]
            public X CompiledFast_LightExpression() => _lambdaCompiledFast_LightExpession(_bb);
        }
    }
}
