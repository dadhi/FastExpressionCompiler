using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace FastExpressionCompiler.Benchmarks
{
    public class ManuallyComposedLambdaBenchmark
    {
        private static Expression<Func<B, X>> ComposeManualExprWithParams(Expression aConstExpr)
        {
            var bParamExpr = Expression.Parameter(typeof(B), "b");

            var expr = Expression.Lambda<Func<B, X>>(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(), aConstExpr, bParamExpr),
                bParamExpr);

            return expr;
        }

        public class A { }
        public class B { }

        public class X
        {
            public A A { get; }
            public B B { get; }

            public X(A a, B b)
            {
                A = a;
                B = b;
            }
        }

        private static readonly A _a = new A();
        private static readonly ConstantExpression _aConstExpr = Expression.Constant(_a, typeof(A));
        private static readonly Expression<Func<B, X>> _expr = ComposeManualExprWithParams(_aConstExpr);

        [MemoryDiagnoser]
        [ClrJob, CoreJob]
        [MarkdownExporter]
        public class Compilation
        {
            [Benchmark]
            public Func<B, X> Compile() => 
                _expr.Compile();

            [Benchmark]
            public Func<B, X> CompileFast() => 
                _expr.CompileFast();

            [Benchmark(Baseline = true)]
            public Func<B, X> CompileFastWithPreCreatedClosure() => 
                _expr.TryCompileWithPreCreatedClosure<Func<B, X>>(ExpressionCompiler.Closure.Create(_a), _aConstExpr)
                ?? _expr.Compile();
        }

        [MemoryDiagnoser]
        [ClrJob, CoreJob]
        [MarkdownExporter]
        public class Invocation
        {
            private static readonly Func<B, X> _lambdaCompiled = _expr.Compile();
            private static readonly Func<B, X> _lambdaCompiledFast = _expr.CompileFast();
            private static readonly Func<B, X> _lambdaCompiledFastWithClosure =
                _expr.TryCompileWithPreCreatedClosure<Func<B, X>>(ExpressionCompiler.Closure.Create(_a), _aConstExpr);

            private static readonly A _aa = new A();
            private static readonly B _bb = new B();
            private static readonly Func<B, X> _lambda = b => new X(_aa, b);

            [Benchmark]
            public X DirectLambdaCall() => _lambda(_bb);

            [Benchmark]
            public X CompiledLambda() => _lambdaCompiled(_bb);

            [Benchmark]
            public X FastCompiledLambda() => _lambdaCompiledFast(_bb);

            [Benchmark(Baseline = true)]
            public X FastCompiledLambdaWithPreCreatedClosure() => _lambdaCompiledFastWithClosure(_bb);
        }
    }
}
