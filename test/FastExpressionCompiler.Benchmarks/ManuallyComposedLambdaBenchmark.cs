using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;

namespace FastExpressionCompiler.Benchmarks
{
    public class ManuallyComposedLambdaBenchmark
    {
        private static Expression<Func<B, X>> ComposeManualExprWithParams()
        {
            var a = new A();
            var bParamExpr = Expression.Parameter(typeof(B), "b");

            var expr = Expression.Lambda<Func<B, X>>(
                Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
                    Expression.Constant(a, typeof(A)), bParamExpr),
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

        private static readonly Expression<Func<B, X>> _expr = ComposeManualExprWithParams();

        [MarkdownExporter, MemoryDiagnoser]
        public class CompileManuallyComposed
        {
            [Benchmark]
            public Func<B, X> CompileExpression()
            {
                return _expr.Compile();
            }

            [Benchmark(Baseline = true)]
            public Func<B, X> CompileFastExpression()
            {
                return ExpressionCompiler.Compile<Func<B, X>>(_expr);
            }
        }

        [MarkdownExporter, MemoryDiagnoser]
        public class InvokeManuallyComposed
        {
            private static readonly Func<B, X> _lambdaCompiled = _expr.Compile();
            private static readonly Func<B, X> _lambdaCompiledFast = ExpressionCompiler.Compile<Func<B, X>>(_expr);

            private static readonly A _aa = new A();
            private static readonly B _bb = new B();
            private static readonly Func<B, X> _lambda = b => new X(_aa, b);

            [Benchmark]
            public X RawLambda()
            {
                return _lambda(_bb);
            }

            [Benchmark]
            public X CompiledLambda()
            {
                return _lambdaCompiled(_bb);
            }

            [Benchmark(Baseline = true)]
            public X FastCompiledLambda()
            {
                return _lambdaCompiledFast(_bb);
            }
        }
    }
}
