using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace FastExpressionCompiler.Benchmarks;

/*
## Base line with the static method, it seems to be a wrong idea for the improvement, because the closure-bound method is faster as I did discovered a long ago.

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3775)
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.203
  [Host]   : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


| Method             | Job      | Runtime  | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | BranchInstructions/Op | CacheMisses/Op | BranchMispredictions/Op | Allocated | Alloc Ratio |
|------------------- |--------- |--------- |----------:|----------:|----------:|------:|--------:|-----:|----------------------:|---------------:|------------------------:|----------:|------------:|
| InvokeCompiled     | .NET 8.0 | .NET 8.0 | 0.4365 ns | 0.0246 ns | 0.0192 ns |  1.00 |    0.06 |    1 |                     1 |             -0 |                      -0 |         - |          NA |
| InvokeCompiledFast | .NET 8.0 | .NET 8.0 | 1.0837 ns | 0.0557 ns | 0.0991 ns |  2.49 |    0.25 |    2 |                     2 |              0 |                       0 |         - |          NA |
|                    |          |          |           |           |           |       |         |      |                       |                |                         |           |             |
| InvokeCompiled     | .NET 9.0 | .NET 9.0 | 0.5547 ns | 0.0447 ns | 0.0871 ns |  1.02 |    0.22 |    1 |                     1 |             -0 |                      -0 |         - |          NA |
| InvokeCompiledFast | .NET 9.0 | .NET 9.0 | 1.1920 ns | 0.0508 ns | 0.0450 ns |  2.20 |    0.34 |    2 |                     2 |              0 |                      -0 |         - |          NA |


## Sealing the closure type does not help

| Method             | Job      | Runtime  | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | BranchInstructions/Op | BranchMispredictions/Op | CacheMisses/Op | Allocated | Alloc Ratio |
|------------------- |--------- |--------- |----------:|----------:|----------:|----------:|------:|--------:|-----:|----------------------:|------------------------:|---------------:|----------:|------------:|
| InvokeCompiledFast | .NET 8.0 | .NET 8.0 | 1.0066 ns | 0.0209 ns | 0.0233 ns | 0.9973 ns |  1.00 |    0.03 |    2 |                     2 |                       0 |              0 |         - |          NA |
| InvokeCompiled     | .NET 8.0 | .NET 8.0 | 0.5040 ns | 0.0217 ns | 0.0169 ns | 0.5016 ns |  0.50 |    0.02 |    1 |                     1 |                      -0 |             -0 |         - |          NA |
|                    |          |          |           |           |           |           |       |         |      |                       |                         |                |           |             |
| InvokeCompiledFast | .NET 9.0 | .NET 9.0 | 1.0640 ns | 0.0539 ns | 0.0929 ns | 1.0106 ns |  1.01 |    0.12 |    2 |                     2 |                       0 |              0 |         - |          NA |
| InvokeCompiled     | .NET 9.0 | .NET 9.0 | 0.5897 ns | 0.0451 ns | 0.0858 ns | 0.6156 ns |  0.56 |    0.09 |    1 |                     1 |                      -0 |             -0 |         - |          NA |


## Steel the same speed with the minimal IL of 2 instructions

Job=.NET 8.0  Runtime=.NET 8.0  

| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| InvokeCompiled     | 0.4647 ns | 0.0321 ns | 0.0268 ns |  1.00 |    0.08 |    1 |         - |          NA |
| InvokeCompiledFast | 0.9739 ns | 0.0433 ns | 0.0481 ns |  2.10 |    0.15 |    2 |         - |          NA |


## But the Func speed is faster, hmm

Job=.NET 8.0  Runtime=.NET 8.0

| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| InvokeCompiled | 0.2685 ns | 0.0210 ns | 0.0186 ns |  1.00 |    0.09 |    2 |         - |          NA |
| JustFunc       | 0.1711 ns | 0.0310 ns | 0.0305 ns |  0.64 |    0.12 |    1 |         - |          NA |

*/
[MemoryDiagnoser, RankColumn]
// [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
// [SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]
public class Issue468_InvokeCompiled_vs_InvokeCompiledFast
{
    Func<bool> _compiled, _compiledFast, _justFunc = static () => true;

