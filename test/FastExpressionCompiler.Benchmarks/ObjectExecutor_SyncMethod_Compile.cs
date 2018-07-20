using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Internal;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class ObjectExecutor_SyncMethod_Compile
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutor_SyncMethod_Compile);

        [Benchmark]
        public object ObjExec() =>
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() =>
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_Compile
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_Compile);

        [Benchmark]
        public object ObjExec() =>
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() =>
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_Execute
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_Execute);

        private static readonly ObjectMethodExecutor _executor =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _executorFastCompiled =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public object ObjExec() => ((Task<string>)_executor.Execute(this, _parameters)).Result;

        [Benchmark(Baseline = true)]
        public object ObjExec_CompiledFast() => ((Task<string>)_executorFastCompiled.Execute(this, _parameters)).Result;
    }

    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_ExecuteAsync
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_ExecuteAsync);

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
}
