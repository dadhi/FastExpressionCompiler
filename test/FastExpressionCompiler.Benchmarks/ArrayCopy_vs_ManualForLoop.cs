using System;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class ArrayCopy_vs_ManualForLoop
    {
        [Params(3, 5)]
        public int Count;

        public Type[] Items;

        [GlobalSetup]
        public void Init()
        {
            Items = new Type[Count];
            for (var i = 1; i < Count; i++) Items[i] = GetType();

            Items[0] = typeof(string);
        }

        [Benchmark(Baseline = true)]
        public Type[] ArrayCopy()
        {
            var source = Items;
            var target = new Type[source.Length];
            Array.Copy(source, 0, target, 0, source.Length);

            return target;
        }

        [Benchmark]
        public Type[] ManualForLoop()
        {
            var source = Items;
            var target = new Type[source.Length];
            for (var i = 0; i < source.Length; i++)
                target[i] = source[i];

            return target;
        }

    }
}
