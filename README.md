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

[![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler)
[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)  

- Windows: [![Windows build](https://ci.appveyor.com/api/projects/status/4iyhed69l3k0k37o/branch/master?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/fastexpressioncompiler/branch/master)
- Linux, MacOS: [![Linux build](https://travis-ci.org/dadhi/FastExpressionCompiler.svg?branch=master)](https://travis-ci.org/dadhi/FastExpressionCompiler)

Supported platforms: __.NET 4.5+__, __.NET Standard 1.3__  
Originally was developed as a part of [DryIoc], so check it out ;-)

## The problem

[ExpressionTree] compilation is used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But `Expression.Compile()` is just slow. 
Moreover, the compiled delegate may be slower than manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sand-boxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

`.CompileFast()` is __10-40x times faster__ than `.Compile()`.  
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

```csharp
var a = new A(); var b = new B();
Expression<Func<X>> expr = () => new X(a, b);

var getX = expr.CompileFast();
var x = getX();
```

Manually composed lambda expression:

```csharp
var a = new A();
var bParamExpr = Expression.Parameter(typeof(B), "b");
var expr = Expression.Lambda(
    Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var getX = expr.CompileFast();
var x = getX(new B());
```

Using `ExpressionInfo` instead of `Expression`:

```csharp
var a = new A();
var bParamExpr = ExpressionInfo.Parameter(typeof(B), "b");
var expr = ExpressionInfo.Lambda(
    ExpressionInfo.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
        ExpressionInfo.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var getX = expr.CompileFast();
var x = getX(new B());
```

__Note:__ Simplify your life in C# 6+ with `using static`

```csharp
using static System.Linq.Expressions.Expression;
// or
//  using static FastExpressionCompiler.ExpressionInfo;

var a = new A();
var bParamExpr = Parameter(typeof(B), "b");
var expr = Lambda(
    New(typeof(X).GetTypeInfo().DeclaredConstructors.First(), Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var x = expr.CompileFast()(new B());
```


## Benchmarks

``` ini

BenchmarkDotNet=v0.11.0, OS=Windows 10.0.14393.2248 (1607/AnniversaryUpdate/Redstone1)
Intel Core i5-6300U CPU 2.40GHz (Skylake), 1 CPU, 4 logical and 2 physical cores
Frequency=2437501 Hz, Resolution=410.2562 ns, Timer=TSC
  [Host] : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3062.0
  Clr    : .NET Framework 4.7.2 (CLR 4.0.30319.42000), 64bit RyuJIT-v4.7.3062.0
  Core   : .NET Core 2.0.9 (CoreCLR 4.6.26614.01, CoreFX 4.6.26614.01), 64bit RyuJIT

```

### Hoisted expression with constructor and two arguments in closure

```csharp
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

|      Method |  Job | Runtime |      Mean |      Error |     StdDev |    Median | Scaled | ScaledSD |   Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----- |-------- |----------:|-----------:|-----------:|----------:|-------:|---------:|--------:|-------:|-------:|----------:|
|     Compile |  Clr |     Clr | 713.72 us | 15.6659 us | 41.2702 us | 698.30 us |  39.96 |     2.36 | 15.6250 |      - |      - |  25.14 KB |
| CompileFast |  Clr |     Clr |  17.86 us |  0.3338 us |  0.2606 us |  17.89 us |   1.00 |     0.00 |  1.5259 | 0.7629 | 0.0305 |   2.38 KB |
|             |      |         |           |            |            |           |        |          |         |        |        |           |
|     Compile | Core |    Core | 523.29 us | 10.0131 us | 10.7139 us | 522.02 us |  38.27 |     0.85 |  2.9297 | 0.9766 |      - |    4.5 KB |
| CompileFast | Core |    Core |  13.68 us |  0.1513 us |  0.1416 us |  13.60 us |   1.00 |     0.00 |  1.4801 | 0.7324 | 0.0458 |   2.26 KB |

Invoking compiled delegate comparing to direct constructor call:

|                Method |  Job | Runtime |      Mean |     Error |    StdDev |    Median | Scaled | ScaledSD |  Gen 0 | Allocated |
|---------------------- |----- |-------- |----------:|----------:|----------:|----------:|-------:|---------:|-------:|----------:|
| DirectConstructorCall |  Clr |     Clr |  6.905 ns | 0.1926 ns | 0.1708 ns |  6.864 ns |   0.40 |     0.04 | 0.0203 |      32 B |
|        CompiledLambda |  Clr |     Clr | 20.457 ns | 0.6230 ns | 1.7572 ns | 20.147 ns |   1.19 |     0.15 | 0.0203 |      32 B |
|    FastCompiledLambda |  Clr |     Clr | 17.320 ns | 0.5935 ns | 1.7498 ns | 16.832 ns |   1.00 |     0.00 | 0.0203 |      32 B |
|                       |      |         |           |           |           |           |        |          |        |           |
| DirectConstructorCall | Core |    Core | 10.900 ns | 0.3226 ns | 0.6059 ns | 10.762 ns |   0.58 |     0.03 | 0.0203 |      32 B |
|        CompiledLambda | Core |    Core | 19.350 ns | 0.3138 ns | 0.2782 ns | 19.275 ns |   1.03 |     0.03 | 0.0203 |      32 B |
|    FastCompiledLambda | Core |    Core | 18.846 ns | 0.3653 ns | 0.4061 ns | 18.900 ns |   1.00 |     0.00 | 0.0203 |      32 B |


### Hoisted expression with static method and two nested lambdas and two arguments in closure

```csharp
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

|      Method |  Job | Runtime |        Mean |      Error |     StdDev | Scaled | ScaledSD |   Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----- |-------- |------------:|-----------:|-----------:|-------:|---------:|--------:|-------:|-------:|----------:|
|     Compile |  Clr |     Clr | 1,120.60 us | 18.4641 us | 16.3680 us |  17.55 |     0.39 | 13.6719 | 1.9531 |      - |   23.2 KB |
| FastCompile |  Clr |     Clr |    63.87 us |  1.2301 us |  1.1506 us |   1.00 |     0.00 |  5.1270 | 2.5635 | 0.1221 |   7.91 KB |
|             |      |         |             |            |            |        |          |         |        |        |           |
|     Compile | Core |    Core |   941.99 us | 13.1721 us | 12.3212 us |  17.76 |     0.32 |  7.8125 | 1.9531 |      - |  12.12 KB |
| FastCompile | Core |    Core |    53.06 us |  0.8499 us |  0.7097 us |   1.00 |     0.00 |  5.0659 | 2.5024 | 0.1831 |   7.74 KB |

Invoking compiled delegate comparing to direct method call:

|             Method |  Job | Runtime |        Mean |     Error |    StdDev |      Median | Scaled | ScaledSD |  Gen 0 | Allocated |
|------------------- |----- |-------- |------------:|----------:|----------:|------------:|-------:|---------:|-------:|----------:|
|   DirectMethodCall |  Clr |     Clr |   162.71 ns |  4.971 ns |  4.650 ns |   162.45 ns |   0.81 |     0.04 | 0.1168 |     184 B |
|     CompiledLambda |  Clr |     Clr | 2,551.91 ns | 46.831 ns | 41.514 ns | 2,552.15 ns |  12.68 |     0.57 | 0.1755 |     280 B |
| FastCompiledLambda |  Clr |     Clr |   201.59 ns |  4.102 ns |  8.918 ns |   200.38 ns |   1.00 |     0.00 | 0.1521 |     240 B |
|                    |      |         |             |           |           |             |        |          |        |           |
|   DirectMethodCall | Core |    Core |    79.05 ns |  1.558 ns |  1.457 ns |    79.23 ns |   0.72 |     0.03 | 0.1067 |     168 B |
|     CompiledLambda | Core |    Core | 2,302.00 ns | 45.045 ns | 63.147 ns | 2,300.54 ns |  20.83 |     1.00 | 0.1640 |     264 B |
| FastCompiledLambda | Core |    Core |   110.71 ns |  2.211 ns |  4.760 ns |   108.67 ns |   1.00 |     0.00 | 0.1423 |     224 B |


### Manually composed expression with parameters and closure

```csharp
var a = new A();
var bParamExpr = Expression.Parameter(typeof(B), "b");
var expr = Expression.Lambda(
    Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);
```

Compiling expression:

|                           Method |  Job | Runtime |       Mean |      Error |     StdDev |     Median | Scaled | ScaledSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|--------------------------------- |----- |-------- |-----------:|-----------:|-----------:|-----------:|-------:|---------:|-------:|-------:|-------:|----------:|
|                          Compile |  Clr |     Clr | 428.943 us |  8.5066 us | 23.2866 us | 430.301 us |  33.92 |     4.07 | 2.9297 | 1.4648 |      - |   4.95 KB |
|                      CompileFast |  Clr |     Clr |  19.940 us |  0.4541 us |  0.9774 us |  19.621 us |   1.58 |     0.19 | 1.3733 | 0.6714 | 0.0305 |   2.13 KB |
| CompileFastWithPreCreatedClosure |  Clr |     Clr |  12.800 us |  0.5018 us |  1.4558 us |  12.211 us |   1.00 |     0.00 | 1.0376 | 0.5188 | 0.0305 |    1.6 KB |
|                                  |      |         |            |            |            |            |        |          |        |        |        |           |
|                          Compile | Core |    Core | 422.724 us | 13.4465 us | 36.8097 us | 410.179 us |  47.70 |     4.70 | 2.9297 | 1.4648 |      - |   4.75 KB |
|                      CompileFast | Core |    Core |  13.058 us |  0.2534 us |  0.3468 us |  13.026 us |   1.47 |     0.08 | 1.3275 | 0.6561 | 0.0458 |   2.02 KB |
| CompileFastWithPreCreatedClosure | Core |    Core |   8.883 us |  0.1767 us |  0.4368 us |   8.747 us |   1.00 |     0.00 | 1.0071 | 0.5035 | 0.0305 |   1.53 KB |

Invoking compiled delegate comparing to normal delegate:

|                                  Method |  Job | Runtime |      Mean |     Error |    StdDev | Scaled | ScaledSD |  Gen 0 | Allocated |
|---------------------------------------- |----- |-------- |----------:|----------:|----------:|-------:|---------:|-------:|----------:|
|                               RawLambda |  Clr |     Clr | 13.001 ns | 0.2483 ns | 0.3049 ns |   1.64 |     0.04 | 0.0203 |      32 B |
|                          CompiledLambda |  Clr |     Clr | 21.049 ns | 0.3876 ns | 0.3436 ns |   2.66 |     0.05 | 0.0203 |      32 B |
|                      FastCompiledLambda |  Clr |     Clr | 12.628 ns | 0.2421 ns | 0.2486 ns |   1.60 |     0.04 | 0.0203 |      32 B |
| FastCompiledLambdaWithPreCreatedClosure |  Clr |     Clr |  7.910 ns | 0.1117 ns | 0.1045 ns |   1.00 |     0.00 | 0.0203 |      32 B |
|                                         |      |         |           |           |           |        |          |        |           |
|                               RawLambda | Core |    Core | 16.434 ns | 0.1441 ns | 0.1277 ns |   0.99 |     0.01 | 0.0203 |      32 B |
|                          CompiledLambda | Core |    Core | 16.494 ns | 0.1299 ns | 0.1151 ns |   1.00 |     0.01 | 0.0203 |      32 B |
|                      FastCompiledLambda | Core |    Core | 16.568 ns | 0.1767 ns | 0.1653 ns |   1.00 |     0.01 | 0.0203 |      32 B |
| FastCompiledLambdaWithPreCreatedClosure | Core |    Core | 16.546 ns | 0.1251 ns | 0.1109 ns |   1.00 |     0.00 | 0.0203 |      32 B |


### ExpressionInfo vs Expression

`FastExpressionCompiler.ExpressionInfo` is the lightweight version of `Expression`. 
You may look at it as just a thin wrapper on operation node which helps you to compose the computation tree. But
it __won't do any node compatibility verification__ for the tree as the `Expression` does (and why it is slow).
Hopefully, you are checking the expression arguments yourself, and not waiting for `Expression` exceptions to blow up.

__Note:__ At the moment `ExpressionInfo` is not supported for all supported expression types
([#46](https://github.com/dadhi/FastExpressionCompiler/issues/46)).

[Sample expression](https://github.com/dadhi/FastExpressionCompiler/blob/8cab34992be52e5f0f18805f21e0e6faab69493a/test/FastExpressionCompiler.UnitTests/ExpressionInfoTests.cs#L145)

Creating expression:

|               Method |  Job | Runtime |       Mean |      Error |     StdDev |     Median | Scaled | ScaledSD |  Gen 0 | Allocated |
|--------------------- |----- |-------- |-----------:|-----------:|-----------:|-----------:|-------:|---------:|-------:|----------:|
|     CreateExpression |  Clr |     Clr | 5,789.3 ns | 182.642 ns | 526.963 ns | 5,677.1 ns |  10.05 |     1.12 | 0.9613 |    1512 B |
| CreateExpressionInfo |  Clr |     Clr |   578.5 ns |  13.347 ns |  39.355 ns |   560.7 ns |   1.00 |     0.00 | 0.4520 |     712 B |
|                      |      |         |            |            |            |            |        |          |        |           |
|     CreateExpression | Core |    Core | 6,497.4 ns | 115.563 ns |  96.500 ns | 6,475.1 ns |   9.25 |     0.17 | 0.9003 |    1424 B |
| CreateExpressionInfo | Core |    Core |   702.4 ns |   8.578 ns |   8.024 ns |   700.1 ns |   1.00 |     0.00 | 0.4520 |     712 B |

Creating and compiling:

|                               Method |  Job | Runtime |      Mean |      Error |    StdDev | Scaled | ScaledSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------------------------------- |----- |-------- |----------:|-----------:|----------:|-------:|---------:|-------:|-------:|-------:|----------:|
|         CreateExpression_and_Compile |  Clr |     Clr | 684.45 us |  8.9497 us | 7.9336 us |  28.92 |     0.44 | 4.8828 | 1.9531 |      - |   8.09 KB |
|     CreateExpression_and_CompileFast |  Clr |     Clr |  32.53 us |  0.4963 us | 0.4400 us |   1.37 |     0.02 | 4.4556 | 2.1973 | 0.0610 |   6.87 KB |
| CreateExpressionInfo_and_CompileFast |  Clr |     Clr |  23.67 us |  0.2824 us | 0.2503 us |   1.00 |     0.00 | 3.9673 | 1.9836 | 0.0610 |    6.1 KB |
|                                      |      |         |           |            |           |        |          |        |        |        |           |
|         CreateExpression_and_Compile | Core |    Core | 580.09 us | 10.5438 us | 9.3468 us |  27.57 |     0.48 | 3.9063 | 1.9531 |      - |   7.32 KB |
|     CreateExpression_and_CompileFast | Core |    Core |  31.89 us |  0.4771 us | 0.4229 us |   1.52 |     0.02 | 4.6387 | 2.3193 | 0.0610 |   7.14 KB |
| CreateExpressionInfo_and_CompileFast | Core |    Core |  21.05 us |  0.1887 us | 0.1673 us |   1.00 |     0.00 | 4.2114 | 2.1057 | 0.0610 |   6.48 KB |



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

1. Using `ExpressionInfo` instead of `Expression` for _lightweight_ expression creation.  
Won't speed-up compilation alone, but may speed-up construction+compilation.
2. Using `expr.TryCompileWithPreCreatedClosure` and `expr.TryCompileWithoutClosure` when you know the 
expression at hand and may optimize for delegate with closure or for "static" delegate.

Both optimizations are visible in benchmark results: search for `ExpressionInfo` and 
`FastCompiledLambdaWithPreCreatedClosure` respectively.
