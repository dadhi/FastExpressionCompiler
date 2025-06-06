using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using FastExpressionCompiler.ImTools;

namespace FastExpressionCompiler.Benchmarks;

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

[MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
public class SmallList_Switch_vs_AsSpan_ByRef_Access
{
    /*
    ## Baseline: hmm, why AsSpan is faster even if it is utilized only by half of the acces, the other part hits the heap?

    BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4061/24H2/2024Update/HudsonValley)
    Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
    .NET SDK 9.0.203
      [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
      DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


    | Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | BranchInstructions/Op | BranchMispredictions/Op | CacheMisses/Op | Allocated | Alloc Ratio |
    |------------------------ |----------:|----------:|----------:|------:|--------:|-----:|----------------------:|------------------------:|---------------:|----------:|------------:|
    | Double_and_Sum_AsSpan   |  9.959 ns | 0.2341 ns | 0.4567 ns |  0.64 |    0.04 |    1 |                    29 |                       0 |              0 |         - |          NA |
    | Double_and_Sum_BySwitch | 15.605 ns | 0.3465 ns | 0.7532 ns |  1.00 |    0.07 |    2 |                    35 |                       0 |              0 |         - |          NA |


    ## Indexer using Unsafe.Add vs AsSpan()[index]

    | Method                 | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | BranchInstructions/Op | BranchMispredictions/Op | CacheMisses/Op | Allocated | Alloc Ratio |
    |----------------------- |---------:|---------:|---------:|------:|--------:|-----:|----------------------:|------------------------:|---------------:|----------:|------------:|
    | Double_and_Sum_Indexer | 17.29 ns | 0.380 ns | 0.355 ns |  1.00 |    0.03 |    1 |                    57 |                       0 |              0 |         - |          NA |
    | Double_and_Sum_AsSpan  | 22.10 ns | 0.311 ns | 0.275 ns |  1.28 |    0.03 |    2 |                    57 |                       0 |              0 |         - |          NA |


    ## Indexer using Rest[] vs. Rest.GetSurePresentItemRef(i)

    | Method                 | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | BranchInstructions/Op | BranchMispredictions/Op | CacheMisses/Op | Allocated | Alloc Ratio |
    |----------------------- |---------:|---------:|---------:|------:|--------:|-----:|----------------------:|------------------------:|---------------:|----------:|------------:|
    | Double_and_Sum_AsSpan  | 17.97 ns | 0.454 ns | 1.325 ns |  0.83 |    0.08 |    1 |                    41 |                       0 |              0 |         - |          NA |
    | Double_and_Sum_Indexer | 21.82 ns | 0.478 ns | 1.309 ns |  1.00 |    0.08 |    2 |                    49 |                       0 |              0 |         - |          NA |

    */

    SmallList<int, Stack8<int>> _list;

    [GlobalSetup]
    public void Init()
    {
        // half on stack and half on heap
        for (var i = 0; i < 16; i++)
            _list.Add(i);
    }

    [Benchmark(Baseline = true)]
    public int Double_and_Sum_Indexer()
    {
        var sum = 0;
        for (var i = 0; i < _list.Count; i++)
        {
            ref var n = ref _list.GetSurePresentItemRef(i);
            n += n;
            sum += n;
        }
        return sum;
    }

    // [Benchmark]
    // public int Double_and_Sum_AsSpan()
    // {
    //     var sum = 0;
    //     for (var i = 0; i < _list.Count; i++)
    //     {
    //         ref var n = ref _list.GetSurePresentItemRef2(i);
    //         n += n;
    //         sum += n;
    //     }
    //     return sum;
    // }
}

[MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
public class SmallList_Switch_vs_AsSpan_ByRef_Add
{
    /*
    ## Strange baseline

    BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4202/24H2/2024Update/HudsonValley)
    Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
    .NET SDK 9.0.203
    [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
    DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

    | Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | BranchInstructions/Op | CacheMisses/Op | BranchMispredictions/Op | Gen0   | Allocated | Alloc Ratio |
    |------------- |---------:|---------:|---------:|------:|--------:|-----:|----------------------:|---------------:|------------------------:|-------:|----------:|------------:|
    | Add_AsSpan   | 38.59 ns | 0.833 ns | 2.417 ns |  0.92 |    0.08 |    1 |                    78 |              1 |                       0 | 0.0063 |      40 B |        1.00 |
    | Add_BySwitch | 41.96 ns | 0.876 ns | 2.458 ns |  1.00 |    0.08 |    2 |                    80 |              1 |                       0 | 0.0063 |      40 B |        1.00 |
    */

    [Benchmark(Baseline = true)]
    public int Add_BySpan()
    {
        SmallList<int, Stack4<int>> list = default;

        for (var n = 8; n > 0; --n)
            list.Add(n + 3);

        var sum = 0;
        foreach (var n in list)
            sum += n;
        return sum;
    }
}

[MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
public class StackSearch
{
    /*
    ## Baseline

    BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4202/24H2/2024Update/HudsonValley)
    Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
    .NET SDK 9.0.203
    [Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
    DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


    | Method           | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Rank | BranchInstructions/Op | BranchMispredictions/Op | CacheMisses/Op | Allocated | Alloc Ratio |
    |----------------- |---------:|---------:|---------:|---------:|------:|--------:|-----:|----------------------:|------------------------:|---------------:|----------:|------------:|
    | Search_SIMD_loop | 46.65 ns | 0.763 ns | 0.637 ns | 46.84 ns |  1.00 |    0.02 |    1 |                   103 |                       0 |              0 |         - |          NA |
    | Search_ILP_4     | 91.72 ns | 1.227 ns | 1.088 ns | 91.91 ns |  1.97 |    0.03 |    2 |                   138 |                       0 |              0 |         - |          NA |
    | Search_loop      | 96.71 ns | 1.975 ns | 4.499 ns | 94.53 ns |  2.07 |    0.10 |    2 |                   274 |                       0 |              0 |         - |          NA |
    */

    [Benchmark]
    public int Search_loop()
    {
        Stack8<int> hashes = default;
        Stack8<SmallMap.Entry<int>> entries = default;

        for (var n = 0; n < 8; ++n)
        {
            hashes.GetSurePresentItemRef(n) = default(IntEq).GetHashCode(n);
            entries.GetSurePresentItemRef(n) = new SmallMap.Entry<int>(n);
        }

        var sum = 0;
        for (var i = 12; i >= -4; --i)
        {
            ref var e = ref entries.TryGetEntryRef_loop(
                ref hashes, i, out var found,
                default(IntEq), default(Use<SmallMap.Entry<int>>));
            if (found)
                sum += e.Key;
        }

        return sum;
    }

    [Benchmark]
    public int Search_ILP_4()
    {
        Stack8<int> hashes = default;
        Stack8<SmallMap.Entry<int>> entries = default;

        for (var n = 0; n < 8; ++n)
        {
            hashes.GetSurePresentItemRef(n) = default(IntEq).GetHashCode(n);
            entries.GetSurePresentItemRef(n) = new SmallMap.Entry<int>(n);
        }

        var sum = 0;
        for (var i = 12; i >= -4; --i)
        {
            ref var e = ref entries.TryGetEntryRef_ILP(
                ref hashes, i, out var found,
                default(IntEq), default(Size8), default(Use<SmallMap.Entry<int>>));
            if (found)
                sum += e.Key;
        }

        return sum;
    }

    [Benchmark(Baseline = true)]
    public int Search_SIMD_loop()
    {
        Stack8<int> hashes = default;
        Stack8<SmallMap.Entry<int>> entries = default;

        for (var n = 0; n < 8; ++n)
        {
            hashes.GetSurePresentItemRef(n) = default(IntEq).GetHashCode(n);
            entries.GetSurePresentItemRef(n) = new SmallMap.Entry<int>(n);
        }

        var sum = 0;
        for (var i = 12; i >= -4; --i)
        {
            ref var e = ref entries.TryGetEntryRef(
                ref hashes, i, out var found,
                default(IntEq), default(Size8), default(Use<SmallMap.Entry<int>>));
            if (found)
                sum += e.Key;
        }

        return sum;
    }
}
