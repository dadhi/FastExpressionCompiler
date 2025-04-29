using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace FastExpressionCompiler.Benchmarks;

/*
## After the the work done foe #468 the results are the following:

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3775)
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.203
  [Host]   : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.15 (8.0.1525.16413), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2


| Method             | Job      | Runtime  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |--------- |--------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| InvokeCompiled     | .NET 8.0 | .NET 8.0 | 0.4535 ns | 0.0262 ns | 0.0245 ns |  1.00 |    0.07 |         - |          NA |
| InvokeCompiledFast | .NET 8.0 | .NET 8.0 | 0.4847 ns | 0.0056 ns | 0.0049 ns |  1.07 |    0.06 |         - |          NA |
|                    |          |          |           |           |           |       |         |           |             |
| InvokeCompiled     | .NET 9.0 | .NET 9.0 | 0.4893 ns | 0.0022 ns | 0.0018 ns |  1.00 |    0.01 |         - |          NA |
| InvokeCompiledFast | .NET 9.0 | .NET 9.0 | 0.4990 ns | 0.0125 ns | 0.0105 ns |  1.02 |    0.02 |         - |          NA |
*/

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
public class Issue468_Compile_vs_FastCompile
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
    return _compiled();
  }
}
