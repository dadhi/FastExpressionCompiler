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


## HERE IS THE REASON: 

FEC creates the DynamicMethod with `owner` param, but System compile uses the different overload without owner and internally with `transparentMethod: true`.
Using this latter (System) overload drastically slows down the compilation but removes the additional branch instruction in the invocation, making a super simple delegates faster.
But for the delegates doing actual/more work, having additional branch instruction is negligible and usually does not show in the invocation performance.  

2x slow: `var method = new DynamicMethod(string.Empty, returnType, closurePlusParamTypes, typeof(ArrayClosure), true);`
                                                                                               ^^^^^^^^^^^^^^^^^^^^
parity:        `var method = new DynamicMethod(string.Empty, returnType, closurePlusParamTypes, true);`

Job=.NET 8.0  Runtime=.NET 8.0

| Method             | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | BranchInstructions/Op | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|------:|--------:|-----:|----------------------:|----------:|------------:|
| InvokeCompiled     | 0.5075 ns | 0.0153 ns | 0.0143 ns |  1.00 |    0.04 |    1 |                     1 |         - |          NA |
| InvokeCompiledFast | 0.5814 ns | 0.0433 ns | 0.0699 ns |  1.15 |    0.14 |    1 |                     1 |         - |          NA |


## Not with full eval before Compile the results are funny in the good way

Job=.NET 8.0  Runtime=.NET 8.0

| Method                         | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | BranchInstructions/Op | Allocated | Alloc Ratio |
|------------------------------- |----------:|----------:|----------:|------:|--------:|-----:|----------------------:|----------:|------------:|
| InvokeCompiled                 | 0.5071 ns | 0.0289 ns | 0.0242 ns |  1.00 |    0.06 |    2 |                     1 |         - |          NA |
| InvokeCompiledFastWithEvalFlag | 0.0804 ns | 0.0341 ns | 0.0351 ns |  0.16 |    0.07 |    1 |                     1 |         - |          NA |


## Fastest so far

DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

| Method                                | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | BranchInstructions/Op | Allocated | Alloc Ratio |
|-------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-----:|----------------------:|----------:|------------:|
| InvokeCompiled                        | 0.5088 ns | 0.0399 ns | 0.0842 ns | 0.4707 ns |  1.02 |    0.22 |    2 |                     1 |         - |          NA |
| InvokeCompiledFast                    | 0.1105 ns | 0.0360 ns | 0.0799 ns | 0.0689 ns |  0.22 |    0.16 |    1 |                     1 |         - |          NA |
| InvokeCompiledFast_DisableInterpreter | 1.0607 ns | 0.0540 ns | 0.0887 ns | 1.0301 ns |  2.13 |    0.34 |    3 |                     2 |         - |          NA |

## Comparing to the direct interpretation

| Method                                | Mean       | Error     | StdDev    | Median     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |-----------:|----------:|----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| InvokeCompiled                        |  0.3347 ns | 0.0313 ns | 0.0373 ns |  0.3241 ns |   1.01 |    0.15 |    3 |      - |         - |          NA |
| InvokeCompiledFast                    |  0.0269 ns | 0.0214 ns | 0.0229 ns |  0.0198 ns |   0.08 |    0.07 |    1 |      - |         - |          NA |
| InvokeCompiledFast_DisableInterpreter |  0.9317 ns | 0.0485 ns | 0.0558 ns |  0.9097 ns |   2.81 |    0.31 |    4 |      - |         - |          NA |
| Interpret                             | 81.7969 ns | 0.6588 ns | 0.5501 ns | 81.7534 ns | 246.89 |   23.21 |    5 | 0.0076 |      48 B |          NA |
| JustFunc                              |  0.0335 ns | 0.0219 ns | 0.0499 ns |  0.0000 ns |   0.10 |    0.15 |    2 |      - |         - |          NA |

## Comparing basic interpretation without boxing all-in-one and new one split by types: bool, decimal, the rest

DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

| Method        | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Interpret     | 93.63 ns | 1.819 ns | 3.838 ns | 92.05 ns |  1.00 |    0.06 |    2 |         - |          NA |
| Interpret_new | 68.28 ns | 1.350 ns | 1.554 ns | 68.08 ns |  0.73 |    0.03 |    1 |         - |          NA |

## Removing the type code from the PValue struct, no changes

DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

| Method        | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Interpret     | 86.94 ns | 1.015 ns | 0.847 ns |  1.00 |    0.01 |    2 |         - |          NA |
| Interpret_new | 64.04 ns | 0.838 ns | 1.446 ns |  0.74 |    0.02 |    1 |         - |          NA |

## Specializing for int does it job

| Method        | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Interpret     | 94.70 ns | 1.923 ns | 2.936 ns | 95.38 ns |  1.00 |    0.04 |    2 |         - |          NA |
| Interpret_new | 57.18 ns | 1.175 ns | 3.075 ns | 55.78 ns |  0.60 |    0.04 |    1 |         - |          NA |

## More int specialization

