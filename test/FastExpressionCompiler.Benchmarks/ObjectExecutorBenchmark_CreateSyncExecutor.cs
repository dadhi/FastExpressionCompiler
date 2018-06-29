using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Internal;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectExecutorBenchmark_CreateSyncExecutor
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutorBenchmark_CreateSyncExecutor);

        [Benchmark]
        public object ObjExec() => 
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() => 
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }
    
    [MemoryDiagnoser]
    public class ObjectExecutorBenchmark_CreateAsyncExecutor
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutorBenchmark_CreateAsyncExecutor);

        [Benchmark]
        public object ObjExec() => 
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() => 
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    [MemoryDiagnoser]
    public class ObjectExecutorBenchmark_AsyncExecutor_InvokeSync
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutorBenchmark_AsyncExecutor_InvokeSync);

        private static readonly ObjectMethodExecutor _exec =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _execFastCompiled =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = {1, 2};

        [Benchmark]
        public object ObjExec() => _exec.Execute(this, _parameters);

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() => _execFastCompiled.Execute(this, _parameters);
    }

    [MemoryDiagnoser]
    public class ObjectExecutorBenchmark_AsyncExecutor_InvokeAsync_WithAwait
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutorBenchmark_AsyncExecutor_InvokeAsync_WithAwait);

        private static readonly ObjectMethodExecutor _exec =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _execFastCompiled =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public async Task ObjExec() => await _exec.ExecuteAsync(this, _parameters);

        [Benchmark(Baseline = true)]
        public async Task ObjExec_CompiledFast() => await _execFastCompiled.ExecuteAsync(this, _parameters);
    }

    [MemoryDiagnoser]
    public class ObjectExecutorBenchmark_AsyncExecutor_InvokeAsync_WithoutAwait
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutorBenchmark_AsyncExecutor_InvokeAsync_WithoutAwait);

        private static readonly ObjectMethodExecutor _exec =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _execFastCompiled =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public void ObjExec() => _exec.ExecuteAsync(this, _parameters);

        [Benchmark(Baseline = true)]
        public void ObjExec_CompiledFast() => _execFastCompiled.ExecuteAsync(this, _parameters);
    }
}
