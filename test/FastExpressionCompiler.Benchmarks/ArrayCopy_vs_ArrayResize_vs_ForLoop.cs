using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace FastExpressionCompiler.Benchmarks
{
/*
BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.307
  [Host]     : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2


|             Method | Count |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |   Gen0 | BranchInstructions/Op | CacheMisses/Op | BranchMispredictions/Op | Allocated | Alloc Ratio |
|------------------- |------ |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------------------:|---------------:|------------------------:|----------:|------------:|
|          ArrayCopy |     4 | 18.24 ns | 0.427 ns | 0.902 ns | 17.97 ns |  1.00 |    0.00 | 0.0140 |                    42 |              0 |                       0 |      88 B |        1.00 |
|      ManualForLoop |     4 | 15.43 ns | 0.368 ns | 1.025 ns | 15.08 ns |  0.86 |    0.07 | 0.0140 |                    30 |              0 |                       0 |      88 B |        1.00 |
| MarshallingForLoop |     4 | 19.52 ns | 0.896 ns | 2.526 ns | 18.41 ns |  1.03 |    0.11 | 0.0140 |                    40 |              0 |                       0 |      88 B |        1.00 |
|        ArrayResize |     4 | 24.71 ns | 2.497 ns | 7.165 ns | 22.67 ns |  1.59 |    0.39 | 0.0140 |                    47 |              0 |                       0 |      88 B |        1.00 |
*/

    [MemoryDiagnoser]
    [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
    public class ArrayCopy_vs_ArrayResize_vs_ForLoop
    {
        [Params(4)]
        // [Params(4, 8)]
        public int Count;

        public Type[] Items;

        [GlobalSetup]
        public void Init()
        {
            Items = new Type[Count];
            for (var i = 1; i < Count; i++) 
                Items[i] = GetType();

            Items[0] = typeof(string);
        }

        [Benchmark(Baseline = true)]
        public Type[] ArrayCopy()
        {
            var source = Items;
            var target = new Type[source.Length << 1];
            Array.Copy(source, 0, target, 0, source.Length);
            return target;
        }

        [Benchmark]
        public Type[] ManualForLoop()
        {
            var source = Items;
            var target = new Type[source.Length << 1];
            for (var i = 0; i < source.Length; i++)
                target[i] = source[i];
            return target;
        }

        [Benchmark]
        public Type[] MarshallingForLoop()
        {
            var count = Items.Length;
            ref var source = ref MemoryMarshal.GetArrayDataReference(Items);
            ref var sourceNoMore = ref Unsafe.Add(ref source, count);
            var targetArr = new Type[count << 1];
            ref var target = ref MemoryMarshal.GetArrayDataReference(targetArr);
            while (Unsafe.IsAddressLessThan(ref source, ref sourceNoMore))
            {
                target = source;
                target = ref Unsafe.Add(ref target, 1);
                source = ref Unsafe.Add(ref source, 1);
            }
            return targetArr;
        }

        [Benchmark]
        public Type[] ArrayResize()
        {
            var target = Items;
            Array.Resize(ref target, target.Length << 1);
            return target;
        }
    }
}