| Method        | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Interpret     | 88.66 ns | 1.701 ns | 1.747 ns |  1.00 |    0.03 |    2 |         - |          NA |
| Interpret_new | 48.80 ns | 0.671 ns | 0.595 ns |  0.55 |    0.01 |    1 |         - |          NA |

*/
[MemoryDiagnoser, RankColumn]
// [HardwareCounters(HardwareCounter.BranchInstructions)]
// [SimpleJob(RuntimeMoniker.Net90)]
// [SimpleJob(RuntimeMoniker.Net80)]
public class Issue468_InvokeCompiled_vs_InvokeCompiledFast
{
    Expression<Func<bool>> _expr;
    Func<bool> _compiled, _compiledFast, _compiledFast_DisableInterpreter, _justFunc = static () => true;

    [GlobalSetup]
    public void Setup()
    {
        var expr = IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET.CreateExpression();
        _compiled = expr.CompileSys();
        _compiledFast = expr.CompileFast();
        _compiledFast_DisableInterpreter = expr.CompileFast(flags: CompilerFlags.DisableInterpreter);
        _expr = expr;
    }

    // [Benchmark(Baseline = true)]
    public bool InvokeCompiled()
    {
        return _compiled();
    }

    // [Benchmark]
    public bool InvokeCompiledFast()
    {
        return _compiledFast();
    }

    // [Benchmark]
    public bool InvokeCompiledFast_DisableInterpreter()
    {
        return _compiledFast_DisableInterpreter();
    }

    // [Benchmark(Baseline = true)]
    // public bool Interpret()
    // {
    //     return ExpressionCompiler.Interpreter.TryInterpretBool(out var result, _expr.Body) && result;
    // }

    [Benchmark]
    public bool Interpret_new()
    {
        return ExpressionCompiler.Interpreter.TryInterpretBool(out var result, _expr.Body, CompilerFlags.Default) && result;
    }

    // [Benchmark]
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


## Funny results after adding eval before compile

Job=.NET 8.0  Runtime=.NET 8.0

| Method                    | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Compiled                  | 22,507.0 ns | 435.99 ns | 652.57 ns | 22,519.1 ns | 131.40 |    8.03 |    3 | 0.6714 | 0.6409 |    4232 B |       11.02 |
| CompiledFast              |  3,051.9 ns |  59.71 ns |  55.86 ns |  3,036.6 ns |  17.82 |    1.01 |    2 | 0.1755 | 0.1678 |    1143 B |        2.98 |
| CompiledFast_WithEvalFlag |    171.8 ns |   3.49 ns |   9.44 ns |    167.6 ns |   1.00 |    0.08 |    1 | 0.0610 |      - |     384 B |        1.00 |


## Now we're talking (after small interpreter optimizations)

DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2

| Method                          | Mean         | Error      | StdDev     | Median       | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |-------------:|-----------:|-----------:|-------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Compiled                        | 22,937.50 ns | 447.883 ns | 784.432 ns | 22,947.67 ns | 230.86 |   14.14 |    3 | 0.6714 | 0.6409 |    4232 B |       88.17 |
| CompiledFast                    |     99.62 ns |   2.044 ns |   5.275 ns |     97.03 ns |   1.00 |    0.07 |    1 | 0.0076 |      - |      48 B |        1.00 |
| CompiledFast_DisableInterpreter |  3,010.37 ns |  60.174 ns |  91.893 ns |  3,010.03 ns |  30.30 |    1.80 |    2 | 0.1755 | 0.1678 |    1143 B |       23.81 |


## New results after cleanup

| Method                          | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Compiled                        | 22,844.78 ns | 327.497 ns | 290.317 ns | 351.80 |    6.40 |    3 | 0.6714 | 0.6409 |    4232 B |          NA |
| CompiledFast_DisableInterpreter |  3,089.13 ns |  54.757 ns |  48.541 ns |  47.57 |    0.96 |    2 | 0.1793 | 0.1755 |    1144 B |          NA |
| CompiledFast                    |     64.95 ns |   1.082 ns |   0.903 ns |   1.00 |    0.02 |    1 |      - |      - |         - |          NA |

*/
[MemoryDiagnoser, RankColumn]
// [SimpleJob(RuntimeMoniker.Net90)]
// [SimpleJob(RuntimeMoniker.Net80)]
public class Issue468_Compile_vs_FastCompile
{
    Expression<Func<bool>> _expr;

    [GlobalSetup]
    public void Setup()
    {
        _expr = IssueTests.Issue468_Optimize_the_delegate_access_to_the_Closure_object_for_the_modern_NET.CreateExpression();
    }

    [Benchmark]
    public object Compiled()
    {
        return _expr.Compile();
    }

    [Benchmark]
    public object CompiledFast_DisableInterpreter()
    {
        return _expr.CompileFast(flags: CompilerFlags.DisableInterpreter);
    }

    [Benchmark(Baseline = true)]
    public object CompiledFast()
    {
        return _expr.CompileFast();
    }
}
