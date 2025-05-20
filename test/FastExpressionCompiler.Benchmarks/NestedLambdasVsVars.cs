using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using static System.Linq.Expressions.Expression;
using L = FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.Benchmarks
{
    [MemoryDiagnoser, RankColumn, Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    // [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
    public class NestedLambdasVsVars
    {
        /*
## Compilation

### V2

|                                                                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                           Expression_with_sub_expressions_CompiledFast |  78.27 us | 0.3404 us | 0.3184 us |  1.00 |    0.00 | 4.3945 | 2.1973 | 0.2441 |  20.42 KB |
|                               Expression_with_sub_expressions_Compiled | 640.89 us | 4.6905 us | 4.3875 us |  8.19 |    0.08 | 5.8594 | 2.9297 |      - |  27.04 KB |
| Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |  46.36 us | 0.3881 us | 0.3441 us |  0.59 |    0.01 | 2.7466 | 1.3428 | 0.1831 |  12.61 KB |
|      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 864.08 us | 2.0120 us | 1.8820 us | 11.04 |    0.06 | 3.9063 | 1.9531 |      - |  20.96 KB |

### V3

|                                            Method |      Mean |     Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|-------------------------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
| LightExpression_with_sub_expressions_CompiledFast |  32.37 us |  0.442 us | 0.413 us |  1.00 |    0.00 | 2.1973 | 1.0986 | 0.1831 |   9.03 KB |
|          Expression_with_sub_expressions_Compiled | 637.97 us | 12.327 us | 9.624 us | 19.71 |    0.37 | 5.8594 | 2.9297 |      - |  26.31 KB |

## Invocation

### V2

|                                                                 Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|                           Expression_with_sub_expressions_CompiledFast |    57.17 ns | 0.1766 ns | 0.1566 ns |  1.00 |    0.00 | 0.0627 |     - |     - |     296 B |
|                               Expression_with_sub_expressions_Compiled | 1,083.94 ns | 2.6288 ns | 2.4590 ns | 18.96 |    0.07 | 0.0458 |     - |     - |     224 B |
| Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast |    51.78 ns | 0.2234 ns | 0.2089 ns |  0.91 |    0.00 | 0.0593 |     - |     - |     280 B |
|      Expression_with_sub_expressions_assigned_to_vars_in_block_Compile | 1,644.84 ns | 5.2784 ns | 4.4077 ns | 28.77 |    0.10 | 0.0782 |     - |     - |     376 B |

### V3

|                                            Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
| LightExpression_with_sub_expressions_CompiledFast |    13.40 ns |  0.190 ns |  0.158 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      32 B |
|          Expression_with_sub_expressions_Compiled | 1,083.09 ns | 21.502 ns | 30.142 ns | 80.91 |    3.16 | 0.0534 |     - |     - |     224 B |

## Create+Compile

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT

### V2
|                                            Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|-------------------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
| LightExpression_with_sub_expressions_CompiledFast |  26.36 us | 0.113 us | 0.100 us |  1.00 |    0.00 | 2.0752 | 1.0376 | 0.1831 |   9.66 KB |
|      Expression_with_sub_expressions_CompiledFast |  30.99 us | 0.156 us | 0.146 us |  1.18 |    0.01 | 2.1973 | 1.0986 | 0.1831 |  10.22 KB |
|          Expression_with_sub_expressions_Compiled | 563.10 us | 1.141 us | 0.953 us | 21.38 |    0.07 | 5.8594 | 2.9297 |      - |  27.52 KB |

### V3
|                                            Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|-------------------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
| LightExpression_with_sub_expressions_CompiledFast |  37.16 us | 0.720 us | 0.829 us |  1.00 |    0.00 | 2.3804 | 1.1597 | 0.1831 |   9.81 KB |
|          Expression_with_sub_expressions_Compiled | 652.33 us | 7.463 us | 6.616 us | 17.62 |    0.46 | 5.8594 | 2.9297 |      - |  27.51 KB |

### V3.4.0

BenchmarkDotNet v0.13.7, Windows 11 (10.0.22621.1992/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 7.0.307
  [Host]     : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.10 (7.0.1023.36312), X64 RyuJIT AVX2

|                                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | CacheMisses/Op | BranchInstructions/Op | BranchMispredictions/Op |   Gen1 |   Gen2 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|---------------:|----------------------:|------------------------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  40.48 us |  1.821 us |  5.137 us |  1.00 |    0.00 | 1.7090 |            640 |                46,372 |                     454 | 1.4648 | 0.1221 |  10.67 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 793.63 us | 15.595 us | 23.815 us | 19.97 |    2.28 | 3.9063 |          1,855 |               894,256 |                  30,865 | 1.9531 |      - |  28.47 KB |        2.67 |

#### SmallList2 ConstantUsage thingy

|                                            Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  22.57 us |  0.442 us |  0.862 us |  22.26 us |  1.00 |    0.00 | 1.7090 | 1.6785 |  10.52 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 517.53 us | 10.254 us | 27.546 us | 503.49 us | 22.95 |    1.36 | 3.9063 | 2.9297 |  28.91 KB |        2.75 |

### Fix double compilation of nested lambdas

|                                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 |   Gen2 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  21.32 us |  0.387 us |  0.717 us |  1.00 |    0.00 | 1.4648 | 1.4038 | 0.0305 |      9 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 571.67 us | 11.221 us | 12.922 us | 26.75 |    1.11 | 3.9063 | 2.9297 |      - |  28.47 KB |        3.16 |

|                                            Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  18.82 us |  0.373 us |  0.894 us |  18.53 us |  1.00 |    0.00 | 1.4648 | 1.3428 |   9.02 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 508.95 us | 10.150 us | 24.320 us | 498.55 us | 27.09 |    1.65 | 3.9063 | 2.9297 |  28.47 KB |        3.15 |

### Removed ClosureInfo from the LambdaInfo

|                                            Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  20.96 us |  0.417 us |  1.098 us |  20.67 us |  1.00 |    0.00 | 1.2817 | 1.2512 |   7.99 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 584.61 us | 25.681 us | 71.163 us | 554.55 us | 28.18 |    4.04 | 3.9063 | 2.9297 |  28.47 KB |        3.56 |

### Optimize the convert from object to avoid calling GetMethods

|                                            Method |      Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  19.31 us |  0.321 us |  0.343 us |  1.00 |    0.00 | 1.1902 | 1.1292 |   7.32 KB |        1.00 |
|          Expression_with_sub_expressions_Compiled | 602.67 us | 22.702 us | 65.502 us | 30.55 |    3.29 | 3.9063 | 2.9297 |   28.7 KB |        3.92 |

## v5.3.0 Pooled ILGenerator - really?

| Method                                            | Mean      | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------------------- |----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| LightExpression_with_sub_expressions_CompiledFast |  17.58 us | 0.342 us | 0.522 us |  1.00 |    0.04 |    1 | 1.1597 | 1.0986 |   7.17 KB |        1.00 |
| Expression_with_sub_expressions_Compiled          | 518.95 us | 8.148 us | 7.223 us | 29.54 |    0.94 |    2 | 3.9063 | 2.9297 |  28.25 KB |        3.94 |

*/
        private Expression<Func<A>> _expr;//, _exprWithVars;
        private LightExpression.Expression<Func<A>> _lightExpr;

        private Func<A> _exprCompiled, _exprCompiledFast, _lightExprCompiledFast;

        [GlobalSetup]
        public void Init()
        {
            _expr = CreateExpression();
            _lightExpr = CreateLightExpression();

            _exprCompiled = _expr.Compile();
            _exprCompiledFast = _expr.CompileFast(true);
            _lightExprCompiledFast = LightExpression.ExpressionCompiler.CompileFast(_lightExpr, true);
        }

        [Benchmark(Baseline = true)]
        public object LightExpression_with_sub_expressions_CompiledFast()
        {
            //return CreateLightExpression();
            return LightExpression.ExpressionCompiler.CompileFast(CreateLightExpression(), true);
            // return LightExpression.ExpressionCompiler.CompileFast(_lightExpr, true);
            // return _lightExprCompiledFast();
        }

        [Benchmark]
        public object Expression_with_sub_expressions_Compiled()
        {
            return CreateExpression().Compile();
            // return _expr.Compile();
            // return _exprCompiled();
        }

        //[Benchmark]
        //[Benchmark(Baseline = true)]
        //public object Expression_with_sub_expressions_CompiledFast()
        //{
        //    //return CreateExpression();
        //    //return CreateExpression().CompileFast(true);
        //return _expr.CompileFast(true);
        //    return _exprCompiledFast();
        //}

        ////[Benchmark]
        //public object Expression_with_sub_expressions_assigned_to_vars_in_block_CompiledFast()
        //{
        //    //return _exprWithVars.CompileFast(true);
        //    return _exprWithVarsCompiledFast();
        //    //return _exprWithVars.CompileFast(true).Invoke();
        //}

        ////[Benchmark]
        //public object Expression_with_sub_expressions_assigned_to_vars_in_block_Compile()
        //{
        //    //return _exprWithVars.Compile();
        //    return _exprWithVarsCompiled();
        //    //return _exprWithVars.Compile().Invoke();
        //}

        public readonly object[] _objects = new object[3];
        private static readonly ConstructorInfo _aCtor = typeof(A).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _bCtor = typeof(B).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _cCtor = typeof(C).GetTypeInfo().DeclaredConstructors.First();
        private static readonly ConstructorInfo _dCtor = typeof(D).GetTypeInfo().DeclaredConstructors.First();

        public object GetOrAdd(int i, Func<object> getValue) =>
            _objects[i] ?? (_objects[i] = getValue());

        private Expression<Func<A>> CreateExpression()
        {
            var test = Constant(new NestedLambdasVsVars());
            var getOrAddMethod = test.Type.GetMethod(nameof(GetOrAdd));
            var d = Convert(
                Call(test, getOrAddMethod,
                    Constant(2),
                    Lambda<Func<object>>(New(_dCtor))),
                typeof(D));

            var c = Convert(
                Call(test, getOrAddMethod,
                    Constant(1),
                    Lambda<Func<object>>(New(_cCtor, d))),
                typeof(C));

            var b = Convert(
                Call(test, getOrAddMethod,
                    Constant(0),
                    Lambda<Func<object>>(New(_bCtor, c, d))),
                typeof(B));

            return Lambda<Func<A>>(New(_aCtor, b, c));
        }

        private LightExpression.Expression<Func<A>> CreateLightExpression()
        {
            var test = L.Constant(new NestedLambdasVsVars());
            var getOrAddMethod = test.Type.GetMethod(nameof(GetOrAdd));
            var d = L.Convert(
                L.Call(test, getOrAddMethod,
                    L.Constant(2),
                    L.Lambda<Func<object>>(L.New(_dCtor), typeof(object))),
                typeof(D));

            var c = L.Convert(
                L.Call(test, getOrAddMethod,
                    L.Constant(1),
                    L.Lambda<Func<object>>(L.New(_cCtor, d), typeof(object))),
                typeof(C));

            var b = L.Convert(
                L.Call(test, getOrAddMethod,
                    L.Constant(0),
                    L.Lambda<Func<object>>(L.New(_bCtor, c, d), typeof(object))),
                typeof(B));

            return L.Lambda<Func<A>>(L.New(_aCtor, b, c), typeof(A));
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
