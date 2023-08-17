using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using FastExpressionCompiler.ImTools;

namespace FastExpressionCompiler.Benchmarks;

using static ExpressionCompiler;

/*
## Identical performance via inlining

BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.306
  [Host]     : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.9 (7.0.923.32018), X64 RyuJIT AVX2

|              Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
|         AccessByRef | 5.079 ns | 0.1242 ns | 0.1820 ns |  1.00 |    0.00 |         - |          NA |
| ByIGetRefStructImpl | 5.034 ns | 0.1238 ns | 0.2325 ns |  0.99 |    0.05 |         - |          NA |

## Still no inlining of the static local metods :(

|                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
|            AccessByRef |  4.900 ns | 0.1073 ns | 0.0952 ns |  1.00 |    0.00 |         - |          NA |
| StaticMethodAsDelegate | 17.237 ns | 0.2382 ns | 0.1859 ns |  3.52 |    0.09 |         - |          NA |
*/

[MemoryDiagnoser]
public class AccessByRef_vs_ByIGetRefStructImpl
{
    private readonly SmallList<LabelInfo> Labels;

    public AccessByRef_vs_ByIGetRefStructImpl()
    {
        for (var i = 0; i < 8; ++i)
            Labels.PushLastDefault();
    }

    // [Benchmark(Baseline = true)]
    // public void AccessByRef()
    // {
    //     for (short i = 3; i < 8; ++i)
    //     {
    //         ref var l = ref Labels.DebugDeepItems[i];
    //         l.InlinedLambdaInvokeIndex = i;
    //     }
    // }

    // [Benchmark]
    // public void ByIGetRefStructImpl()
    // {
    //     for (short i = 3; i < 8; ++i)
    //         Labels.GetSurePresentItem<SetInlinedLambdaInvokeIndex, short, xo>(i, i);
    // }

    // [Benchmark]
    // public void StaticMethodAsDelegate()
    // {
    //     for (short i = 3; i < 8; ++i)
    //         Labels.GetSurePresentItem(i, i, Handle);

    //     [MethodImpl((MethodImplOptions)256)]
    //     static xo Handle(ref LabelInfo it, in short n)
    //     {
    //         it.InlinedLambdaInvokeIndex = n;
    //         return default;
    //     }
    // }

    // public struct SetInlinedLambdaInvokeIndex : IHandleRef<LabelInfo, short, xo>
    // {
    //     [MethodImpl((MethodImplOptions)256)]
    //     public xo Handle(ref LabelInfo it, in short n)
    //     {
    //         it.InlinedLambdaInvokeIndex = n;
    //         return default;
    //     }
    // }
}
