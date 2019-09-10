using System;
using BenchmarkDotNet.Attributes;
using System.Linq.Expressions;

namespace FastExpressionCompiler.Benchmarks
{
    public class GenericConverter
    {
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
