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
//
//
// ## Invoke compiled vs compiled fast
//
// |             Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
// |------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
// | InvokeCompiled     | 690.90 ns | 13.430 ns | 20.101 ns | 688.92 ns | 19.05 |    1.57 | 0.0381 |     - |     - |     120 B |
// | InvokeCompiledFast |  35.37 ns |  1.191 ns |  3.513 ns |  34.21 ns |  1.00 |    0.00 | 0.0178 |     - |     - |      56 B |
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

        [MemoryDiagnoser]
        public class Compile
        {
            [Benchmark]
            public object CompileSystem() => _expression.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => _expression.CompileFast();
        }

        static Func<Test, bool> _compiledSystem = _expression.Compile();
        static Func<Test, bool> _compiledFast = _expression.CompileFast();

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
        }
    }
}
