using System;
using BenchmarkDotNet.Attributes;
using System.Linq.Expressions;

namespace FastExpressionCompiler.Benchmarks
{
    public class GenericConverter
    {
        /*

        |      Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
        |     Compile | 57.371 us | 0.2667 us | 0.2364 us |  8.82 |    0.08 | 0.9155 | 0.4272 |      - |    4.4 KB |
        | CompileFast |  6.506 us | 0.0561 us | 0.0498 us |  1.00 |    0.00 | 0.4959 | 0.2441 | 0.0305 |   2.29 KB |

         */
        [MemoryDiagnoser]
        public class Compilation
        {
            [Benchmark]
            public Func<int, X> Compile() => 
                GetConverter<int, X>();

            [Benchmark(Baseline = true)]
            public Func<int, X> CompileFast() =>
                GetConverter_CompiledFast<int, X>();
        }

        [MemoryDiagnoser, IterationCount(50)]
        public class Invocation
        {
            private static readonly Func<int, X> _compiled     = GetConverter<int, X>();
            private static readonly Func<int, X> _compiledFast = GetConverter_CompiledFast<int, X>();

            [Benchmark]
            public X Invoke_Compiled() =>
                _compiled(1);

            [Benchmark(Baseline = true)]
            public X Invoke_CompileFast() =>
                _compiledFast(1);
        }

        public static Func<TFrom, TTo> GetConverter<TFrom, TTo>()
        {
            var fromParam = Expression.Parameter(typeof(TFrom), "from");
            var expr = Expression.Lambda<Func<TFrom, TTo>>(Expression.Convert(fromParam, typeof(TTo)), fromParam);
            return expr.Compile();
        }

        public static Func<TFrom, TTo> GetConverter_CompiledFast<TFrom, TTo>()
        {
            var fromParam = Expression.Parameter(typeof(TFrom), "from");
            var expr = Expression.Lambda<Func<TFrom, TTo>>(Expression.Convert(fromParam, typeof(TTo)), fromParam);
            return expr.CompileFast(true);
        }

        public enum X
        {
            A,
            B
        };
    }
}
