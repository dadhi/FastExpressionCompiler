using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
// # Benchmark
// 
// BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
// Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
// .NET Core SDK=6.0.302
//   [Host]     : .NET Core 6.0.7 (CoreCLR 6.0.722.32202, CoreFX 6.0.722.32202), X64 RyuJIT
//   DefaultJob : .NET Core 6.0.7 (CoreCLR 6.0.722.32202, CoreFX 6.0.722.32202), X64 RyuJIT
//
// ## Compile vs CompileFast
//
// |        Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
// |-------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
// | CompileSystem | 203.18 us | 4.013 us | 6.594 us | 15.26 |    0.60 | 3.1738 | 1.4648 |      - |  10.05 KB |
// |   CompileFast |  13.32 us | 0.266 us | 0.382 us |  1.00 |    0.00 | 1.2512 | 0.6256 | 0.0763 |   3.84 KB |

// ## net 7.0

// BenchmarkDotNet=v0.13.4, OS=Windows 10 (10.0.19042.928/20H2/October2020Update)
// Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
// .NET SDK=7.0.100
//   [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
//   DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

// |        Method |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
// |-------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
// | CompileSystem | 268.22 us | 10.680 us | 31.323 us | 22.98 |    3.51 | 2.9297 | 2.4414 |   9.88 KB |        3.40 |
// |   CompileFast |  11.77 us |  0.387 us |  1.123 us |  1.00 |    0.00 | 0.9460 | 0.9308 |   2.91 KB |        1.00 |
//
// ## Invoke compiled vs compiled fast
//
// |              Method |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
// |-------------------- |----------:|---------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
// |      InvokeCompiled | 666.34 ns | 9.137 ns | 8.546 ns | 664.91 ns | 20.66 |    0.76 | 0.0381 |     - |     - |     120 B |
// |  InvokeCompiledFast |  32.03 ns | 0.636 ns | 1.045 ns |  31.79 ns |  1.00 |    0.00 | 0.0178 |     - |     - |      56 B |
// | InvokePlainDelegate |  32.74 ns | 0.798 ns | 2.314 ns |  31.79 ns |  1.02 |    0.08 | 0.0178 |     - |     - |      56 B |

// ## net 7.0

// BenchmarkDotNet=v0.13.4, OS=Windows 10 (10.0.19042.928/20H2/October2020Update)
// Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
// .NET SDK=7.0.100
//   [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
//   DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2

// |              Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
// |-------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
// |      InvokeCompiled | 682.98 ns | 13.190 ns | 37.845 ns | 668.13 ns | 19.88 |    1.50 | 0.0381 |     120 B |        2.14 |
// |  InvokeCompiledFast |  34.44 ns |  0.689 ns |  1.989 ns |  33.76 ns |  1.00 |    0.00 | 0.0178 |      56 B |        1.00 |
// | InvokePlainDelegate |  35.20 ns |  0.914 ns |  2.665 ns |  34.26 ns |  1.02 |    0.10 | 0.0178 |      56 B |        1.00 |
//

    public class ExprLinqAnyOfNotNullDecimal
    {
        public class Test
        {
            public Test2[] A { get; set; }
        }

        public class Test2
        {
            public decimal? Value { get; set; }
        }

        static Expression<Func<Test, bool>> _expression = t => t.A.Any(e => e.Value != null);

/*

## Baseline

|        Method |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|   CompileFast |   7.286 us | 0.1417 us | 0.1891 us |  1.00 |    0.00 | 0.5188 | 0.4883 |   3.19 KB |        1.00 |
| CompileSystem | 111.985 us | 2.0430 us | 1.8111 us | 15.30 |    0.46 | 1.4648 | 1.2207 |  10.04 KB |        3.15 |


## v5.3.0 

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4061)
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

| Method        | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| CompileFast   |   7.070 us | 0.1356 us | 0.1268 us |  1.00 |    0.02 |    1 | 0.3815 | 0.3662 |   2.38 KB |        1.00 |
| CompileSystem | 137.265 us | 1.6377 us | 1.4518 us | 19.42 |    0.39 |    2 | 1.4648 | 1.2207 |   9.82 KB |        4.12 |
*/
        [MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
        public class Compile
        {
            [Benchmark]
            public object CompileSystem() => _expression.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => _expression.CompileFast();
        }

        static Func<Test, bool> _compiledSystem = _expression.Compile();
        static Func<Test, bool> _compiledFast = _expression.CompileFast();
        static Func<Test, bool> _delegate = t => t.A.Any(e => e.Value != null);

        static Test _test = new Test()
        {
            A = new[]
            {
                new Test2() { Value = 0 },
            },
        };

        [MemoryDiagnoser]
        public class Invoke
        {
            [Benchmark]
            public object InvokeCompiled() => _compiledSystem(_test);

            [Benchmark(Baseline = true)]
            public object InvokeCompiledFast() => _compiledFast(_test);

            [Benchmark]
            public object InvokePlainDelegate() => _delegate(_test);
        }
    }
}
