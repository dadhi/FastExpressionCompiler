using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor
    {
        public async Task Foo(int a, int b) => await Task.Delay(1);
        public async Task<int> FooInt(int a, int b) => await Task.FromResult(a + b);
        public async Task<string> FooString(int a, int b) => await Task.FromResult((a + b).ToString());

        private const string TestMethodName = nameof(Foo);
        //private const string TestMethodName = nameof(FooInt);
        //private const string TestMethodName = nameof(FooString);

        private static readonly Type _t = typeof(Issue72_Try_CompileFast_for_MS_Extensions_ObjectMethodExecutor);

        private static readonly ObjectMethodExecutor _execCompiled =
            ObjectMethodExecutor.Create(_t.GetMethod(TestMethodName), _t.GetTypeInfo());

        private static readonly ObjectMethodExecutorCompiledFast _execCompiledFast =
            ObjectMethodExecutorCompiledFast.Create(_t.GetMethod(TestMethodName), _t.GetTypeInfo());

        private static readonly object[] _parameters = { 1, 2 };

        [Test] // this is for comparison
        public async Task AsyncExecutor_CompiledNormally_ExecuteAsync_WithAwait()
        {
            await _execCompiled.ExecuteAsync(this, _parameters);
        }

        // Results so far:
        // -  It does not work for any Foo.. method :(
        // - I have also tried to replace all `struct` in ObjectMethodExecutor and helpers with `class` with no success
        //
        [Test][Ignore("FIX ME!")]
        public async Task AsyncExecutor_CompiledFast_ExecuteAsync_WithAwait()
        {
            await _execCompiledFast.ExecuteAsync(this, _parameters);
        }

        [Test] // this works fine without await
        public void AsyncExecutor_CompiledFast_ExecuteAsync_WithoutAwait()
        {
            _execCompiledFast.ExecuteAsync(this, _parameters);
        }
    }
}
