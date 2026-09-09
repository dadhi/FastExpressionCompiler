using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.ImTools;

namespace FastExpressionCompiler.Benchmarks;

[MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class Expand_vs_Expand2
{
    // ForLoopCopyCount is 4 — that's the path where Expand and Expand2 actually differ.
    [Params(4)]
    // [Params(1, 2, 4, 8)]
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
    public Type[] Expand()
    {
        var items = Items;
        Expand(ref items, new Type[items.Length << 1]);
        return items;
    }

    [Benchmark]
    public Type[] Expand2()
    {
        var items = Items;
        Expand2(ref items, new Type[items.Length << 1]);
        return items;
    }

    // Copies of SmallList.Expand / Expand2 — they are internal.
    const int ForLoopCopyCount = 4;

    [MethodImpl((MethodImplOptions)256)]
    static void Expand<T>(ref T[] items, T[] newItems)
    {
        if (items.Length > ForLoopCopyCount)
            Array.Copy(items, newItems, items.Length);
        else
            for (var i = 0; i < items.Length; ++i)
                newItems[i] = items[i];
        items = newItems;
    }

    [MethodImpl((MethodImplOptions)256)]
    static void Expand2<T>(ref T[] items, T[] newItems)
    {
        if (items.Length > ForLoopCopyCount)
            Array.Copy(items, newItems, items.Length);
        else
            for (var i = 0; i < items.Length; ++i)
                newItems.GetSurePresentRef(i) = items.GetSurePresentRef(i);
        items = newItems;
    }
}
