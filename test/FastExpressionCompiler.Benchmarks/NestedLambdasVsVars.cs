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
## The results

BenchmarkDotNet=v0.11.5, OS=Windows 10.0.17134.885 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156248 Hz, Resolution=463.7685 ns, Timer=TSC
.NET Core SDK=3.0.100-preview3-010431
  [Host]     : .NET Core 2.1.12 (CoreCLR 4.6.27817.01, CoreFX 4.6.27818.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.12 (CoreCLR 4.6.27817.01, CoreFX 4.6.27818.01), 64bit RyuJIT

### Compilation

|                                                                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                           Expression_with_sub_expressions_CompiledFast |  78.27 us | 0.3404 us | 0.3184 us |  1.00 |    0.00 | 4.3945 | 2.1973 | 0.2441 |  20.42 KB |
|                               Expression_with_sub_expressions_Compiled | 640.89 us | 4.6905 us | 4.3875 us |  8.19 |    0.08 | 5.8594 | 2.9297 |      - |  27.04 KB |
| Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |  46.36 us | 0.3881 us | 0.3441 us |  0.59 |    0.01 | 2.7466 | 1.3428 | 0.1831 |  12.61 KB |
|      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 864.08 us | 2.0120 us | 1.8820 us | 11.04 |    0.06 | 3.9063 | 1.9531 |      - |  20.96 KB |

#### After fixing the nested lambdas compilation and ArrayClosure only

|                                                                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                           Expression_with_sub_expressions_CompiledFast |  45.86 us | 0.3216 us | 0.3008 us |  1.00 |    0.00 | 3.0518 | 1.5259 | 0.3052 |  13.95 KB |
|                               Expression_with_sub_expressions_Compiled | 645.33 us | 5.1962 us | 4.6063 us | 14.07 |    0.15 | 5.8594 | 2.9297 |      - |  27.04 KB |
| Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |  36.79 us | 0.3446 us | 0.3223 us |  0.80 |    0.01 | 2.9297 | 1.4648 | 0.1831 |  13.48 KB |
|      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 870.74 us | 4.7653 us | 4.4574 us | 18.99 |    0.17 | 3.9063 | 1.9531 |      - |  20.96 KB |

### Making the same nested lambdas to be compiled Once and the split ArrayClosure

|                                       Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
| Expression_with_sub_expressions_CompiledFast |  34.11 us | 0.1718 us | 0.1435 us |  1.00 |    0.00 | 2.1973 | 1.0986 | 0.1831 |  10.22 KB |
|     Expression_with_sub_expressions_Compiled | 643.10 us | 6.6755 us | 6.2443 us | 18.86 |    0.23 | 5.8594 | 2.9297 |      - |  27.04 KB |

#### Removing cast-class and loading constants as variables - PR by @Havunen

|                                       Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
| Expression_with_sub_expressions_CompiledFast |  32.13 us | 0.2413 us | 0.2257 us |  1.00 |    0.00 | 2.0752 | 1.0376 | 0.1831 |   9.53 KB |
|     Expression_with_sub_expressions_Compiled | 627.41 us | 4.8732 us | 4.5584 us | 19.53 |    0.18 | 5.8594 | 2.9297 |      - |  27.04 KB |

#### Optimizing the use of closure variables and cleaning not necessary nested Lambda type creation

|                                       Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
| Expression_with_sub_expressions_CompiledFast |  29.11 us | 0.1346 us | 0.1193 us |  1.00 |    0.00 | 1.9531 | 0.9766 | 0.1831 |   8.95 KB |
|     Expression_with_sub_expressions_Compiled | 633.11 us | 4.1526 us | 3.8843 us | 21.75 |    0.14 | 5.8594 | 2.9297 |      - |  27.04 KB |


### Invocation

|                                                                 Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|                           Expression_with_sub_expressions_CompiledFast |    57.17 ns | 0.1766 ns | 0.1566 ns |  1.00 |    0.00 | 0.0627 |     - |     - |     296 B |
|                               Expression_with_sub_expressions_Compiled | 1,083.94 ns | 2.6288 ns | 2.4590 ns | 18.96 |    0.07 | 0.0458 |     - |     - |     224 B |
| Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |    51.78 ns | 0.2234 ns | 0.2089 ns |  0.91 |    0.00 | 0.0593 |     - |     - |     280 B |
|      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 1,644.84 ns | 5.2784 ns | 4.4077 ns | 28.77 |    0.10 | 0.0782 |     - |     - |     376 B |

#### After fixing the nested lambdas and making them compile once and the split ArrayClosure

|                                       Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
| Expression_with_sub_expressions_CompiledFast |    13.58 ns | 0.0945 ns | 0.0838 ns |  1.00 |    0.00 | 0.0068 |     - |     - |      32 B |
|     Expression_with_sub_expressions_Compiled | 1,122.42 ns | 3.2589 ns | 2.8889 ns | 82.63 |    0.54 | 0.0458 |     - |     - |     224 B |

#### Removing cast-class and loading constants as variables - PR by @Havunen

|                                       Method |        Mean |     Error |    StdDev |  Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------------------- |------------:|----------:|----------:|-------:|--------:|-------:|------:|------:|----------:|
| Expression_with_sub_expressions_CompiledFast |    11.89 ns | 0.1250 ns | 0.1044 ns |   1.00 |    0.00 | 0.0068 |     - |     - |      32 B |
|     Expression_with_sub_expressions_Compiled | 1,261.20 ns | 2.1887 ns | 2.0473 ns | 106.04 |    0.99 | 0.0458 |     - |     - |     224 B |
*/
        private Expression<Func<A>> _expr, _exprWithVars;

        private Func<A> _exprCompiledFast, _exprCompiled, _exprWithVarsCompiledFast, _exprWithVarsCompiled;

        [GlobalSetup]
        public void Init()
        {
            _expr         = CreateExpression();
            _exprWithVars = CreateExpressionWithVars();

            _exprCompiledFast = _expr.CompileFast(true);
            _exprCompiled = _expr.Compile();

            _exprWithVarsCompiledFast = _exprWithVars.CompileFast(true);
            _exprWithVarsCompiled = _exprWithVars.Compile();
        }

        [Benchmark(Baseline = true)]
        public object Expression_with_sub_expressions_CompiledFast()
        {
            //return _expr.CompileFast(true);
            return _exprCompiledFast();
            //return _expr.CompileFast(true).Invoke();
        }

        [Benchmark]
        public object Expression_with_sub_expressions_Compiled()
        {
            //return _expr.Compile();
            return _exprCompiled();
            //return _expr.Compile().Invoke();
        }

        //[Benchmark]
        public object Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast()
        {
            //return _exprWithVars.CompileFast(true);
            return _exprWithVarsCompiledFast();
            //return _exprWithVars.CompileFast(true).Invoke();
        }

        //[Benchmark]
        public object Expression_with_sub_expressions_assigned_to_vars_in_block_Compile()
        {
            //return _exprWithVars.Compile();
            return _exprWithVarsCompiled();
            //return _exprWithVars.Compile().Invoke();
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
                New(typeof(A).GetConstructors()[0], b, c));

            return fe;
        }

        private Expression<Func<A>> CreateExpressionWithVars()
        {
            var test = Constant(new NestedLambdasVsVars());

            var dVar = Parameter(typeof(D), "d");
            var cVar = Parameter(typeof(C), "c");
            var bVar = Parameter(typeof(B), "b");

            var fe = Lambda<Func<A>>(
                Block(typeof(A),
                    new[] { bVar, cVar, dVar },
                    Assign(dVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(2),
                            Lambda(
                                New(typeof(D).GetConstructors()[0], new Expression[0]))),
                        typeof(D))), 
                    Assign(cVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(1),
                            Lambda(
                                New(typeof(C).GetConstructors()[0], dVar))),
                        typeof(C))), 
                    Assign(bVar, Convert(
                        Call(test, test.Type.GetMethod(nameof(GetOrAdd)),
                            Constant(0),
                            Lambda(
                                New(typeof(B).GetConstructors()[0], cVar, dVar))),
                        typeof(B))),
                    New(typeof(A).GetConstructors()[0], bVar, cVar))
                );

            return fe;
        }

        public class A 
        {
            public B B { get; }
            public C C { get; }

            public A(B b, C c)
            {
                B = b;
                C = c;
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