    [GlobalSetup]
    public void Setup()
    {
        var expr = IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET.CreateExpression();
        _compiled = expr.CompileSys();
        _compiledFast = expr.CompileFast(flags: CompilerFlags.EvaluateExpressionIfPossible);
    }

    [Benchmark(Baseline = true)]
    public bool InvokeCompiled()
    {
        return _compiled();
    }

    // [Benchmark]
    public bool InvokeCompiledFast()
    {
        return _compiledFast();
    }

    [Benchmark]
    public bool JustFunc()
    {
        return _justFunc();
    }
}

/*
## Baseline. Does not look good. There is actually a regression I need to find and fix.

| Method       | Job      | Runtime  | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------- |--------- |--------- |---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Compiled     | .NET 8.0 | .NET 8.0 | 23.51 us | 0.468 us | 0.715 us |  1.00 |    0.04 |    2 | 0.6714 | 0.6409 |   4.13 KB |        1.00 |
| CompiledFast | .NET 8.0 | .NET 8.0 | 17.63 us | 0.156 us | 0.146 us |  0.75 |    0.02 |    1 | 0.1831 | 0.1526 |   1.16 KB |        0.28 |
|              |          |          |          |          |          |       |         |      |        |        |           |             |
| Compiled     | .NET 9.0 | .NET 9.0 | 21.27 us | 0.114 us | 0.106 us |  1.00 |    0.01 |    2 | 0.6714 | 0.6409 |   4.13 KB |        1.00 |
| CompiledFast | .NET 9.0 | .NET 9.0 | 16.82 us | 0.199 us | 0.186 us |  0.79 |    0.01 |    1 | 0.1831 | 0.1526 |   1.16 KB |        0.28 |


## After reverting the regression 

| Method                    | Job      | Runtime  | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |--------- |--------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Compiled                  | .NET 8.0 | .NET 8.0 | 25.093 us | 0.4979 us | 1.1034 us |  1.00 |    0.06 |    2 | 0.6714 | 0.6104 |   4.13 KB |        1.00 |
| CompiledFast              | .NET 8.0 | .NET 8.0 |  3.433 us | 0.0680 us | 0.0603 us |  0.14 |    0.01 |    1 | 0.1678 | 0.1526 |   1.12 KB |        0.27 |
| CompiledFast_WithEvalFlag | .NET 8.0 | .NET 8.0 |  3.419 us | 0.0675 us | 0.1409 us |  0.14 |    0.01 |    1 | 0.2365 | 0.2289 |   1.48 KB |        0.36 |
|                           |          |          |           |           |           |       |         |      |        |        |           |             |
| Compiled                  | .NET 9.0 | .NET 9.0 | 25.491 us | 0.4667 us | 0.4137 us |  1.00 |    0.02 |    2 | 0.6714 | 0.6104 |   4.13 KB |        1.00 |
| CompiledFast              | .NET 9.0 | .NET 9.0 |  3.337 us | 0.0634 us | 0.0593 us |  0.13 |    0.00 |    1 | 0.1793 | 0.1755 |   1.12 KB |        0.27 |
| CompiledFast_WithEvalFlag | .NET 9.0 | .NET 9.0 |  3.198 us | 0.0628 us | 0.0588 us |  0.13 |    0.00 |    1 | 0.2365 | 0.2289 |   1.48 KB |        0.36 |

*/
[MemoryDiagnoser, RankColumn]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]
public class Issue468_Compile_vs_FastCompile
{
    Expression<Func<bool>> _expr;

    [GlobalSetup]
    public void Setup()
    {
        _expr = IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET.CreateExpression();
    }

    [Benchmark(Baseline = true)]
    public object Compiled()
    {
        return _expr.Compile();
    }

    [Benchmark]
    public object CompiledFast()
    {
        return _expr.CompileFast();
    }

    [Benchmark]
    public object CompiledFast_WithEvalFlag()
    {
        return _expr.CompileFast(flags: CompilerFlags.EvaluateExpressionIfPossible);
    }
}
