using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace FastExpressionCompiler.Benchmarks
{
    public class HoistedLambdaBenchmark
    {
        private static Expression<Func<X>> GetHoistedExpr()
        {
            var a = new A();
            var b = new B();
            Expression<Func<X>> e = () => new X(a, b);
            return e;
        }

        private static readonly Expression<Func<X>> _hoistedExpr = GetHoistedExpr();

        [MemoryDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Compilation
        {
            /*
            ## 26.01.2019: V2

                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             CompileFast |   7.996 us | 0.0638 us | 0.0565 us |  1.00 |    0.00 |      0.4883 |      0.2441 |      0.0305 |             2.26 KB |
                 Compile | 242.974 us | 1.4929 us | 1.3964 us | 30.39 |    0.26 |      0.7324 |      0.2441 |           - |             4.45 KB |
             */

            [Benchmark]
            public object Compile() => _hoistedExpr.Compile();

            [Benchmark(Baseline = true)]
            public object CompileFast() => _hoistedExpr.CompileFast();
        }

        [MemoryDiagnoser]
        [Orderer(SummaryOrderPolicy.FastestToSlowest)]
        public class Invocation
        {
            /*
            ## 26.01.2019: V2

                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            ---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
             DirectConstructorCall |  6.203 ns | 0.1898 ns | 0.3470 ns |  0.76 |    0.06 |      0.0068 |           - |           - |                32 B |
                FastCompiledLambda |  7.840 ns | 0.2010 ns | 0.1881 ns |  1.00 |    0.00 |      0.0068 |           - |           - |                32 B |
                    CompiledLambda | 12.313 ns | 0.1124 ns | 0.1052 ns |  1.57 |    0.04 |      0.0068 |           - |           - |                32 B |

             */

            private static readonly Func<X> _lambdaCompiled = _hoistedExpr.Compile();
            private static readonly Func<X> _lambdaCompiledFast = _hoistedExpr.CompileFast();

            private readonly A _aa = new A();
            private readonly B _bb = new B();

            [Benchmark]
            public object DirectConstructorCall() => 
                new X(_aa, _bb);

            [Benchmark]
            public object CompiledLambda() => 
                _lambdaCompiled();

            [Benchmark(Baseline = true)]
            public object FastCompiledLambda() => 
                _lambdaCompiledFast();
        }

        #region SUT

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

        #endregion
    }
}
