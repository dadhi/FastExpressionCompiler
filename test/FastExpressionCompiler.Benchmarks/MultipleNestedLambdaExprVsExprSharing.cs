using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;


namespace FastExpressionCompiler.Benchmarks
{
/*
BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.307
  [Host]     : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2


|               Method |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
|     CompiledMultiple | 220.240 us | 3.7595 us | 3.5166 us | 219.878 us | 26.64 |    1.11 | 0.9766 |      - |   10.3 KB |        3.10 |
| CompiledFastMultiple |  11.587 us | 0.2309 us | 0.5836 us |  11.375 us |  1.41 |    0.09 | 0.7935 | 0.7782 |   4.95 KB |        1.49 |
|   CompiledFastShared |   8.258 us | 0.1650 us | 0.2845 us |   8.249 us |  1.00 |    0.00 | 0.5341 | 0.5188 |   3.32 KB |        1.00 |

*/
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
