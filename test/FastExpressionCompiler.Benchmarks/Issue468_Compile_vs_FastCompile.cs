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

*/
[MemoryDiagnoser, RankColumn]
[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]
public class Issue468_InvokeCompiled_vs_InvokeCompiledFast
{
    Func<bool> _compiled, _compiledFast;

    [GlobalSetup]
    public void Setup()
    {
        var expr = IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET.CreateExpression();
        _compiled = expr.CompileSys();
        _compiledFast = expr.CompileFast();
    }

    [Benchmark(Baseline = true)]
    public bool InvokeCompiled()
    {
        return _compiled();
    }

    [Benchmark]
    public bool InvokeCompiledFast()
    {
        return _compiledFast();
    }
}

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
}
