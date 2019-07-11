using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser]
    public class NestedLambdasVsVars
    {
        /*
        ## Initial results:

                    BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.829 (1803/April2018Update/Redstone4)
        Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
        Frequency=2156251 Hz, Resolution=463.7679 ns, Timer=TSC
        .NET Core SDK=3.0.100-preview3-010431
          [Host]     : .NET Core 2.1.11 (CoreCLR 4.6.27617.04, CoreFX 4.6.27617.02), 64bit RyuJIT
          DefaultJob : .NET Core 2.1.11 (CoreCLR 4.6.27617.04, CoreFX 4.6.27617.02), 64bit RyuJIT

                                                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
------------------------------------------------------------------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                                   Expression_with_sub_expressions |  87.36 us | 1.0512 us | 0.9833 us |  1.00 |    0.00 |      4.7607 |      2.3193 |      0.3662 |            22.12 KB |
                           Expression_with_sub_expressions_Compile | 700.20 us | 2.4539 us | 2.1753 us |  8.01 |    0.10 |      5.8594 |      2.9297 |           - |            30.68 KB |
                           Expression_with_sub_expressions_inlined |  90.92 us | 0.4732 us | 0.3951 us |  1.04 |    0.01 |      5.7373 |      2.8076 |      0.4883 |            26.54 KB |
                   Expression_with_sub_expressions_inlined_Compile | 701.86 us | 4.0964 us | 3.8317 us |  8.04 |    0.10 |      6.8359 |      2.9297 |           - |             31.7 KB |
         Expression_with_sub_expressions_assigned_to_vars_in_block |  87.15 us | 0.7257 us | 0.6789 us |  1.00 |    0.01 |      4.8828 |      2.4414 |      0.3662 |             22.7 KB |
 Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 718.69 us | 3.9077 us | 3.4640 us |  8.22 |    0.10 |      6.8359 |      2.9297 |           - |            32.18 KB |

        */
        private Expression<Func<A>> _expr, _exprInlined, _exprWithVars;

        [GlobalSetup]
        public void Init()
        {
            _expr = CreateExpression();
            _exprInlined = CreateExpressionInlined();
            _exprWithVars = CreateExpressionWithVars();
        }

        [Benchmark(Baseline = true)]
        public Func<A> Expression_with_sub_expressions()
        {
            return _expr.CompileFast(true);
        }

        [Benchmark]
        public Func<A> Expression_with_sub_expressions_Compile()
        {
            return _expr.Compile();
        }

        [Benchmark]
        public Func<A> Expression_with_sub_expressions_inlined()
        {
            return _exprInlined.CompileFast(true);
        }

        [Benchmark]
        public Func<A> Expression_with_sub_expressions_inlined_Compile()
        {
            return _exprInlined.Compile();
        }

        [Benchmark]
        public Func<A> Expression_with_sub_expressions_assigned_to_vars_in_block()
        {
            return _exprWithVars.CompileFast(true);
        }

        [Benchmark]
        public Func<A> Expression_with_sub_expressions_assigned_to_vars_in_block_Compile()
        {
            return _exprWithVars.Compile();
        }

        public readonly object[] _objects = new object[3];
        public object GetOrAdd(int i, Func<object> getValue) =>
            _objects[i] ?? (_objects[i] = getValue());

        private Expression<Func<A>> CreateExpression()
        {
            var test = Constant(new NestedLambdasVsVars());

            var d = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(2),
                    Lambda(
                        New(typeof(D).GetConstructors()[0], new Expression[0]))),
                typeof(D));

            var c = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(1),
                    Lambda(
                        New(typeof(C).GetConstructors()[0], d))),
                typeof(C));

            var b = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(0),
                    Lambda(
                        New(typeof(B).GetConstructors()[0], c, d))),
                typeof(B));

            var fe = Lambda<Func<A>>(
                New(typeof(A).GetConstructors()[0], b, c, d));

            return fe;
        }

        private Expression<Func<A>> CreateExpressionInlined()
        {
            var test = Constant(new NestedLambdasVsVars());

            var fe = Lambda<Func<A>>(
                New(typeof(A).GetConstructors()[0], 
                    Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(0),
                            Lambda(
                                New(typeof(B).GetConstructors()[0], 
                                    Convert(
                                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                                            Constant(1),
                                            Lambda(
                                                New(typeof(C).GetConstructors()[0], Convert(
                                                    Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                                                        Constant(2),
                                                        Lambda(
                                                            New(typeof(D).GetConstructors()[0], new Expression[0]))),
                                                    typeof(D))))),
                                        typeof(C)), 
                                    Convert(
                                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                                            Constant(2),
                                            Lambda(
                                                New(typeof(D).GetConstructors()[0], new Expression[0]))),
                                        typeof(D))))),
                        typeof(B)), 
                    Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(1),
                            Lambda(
                                New(typeof(C).GetConstructors()[0], Convert(
                                    Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                                        Constant(2),
                                        Lambda(
                                            New(typeof(D).GetConstructors()[0], new Expression[0]))),
                                    typeof(D))))),
                        typeof(C)), 
                    Convert(
                    Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                        Constant(2),
                        Lambda(
                            New(typeof(D).GetConstructors()[0], new Expression[0]))),
                    typeof(D))));

            return fe;
        }

        private Expression<Func<A>> CreateExpressionWithVars()
        {
            var test = Constant(new NestedLambdasVsVars());

            var d = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(2),
                    Lambda(
                        New(typeof(D).GetConstructors()[0], new Expression[0]))),
                typeof(D));

            var c = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(1),
                    Lambda(
                        New(typeof(C).GetConstructors()[0], d))),
                typeof(C));

            var b = Convert(
                Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                    Constant(0),
                    Lambda(
                        New(typeof(B).GetConstructors()[0], c, d))),
                typeof(B));

            var dVar = Parameter(typeof(D), "d");
            var cVar = Parameter(typeof(C), "c");
            var bVar = Parameter(typeof(B), "b");

            var fe = Lambda<Func<A>>(
                Block(typeof(A),
                    new[] { bVar, cVar, dVar },
                    Assign(dVar, d),
                    Assign(cVar, c),
                    Assign(bVar, b),
                    New(typeof(A).GetConstructors()[0], bVar, cVar, dVar))
                );

            return fe;
        }

        public class A 
        {
            public B B { get; }
            public C C { get; }
            public D D { get; }

            public A(B b, C c, D d)
            {
                B = b;
                C = c;
                D = d;
            }
        }

        public class B
        {
            public C C { get; }
            public D D { get; }

            public B(C c, D d)
            {
                C = c;
                D = d;
            }
        }

        public class C
        {
            public D D { get; }

            public C(D d)
            {
                D = d;
            }
        }

        public class D
        {
        }
    }
}
