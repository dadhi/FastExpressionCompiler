# FastExpressionCompiler

[DryIoc]: https://github.com/dadhi/DryIoc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[ExpressionTree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx
[Marten]: https://github.com/JasperFx/marten
[Rebus]: https://github.com/rebus-org/Rebus
[StructureMap]: https://github.com/structuremap/structuremap
[Lamar]: https://github.com/JasperFx/lamar
[NServiceBus]: https://github.com/Particular/NServiceBus/pull/5071

[LINQ to DB]: https://github.com/linq2db/linq2db/pull/1277
[Moq]: https://github.com/moq/moq4/issues/504#issuecomment-406714210

FastExpressionCompiler [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler)[![fuget.org package api diff](https://www.fuget.org/packages/FastExpressionCompiler/badge.svg?v=2.0.0)](https://www.fuget.org/packages/FastExpressionCompiler/2.0.0/lib/netstandard2.0/diff/1.10.1/)  
FastExpressionCompiler.LightExpression [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.LightExpression)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression)[![fuget.org package last version](https://www.fuget.org/packages/FastExpressionCompiler.LightExpression/badge.svg?v=2.0.0)](https://www.fuget.org/packages/FastExpressionCompiler.LightExpression/2.0.0)



[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)  


- Windows: [![Windows build](https://ci.appveyor.com/api/projects/status/4iyhed69l3k0k37o/branch/master?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/fastexpressioncompiler/branch/master)
- Linux, MacOS: [![Linux build](https://travis-ci.org/dadhi/FastExpressionCompiler.svg?branch=master)](https://travis-ci.org/dadhi/FastExpressionCompiler)

Targets: __.NET 4.5+__, __.NET Standard 1.3__, __.NET Standard 2.0__  
Originally was developed as a part of [DryIoc], so check it out ;-)

## The problem

[ExpressionTree] compilation is used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But `Expression.Compile()` is just slow. 
Moreover, the compiled delegate may be slower than manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sand-boxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

`.CompileFast()` is __10-30x times faster__ than `.Compile()`.  
The compiled delegate may be _in some cases_ 15x times faster than the one produced by `.Compile()`.

__Note:__ The actual performance may vary depending on multiple factors: 
platform, how complex is expression, does it have a closure over the values, does it contain nested lambdas, etc.


## How to install

Install from [NuGet](https://www.nuget.org/packages/FastExpressionCompiler) or grab a single [FastExpressionCompiler.cs](https://github.com/dadhi/FastExpressionCompiler/blob/master/src/FastExpressionCompiler/FastExpressionCompiler.cs) file.


## Some users

[Marten], [Rebus], [StructureMap], [Lamar], [ExpressionToCodeLib], [NServiceBus].

Considering: [Moq], [LINQ to DB]


## How to use

Add reference to _FastExpressionCompiler_ and replace call to `.Compile()` with `.CompileFast()` extension method.

__Note:__ `CompileFast` has an optional parameter `bool ifFastFailedReturnNull = false` to disable fallback to `Compile`.

### Examples

Hoisted lambda expression (created by compiler):

```cs
var a = new A(); var b = new B();
Expression<Func<X>> expr = () => new X(a, b);

var getX = expr.CompileFast();
var x = getX();
```

Manually composed lambda expression:

```cs
var a = new A();
var bParamExpr = Expression.Parameter(typeof(B), "b");
var expr = Expression.Lambda(
    Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var getX = expr.CompileFast();
var x = getX(new B());
```

__Note:__ Simplify your life in C# 6+ with `using static`

```cs
using static System.Linq.Expressions.Expression;
// or
// using static FastExpressionCompiler.LightExpression.Expression;

var a = new A();
var bParamExpr = Parameter(typeof(B), "b");
var expr = Lambda(
    New(typeof(X).GetTypeInfo().DeclaredConstructors.First(), Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var x = expr.CompileFast()(new B());
```

## Benchmarks

```ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156255 Hz, Resolution=463.7670 ns, Timer=TSC
.NET Core SDK=2.2.100
  [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT

```

### Hoisted expression with constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

|     Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|----------- |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|CompileFast |   7.996 us | 0.0638 us | 0.0565 us |  1.00 |    0.00 |      0.4883 |      0.2441 |      0.0305 |             2.26 KB |
|    Compile | 242.974 us | 1.4929 us | 1.3964 us | 30.39 |    0.26 |      0.7324 |      0.2441 |           - |             4.45 KB |


Invoking compiled delegate (also comparing to the direct constructor call):

|                Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|---------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
| DirectConstructorCall |  6.203 ns | 0.1898 ns | 0.3470 ns |  0.76 |    0.06 |      0.0068 |           - |           - |                32 B |
|    FastCompiledLambda |  7.840 ns | 0.2010 ns | 0.1881 ns |  1.00 |    0.00 |      0.0068 |           - |           - |                32 B |
|        CompiledLambda | 12.313 ns | 0.1124 ns | 0.1052 ns |  1.57 |    0.04 |      0.0068 |           - |           - |                32 B |


### Hoisted expression with static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

|                 Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|----------------------- |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     Expression_Compile | 481.33 us | 0.6025 us | 0.5031 us | 29.47 |    0.09 | 2.4414 | 0.9766 |      - |  11.95 KB |
| Expression_CompileFast |  16.33 us | 0.0555 us | 0.0492 us |  1.00 |    0.00 | 1.0986 | 0.5493 | 0.0916 |   5.13 KB |

Invoking compiled delegate comparing to direct method call:

|              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|    DirectMethodCall |    50.78 ns | 0.1651 ns | 0.1544 ns |  0.92 |    0.01 | 0.0356 |     - |     - |     168 B |
|     Invoke_Compiled | 1,385.24 ns | 2.8196 ns | 2.6375 ns | 25.10 |    0.33 | 0.0553 |     - |     - |     264 B |
| Invoke_CompiledFast |    55.20 ns | 0.8883 ns | 0.7875 ns |  1.00 |    0.00 | 0.0220 |     - |     - |     104 B |


### Manually composed expression with parameters and closure

```cs
var a = new A();
var bParamExpr = Expression.Parameter(typeof(B), "b");
var expr = Expression.Lambda(
    Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);
```

Compiling expression:

|                                          Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------------------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
| CompileFastWithPreCreatedClosureLightExpression |   4.892 us | 0.0965 us | 0.0948 us |  1.00 |    0.00 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |
|                CompileFastWithPreCreatedClosure |   5.186 us | 0.0896 us | 0.0795 us |  1.06 |    0.02 |      0.3281 |      0.1602 |      0.0305 |              1.5 KB |
|                                     CompileFast |   7.257 us | 0.0648 us | 0.0606 us |  1.49 |    0.03 |      0.4349 |      0.2136 |      0.0305 |             1.99 KB |
|                                         Compile | 176.107 us | 1.3451 us | 1.2582 us | 36.05 |    0.75 |      0.9766 |      0.4883 |           - |              4.7 KB |

Invoking compiled delegate compared to the normal delegate:

|                                    Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------------------------ |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
| FastCompiledLambdaWithPreCreatedClosureLE | 10.64 ns | 0.0404 ns | 0.0358 ns |  1.00 |      0.0068 |           - |           - |                32 B |
|                          DirectLambdaCall | 10.65 ns | 0.0601 ns | 0.0533 ns |  1.00 |      0.0068 |           - |           - |                32 B |
|                        FastCompiledLambda | 10.98 ns | 0.0434 ns | 0.0406 ns |  1.03 |      0.0068 |           - |           - |                32 B |
|   FastCompiledLambdaWithPreCreatedClosure | 11.10 ns | 0.0369 ns | 0.0345 ns |  1.04 |      0.0068 |           - |           - |                32 B |
|                            CompiledLambda | 11.13 ns | 0.0620 ns | 0.0518 ns |  1.05 |      0.0068 |           - |           - |                32 B |


### FEC.LightExpression.Expression vs Expression

`FastExpressionCompiler.LightExpression.Expression` is the lightweight version of `System.Linq.Expressions.Expression`. 
It is designed to be a __drop-in replacement__ for System Expression - just install __FastExpressionCompiler.LightExpression__ package instead of __FastExpressionCompiler__ and replace the usings

```cs
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
```

with

```cs
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
```

You may look at it as a bare wrapper over expression node which helps you to compose the computation tree.  
It __won't do any node compatibility verification__ for the tree as the `Expression` does and why the creation of the latter is slower.
Hopefully you are checking the expression arguments yourself and not waiting for `Expression` exceptions to blow up - then you are safe.

[Sample expression](https://github.com/dadhi/FastExpressionCompiler/blob/6da130c62f6adaa293f34a1a0c19ea4522f9c989/test/FastExpressionCompiler.LightExpression.UnitTests/LightExpressionTests.cs#L167)

Creating expression:

|               Method  |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|---------------------- |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
| CreateLightExpression |   389.5 ns | 0.9547 ns | 0.7972 ns |  1.00 |    0.00 |      0.1693 |           - |           - |               800 B |
|     CreateExpression  | 3,574.7 ns | 8.0032 ns | 7.4862 ns |  9.18 |    0.02 |      0.2823 |           - |           - |              1344 B |

Creating and compiling:

|                                Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------------------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
| CreateLightExpression_and_CompileFast |  12.68 us | 0.0555 us | 0.0492 us |  1.00 |    0.00 |      1.4343 |      0.7172 |      0.0458 |             6.61 KB |
|      CreateExpression_and_CompileFast |  19.26 us | 0.2559 us | 0.2268 us |  1.52 |    0.02 |      1.5564 |      0.7629 |      0.0305 |             7.23 KB |
|          CreateExpression_and_Compile | 260.67 us | 1.7431 us | 1.6305 us | 20.54 |    0.14 |      1.4648 |      0.4883 |           - |             7.16 KB |



## How it works

The idea is to provide fast compilation for supported expression types,
and fallback to system `Expression.Compile()` for not _yet_ supported types.

__Note__: As of v1.9 most of the types are supported, please open issue if something is not ;-)

Compilation is done by visiting expression nodes and emitting the IL. 
The code is tuned for performance and minimal memory consumption. 

Expression is visited in two rounds (you can skip the first one with up-front knowledge):

1. To collect constants and nested lambdas into closure objects
2. To emit the IL and create the delegate from a `DynamicMethod`

If visitor finds a not supported expression node, 
the compilation is aborted, and null is returned enabling the fallback to normal `.Compile()`.

### Additional optimizations

1. Using `FastExpressionCompiler.LightExpression.Expression` instead of `System.Linq.Expressions.Expression` for the _lightweight_ expression creation.  
It won't speed-up compilation alone but may speed-up the construction.
2. Using `expr.TryCompileWithPreCreatedClosure` and `expr.TryCompileWithoutClosure` when you know the 
expression at hand and may optimize for delegate with the closure or for "static" delegate.

Both optimizations are visible in benchmark results: search for `LightExpression` and 
`FastCompiledLambdaWithPreCreatedClosure` respectively.
