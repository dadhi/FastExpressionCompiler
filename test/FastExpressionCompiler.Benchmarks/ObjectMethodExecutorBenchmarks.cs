using System;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Internal;

namespace FastExpressionCompiler.Benchmarks
{
    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    public class ObjectExecutor_SyncMethod_Compile
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutor_SyncMethod_Compile);

        [Benchmark]
        public object Compile() =>
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object CompileFast() =>
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    public class ObjectExecutor_SyncMethod_Execute
    {
        public string Foo(int a, int b) => (a + b).ToString();

        private static readonly Type _t = typeof(ObjectExecutor_SyncMethod_Execute);

        private static readonly ObjectMethodExecutor _compiled =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _compiledFast =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public object Compiled() => _compiled.Execute(this, _parameters);

        [Benchmark(Baseline = true)]
        public object CompiledFast() => _compiledFast.Execute(this, _parameters);
    }

    [ClrJob, CoreJob]
    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_Compile
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_Compile);

        [Benchmark]
        public object Compile() =>
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark]
        public object CompileFast() =>
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        [Benchmark(Baseline = true)]
        public object CompileFastWithPreCreatedClosure() =>
            ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
    }

    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    public class ObjectExecutor_AsyncMethod_Execute
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_Execute);

        private static readonly ObjectMethodExecutor _compiled =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _compiledFast =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public object Compiled() => ((Task<string>)_compiled.Execute(this, _parameters)).Result;

        [Benchmark(Baseline = true)]
        public object CompiledFast() => ((Task<string>)_compiledFast.Execute(this, _parameters)).Result;
    }

    [CoreJob, ClrJob]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class ObjectExecutor_AsyncMethod_ExecuteAsync
    {
        public async Task<string> Foo(int a, int b) => await Task.FromResult((a + b).ToString());

        private static readonly Type _t = typeof(ObjectExecutor_AsyncMethod_ExecuteAsync);

        private static readonly ObjectMethodExecutor _compiled =
            ObjectMethodExecutor.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _compiledFast =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());
        
        private static readonly ObjectMethodExecutorCompiledFastClosure _compiledFastWithPrecreatedClosure =
            ObjectMethodExecutorCompiledFastClosure.Create(_t.GetMethod(nameof(Foo)), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Benchmark]
        public async Task Compiled() => await _compiled.ExecuteAsync(this, _parameters);

        [Benchmark]
        public async Task CompiledFast() => await _compiledFast.ExecuteAsync(this, _parameters);

        [Benchmark(Baseline = true)]
        public async Task CompiledFastWithPreCreatedClosure() => await _compiledFastWithPrecreatedClosure.ExecuteAsync(this, _parameters);
    }
}
