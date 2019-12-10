using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class NestedLambdaOverhead
    {
        private static readonly Expression<Func<object>> _withoutNestedLambda = () =>
            new A(new B());

        private static readonly Expression<Func<object>> _withNestedLambda = () =>
            new A(() => new B());

        [Benchmark(Baseline = true)]
        public object Without_nested_lambda() => _withoutNestedLambda.CompileFast(true);

        [Benchmark]
        public object With_nested_lambda() => _withNestedLambda.CompileFast(true);

        class A
        {
            public readonly B B;

            public A(B b) { B = b; }
            public A(Func<B> func) { B = func(); }
        }

        class B { }
    }
}
