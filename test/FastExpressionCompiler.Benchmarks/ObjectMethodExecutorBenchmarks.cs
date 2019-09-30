using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MSE = Microsoft.Extensions.Internal;
using MSELE = Microsoft.Extensions.Internal.LE;

namespace FastExpressionCompiler.Benchmarks
{
    //[CoreJob, ClrJob]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ObjectExecutor_SyncMethod_Compile
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutor_SyncMethod_Compile);

        [Benchmark]
        public object Compile() =>
            MSE.ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object CompileFast() =>
            MSE.ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    //[CoreJob, ClrJob]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ObjectExecutor_SyncMethod_Execute
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutor_SyncMethod_Execute);

        private static readonly MSE.ObjectMethodExecutor _compiled =
            MSE.ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly MSE.ObjectMethodExecutorCompiledFast _compiledFast =
            MSE.ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public object Compiled() => _compiled.Execute(this, _parameters);

        [Benchmark(Baseline = true)]
        public object CompiledFast() => _compiledFast.Execute(this, _parameters);
    }

    //[CoreJob]
    [MemoryDiagnoser]
    //[Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ObjectExecutor_AsyncMethod_CreateExecutor
    {
        /*
        ## 25.01.2019: Results in v2.0
                                     Method |        Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------------------------------- |------------:|-----------:|-----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
         CompileFastWithPreCreatedClosureLE |    65.95 us |  0.5213 us |  0.4876 us |  1.00 |    0.00 |      5.2490 |      2.5635 |      0.3662 |            24.28 KB |
           CompileFastWithPreCreatedClosure |    98.33 us |  0.9981 us |  0.8848 us |  1.49 |    0.02 |      5.7373 |      2.8076 |      0.3662 |            26.69 KB |
                                CompileFast |   107.19 us |  0.7070 us |  0.6613 us |  1.63 |    0.01 |      5.9814 |      2.9297 |      0.3662 |            27.72 KB |
                                    Compile | 1,560.78 us | 12.1744 us | 11.3879 us | 23.67 |    0.27 |      7.8125 |      3.9063 |           - |            42.05 KB |

        ## v2.1.0: 
                                     Method |        Mean |      Error |     StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------------------------------- |------------:|-----------:|-----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                                    Compile | 1,392.35 us | 14.8822 us | 13.9208 us | 19.99 |    0.26 |      7.8125 |      3.9063 |           - |            42.05 KB |
                                CompileFast |   112.59 us |  0.9482 us |  0.8869 us |  1.61 |    0.02 |      6.3477 |      3.1738 |      0.4883 |            29.33 KB |
           CompileFastWithPreCreatedClosure |   102.35 us |  0.9648 us |  0.8056 us |  1.47 |    0.01 |      6.1035 |      3.0518 |      0.4883 |            28.25 KB |
         CompileFastWithPreCreatedClosureLE |    69.74 us |  0.3595 us |  0.3002 us |  1.00 |    0.00 |      5.6152 |      2.8076 |      0.3662 |            25.79 KB |

        ## v2.1 after closing the #196

                                     Method |        Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------------------------------- |------------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                                    Compile | 1,377.31 us | 8.5576 us | 8.0048 us | 20.80 |    0.22 |      7.8125 |      3.9063 |           - |            42.05 KB |
                                CompileFast |   104.74 us | 0.9695 us | 0.8594 us |  1.58 |    0.02 |      6.2256 |      3.0518 |      0.4883 |            28.74 KB |
           CompileFastWithPreCreatedClosure |    99.44 us | 1.0721 us | 1.0029 us |  1.50 |    0.02 |      5.9814 |      2.9297 |      0.3662 |            27.83 KB |
         CompileFastWithPreCreatedClosureLE |    66.22 us | 0.4996 us | 0.4673 us |  1.00 |    0.00 |      6.2256 |      3.0518 |      0.4883 |               29 KB |

        ## V3

                                     Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
        ----------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
                                    Compile | 1,574.21 us | 2.5959 us | 2.0267 us | 23.20 |    0.07 | 7.8125 | 3.9063 |      - |  42.17 KB |
                                CompileFast |    98.17 us | 0.7560 us | 0.6702 us |  1.45 |    0.01 | 5.7373 | 2.8076 | 0.3662 |  26.36 KB |
           CompileFastWithPreCreatedClosure |    98.22 us | 0.6903 us | 0.6120 us |  1.45 |    0.01 | 5.6152 | 2.8076 | 0.3662 |  26.07 KB |
         CompileFastWithPreCreatedClosureLE |    67.90 us | 0.2061 us | 0.1928 us |  1.00 |    0.00 | 5.3711 | 2.6855 | 0.3662 |  24.71 KB |
        */

        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_CreateExecutor);

        [Benchmark]
        public object Compile() =>
            MSE.ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark]
        public object CompileFast() =>
            MSE.ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark]
        public object CompileFastWithPreCreatedClosure() =>
            MSE.ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object CompileFastWithPreCreatedClosureLE() =>
            MSELE.ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    //[CoreJob, ClrJob]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class ObjectExecutor_AsyncMethod_Execute
    {
        /*
        ## 25.01.2019: Results in v2.0
               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
             Compiled | 66.72 ns | 0.1322 ns | 0.1237 ns |  0.99 |      0.0372 |           - |           - |               176 B |
         CompiledFast | 67.60 ns | 0.2153 ns | 0.1798 ns |  1.00 |      0.0372 |           - |           - |               176 B |
         */

        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_Execute);

        private static readonly MSE.ObjectMethodExecutor _compiled =
            MSE.ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly MSE.ObjectMethodExecutorCompiledFast _compiledFast =
            MSE.ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public object Compiled() => ((Task<string>)_compiled.Execute(this, _parameters)).Result;

        [Benchmark(Baseline = true)]
        public object CompiledFast() => ((Task<string>)_compiledFast.Execute(this, _parameters)).Result;
    }

    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_ExecuteAsync
    {
        /*
        ## 25.01.2019: Results in v2.0

                                      Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ------------------------------------ |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
         CompiledFastWithPreCreatedClosureLE | 136.8 ns | 0.4822 ns | 0.4511 ns |  1.00 |      0.0422 |           - |           - |               200 B |
                                CompiledFast | 137.4 ns | 0.6253 ns | 0.5543 ns |  1.00 |      0.0422 |           - |           - |               200 B |
           CompiledFastWithPreCreatedClosure | 138.2 ns | 0.5283 ns | 0.4942 ns |  1.01 |      0.0422 |           - |           - |               200 B |
                                    Compiled | 145.0 ns | 0.4992 ns | 0.4670 ns |  1.06 |      0.0422 |           - |           - |               200 B |

        ## V3
                                      Method |     Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
        ------------------------------------ |---------:|----------:|----------:|------:|-------:|------:|------:|----------:|
         CompiledFastWithPreCreatedClosureLE | 132.5 ns | 0.6429 ns | 0.6013 ns |  1.00 | 0.0422 |     - |     - |     200 B |
           CompiledFastWithPreCreatedClosure | 134.0 ns | 0.1357 ns | 0.1203 ns |  1.01 | 0.0422 |     - |     - |     200 B |
                                CompiledFast | 135.3 ns | 0.4344 ns | 0.3628 ns |  1.02 | 0.0422 |     - |     - |     200 B |
                                    Compiled | 140.2 ns | 0.2053 ns | 0.1820 ns |  1.06 | 0.0422 |     - |     - |     200 B |

         */
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_ExecuteAsync);

        private static readonly MSE.ObjectMethodExecutor _compiled =
            MSE.ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly MSE.ObjectMethodExecutorCompiledFast _compiledFast =
            MSE.ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
        
        private static readonly MSE.ObjectMethodExecutorCompiledFastClosure _compiledFastWithPreCreatedClosure =
            MSE.ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly MSELE.ObjectMethodExecutorCompiledFastClosure _compiledFastWithPreCreatedClosureLE =
            MSELE.ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public async Task Compiled() => await _compiled.ExecuteAsync(this, _parameters);

        [Benchmark]
        public async Task CompiledFast() => await _compiledFast.ExecuteAsync(this, _parameters);

        [Benchmark]
        public async Task CompiledFastWithPreCreatedClosure() => await _compiledFastWithPreCreatedClosure.ExecuteAsync(this, _parameters);

        [Benchmark(Baseline = true)]
        public async Task CompiledFastWithPreCreatedClosureLE() => await _compiledFastWithPreCreatedClosureLE.ExecuteAsync(this, _parameters);
    }
}
