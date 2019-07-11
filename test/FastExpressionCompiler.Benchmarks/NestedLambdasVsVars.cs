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

### Compilation

                                                                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                           Expression_with_sub_expressions_CompiledFast |  86.36 us | 0.5609 us | 0.4972 us |  1.00 |    0.00 |      4.7607 |      2.3193 |      0.3662 |            21.95 KB |
                               Expression_with_sub_expressions_Compiled | 701.88 us | 2.5207 us | 2.2346 us |  8.13 |    0.06 |      5.8594 |      2.9297 |           - |            30.68 KB |
 Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |  89.18 us | 0.5426 us | 0.4810 us |  1.03 |    0.01 |      4.8828 |      2.4414 |      0.3662 |            22.36 KB |
      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 719.31 us | 4.1025 us | 3.8375 us |  8.33 |    0.07 |      6.8359 |      2.9297 |           - |            32.18 KB |

### Invocation

                                                                 Method |        Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------------------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                           Expression_with_sub_expressions_CompiledFast |    52.66 ns | 0.2863 ns | 0.2538 ns |  1.00 |    0.00 |      0.0644 |           - |           - |               304 B |
                               Expression_with_sub_expressions_Compiled | 1,654.81 ns | 5.4867 ns | 5.1322 ns | 31.43 |    0.15 |      0.0610 |           - |           - |               296 B |
 Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |    52.63 ns | 0.1644 ns | 0.1537 ns |  1.00 |    0.01 |      0.0644 |           - |           - |               304 B |
      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 1,650.25 ns | 5.8987 ns | 5.2290 ns | 31.34 |    0.17 |      0.0610 |           - |           - |               296 B |

### Compilation + Invocation

                                                                 Method |       Mean |    Error |   StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------------------------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                           Expression_with_sub_expressions_CompiledFast |   486.2 us | 1.939 us | 1.814 us |  1.00 |    0.00 |      4.3945 |      1.9531 |           - |            22.24 KB |
                               Expression_with_sub_expressions_Compiled | 1,538.7 us | 5.844 us | 5.466 us |  3.16 |    0.02 |      5.8594 |      1.9531 |           - |            31.81 KB |
 Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |   506.2 us | 3.517 us | 3.290 us |  1.04 |    0.01 |      4.8828 |      1.9531 |           - |            22.66 KB |
      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 1,548.7 us | 8.320 us | 7.783 us |  3.19 |    0.02 |      5.8594 |      1.9531 |           - |            33.31 KB |


        */
        private Expression<Func<A>> _expr, _exprInlined, _exprWithVars;

        private Func<A> _exprCompiledFast, _exprCompiled, _exprWithVarsCompiledFast, _exprWithVarsCompiled;

        [GlobalSetup]
        public void Init()
        {
            _expr         = CreateExpression();
            _exprWithVars = CreateExpressionWithVars();
            _exprInlined = CreateExpressionInlined();

            _exprCompiledFast = _expr.CompileFast(true);
            _exprCompiled     = _expr.Compile();

            _exprWithVarsCompiledFast = _exprWithVars.CompileFast(true);
            _exprWithVarsCompiled     = _exprWithVars.Compile();
        }

        [Benchmark(Baseline = true)]
        public object Expression_with_sub_expressions_CompiledFast()
        {
            return _expr.CompileFast(true);
            //return _exprCompiledFast();
            //return _expr.CompileFast(true).Invoke();
        }

        [Benchmark]
        public object Expression_with_sub_expressions_Compiled()
        {
            return _expr.Compile();
            //return _expr.Compile().Invoke();
            //return _exprCompiled();
        }

        [Benchmark]
        public object Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast()
        {
            return _exprWithVars.CompileFast(true);
            //return _exprWithVarsCompiledFast();
            //return _exprWithVars.CompileFast(true).Invoke();
        }

        [Benchmark]
        public object Expression_with_sub_expressions_assigned_to_vars_in_block_Compile()
        {
            return _exprWithVars.Compile();
            //return _exprWithVarsCompiled();
            //return _exprWithVars.Compile().Invoke();
        }

        //[Benchmark]
        public Func<A> Expression_with_sub_expressions_inlined()
        {
            return _exprInlined.CompileFast(true);
        }

        //[Benchmark]
        public Func<A> Expression_with_sub_expressions_inlined_Compile()
        {
            return _exprInlined.Compile();
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
