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
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
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

    //[CoreJob, ClrJob]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
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
