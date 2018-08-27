using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;


namespace FastExpressionCompiler.Benchmarks
{
    //[NUnit.Framework.TestFixture]
    [MemoryDiagnoser]
    public class MultipleNestedLambdaExprVsExprSharing
    {
        public static Expression<Func<A>> CreateMultipleXExpr()
        {
            return Lambda<Func<A>>(
                New(
                    typeof(A).GetTypeInfo().GetConstructors().First(),
                    Lambda<Func<X>>(
                        New(typeof(X).GetTypeInfo().GetConstructors().First(), 
                            New(typeof(Y).GetTypeInfo().GetConstructors().First()))),
                    New(typeof(B).GetTypeInfo().GetConstructors().First(),
                        Lambda<Func<X>>(
                            New(typeof(X).GetTypeInfo().GetConstructors().First(),
                                New(typeof(Y).GetTypeInfo().GetConstructors().First())))
                )
            ));
        }

        public static Expression<Func<A>> CreateSharedXExpr()
        {
            var xExpr = Lambda<Func<X>>(New(typeof(X).GetTypeInfo().GetConstructors().First(),
                New(typeof(Y).GetTypeInfo().GetConstructors().First())));
            return Lambda<Func<A>>(
                New(
                    typeof(A).GetTypeInfo().GetConstructors().First(),
                    xExpr,
                    New(typeof(B).GetTypeInfo().GetConstructors().First(),
                        xExpr
                    )
                ));
        }

        public static readonly Expression<Func<A>> MultipleXExpr = CreateMultipleXExpr();
        public static readonly Expression<Func<A>> SharedXExpr = CreateSharedXExpr();

        [Benchmark]
        public Func<A> CompiledMultiple() => MultipleXExpr.Compile();

        //[NUnit.Framework.Test]
        [Benchmark]
        public Func<A> CompiledFastMultiple() => MultipleXExpr.CompileFast();

        //[NUnit.Framework.Test]
        [Benchmark(Baseline = true)]
        public Func<A> CompiledFastShared() => SharedXExpr.CompileFast();

        public class A
        {
            public Func<X> Fx { get; }
            public B B { get; }
            public A(Func<X> fx, B b)
            {
                Fx = fx;
                B = b;
            }
        }

        public class B
        {
            public Func<X> Fx { get; }
            public B(Func<X> fx)
            {
                Fx = fx;
            }
        }

        public class X
        {
            public Y Y { get; }
            public X(Y y)
            {
                Y = y;
            }
        }

        public class Y { }
    }

}
