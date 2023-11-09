# FastExpressionCompiler

<img src="./logo.png" alt="logo"/>

[DryIoc]: https://github.com/dadhi/DryIoc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[ExpressionTree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx
[Marten]: https://github.com/JasperFx/marten
[Rebus]: https://github.com/rebus-org/Rebus
[StructureMap]: https://github.com/structuremap/structuremap
[Lamar]: https://github.com/JasperFx/lamar
[NServiceBus]: https://github.com/Particular/NServiceBus/pull/5071

[LINQ2DB]: https://github.com/linq2db/linq2db/pull/1277
[Moq]: https://github.com/moq/moq4/issues/504#issuecomment-406714210
[Apex.Serialization]: https://github.com/dbolin/Apex.Serialization
[MapsterMapper]: https://github.com/MapsterMapper/Mapster 

[![latest release notes](https://img.shields.io/badge/latest%20release%20notes-v3.3.4-blue)](https://github.com/dadhi/FastExpressionCompiler/releases/tag/v3.3.4)
[![Windows build](https://ci.appveyor.com/api/projects/status/4iyhed69l3k0k37o/branch/master?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/fastexpressioncompiler/branch/master)[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)  

Targets .NET Standard 2.0, 2.1 and .NET 4.5

NuGet packages:

- FastExpressionCompiler [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler)
  * sources package: FastExpressionCompiler.src [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.src)](https://www.nuget.org/packages/FastExpressionCompiler.src)
  * sources with the public code made internal: FastExpressionCompiler.Internal.src [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.Internal.src)](https://www.nuget.org/packages/FastExpressionCompiler.Internal.src)
- FastExpressionCompiler.LightExpression [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.LightExpression)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression)  
  * sources package: FastExpressionCompiler.LightExpression.src [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.LightExpression.src)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression.src)  
  * sources with the public code made internal: FastExpressionCompiler.LightExpression.Internal.src [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.LightExpression.Internal.src)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression.Internal.src)

The project was originally a part of the [DryIoc], so check it out ;-)

## The problem

[ExpressionTree] compilation is used by the wide variety of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But `Expression.Compile()` is just slow. 
Moreover the compiled delegate may be slower than the manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sand-boxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

See also [a deep dive to Delegate internals](https://mattwarren.org/2017/01/25/How-do-.NET-delegates-work/#different-types-of-delegates).


## The solution

The FastExpressionCompiler `.CompileFast()` extension method is __10-40x times faster__ than `.Compile()`.  
The compiled delegate may be _in some cases_ a lot faster than the one produced by `.Compile()`.

__Note:__ The actual performance may vary depending on the multiple factors: 
platform, how complex is expression, does it have a closure, does it contain nested lambdas, etc.

In addition, the memory consumption taken by the compilation will be much smaller (check the `Allocated` column in the [benchmarks](#benchmarks) below).

## Benchmarks

**Updated to .NET 8.0**

```ini
BenchmarkDotNet v0.13.10, Windows 11 (10.0.22621.2428/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.100-rc.2.23502.2
[Host]     : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
DefaultJob : .NET 8.0.0 (8.0.23.47906), X64 RyuJIT AVX2
```

### Hoisted expression with the constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

| Method      | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Compile     | 121.969 us | 2.4180 us | 5.6040 us | 120.830 us | 35.77 |    2.46 | 0.7324 |      - |   4.49 KB |        2.92 |
| CompileFast |   3.406 us | 0.0677 us | 0.1820 us |   3.349 us |  1.00 |    0.00 | 0.2441 | 0.2365 |   1.54 KB |        1.00 |


Invoking the compiled delegate (comparing to the direct constructor call):

| Method                | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| DirectConstructorCall | 5.734 ns | 0.1501 ns | 0.2745 ns | 5.679 ns |  0.86 |    0.05 | 0.0051 |      32 B |        1.00 |
| CompiledLambda        | 6.857 ns | 0.1915 ns | 0.5434 ns | 6.704 ns |  1.01 |    0.09 | 0.0051 |      32 B |        1.00 |
| FastCompiledLambda    | 6.746 ns | 0.1627 ns | 0.1442 ns | 6.751 ns |  1.00 |    0.00 | 0.0051 |      32 B |        1.00 |


### Hoisted expression with the static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

|      Method |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|     Compile | 401.40 us | 7.509 us | 5.863 us | 400.79 us | 35.56 |    1.14 | 1.9531 | 0.9766 |  12.11 KB |        2.66 |
| CompileFast |  11.47 us | 0.228 us | 0.509 us |  11.28 us |  1.00 |    0.00 | 0.7324 | 0.7172 |   4.55 KB |        1.00 |


Invoking compiled delegate comparing to direct method call:

|              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|    DirectMethodCall |    67.15 ns |  1.401 ns |  1.965 ns |  1.06 |    0.05 | 0.0535 |     - |     - |     168 B |
|     Invoke_Compiled | 1,889.47 ns | 37.145 ns | 53.272 ns | 29.75 |    1.44 | 0.0839 |     - |     - |     264 B |
| Invoke_CompiledFast |    63.21 ns |  1.239 ns |  2.203 ns |  1.00 |    0.00 | 0.0331 |     - |     - |     104 B |


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

|                      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|---------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                     Compile | 179.266 us | 3.5687 us | 7.2089 us | 39.11 |    2.15 | 1.4648 | 0.7324 |      - |   4.74 KB |
|                 CompileFast |   4.791 us | 0.0955 us | 0.2307 us |  1.04 |    0.06 | 0.4578 | 0.2289 | 0.0305 |   1.41 KB |
| CompileFast_LightExpression |   4.636 us | 0.0916 us | 0.1531 us |  1.00 |    0.00 | 0.4425 | 0.2213 | 0.0305 |   1.38 KB |


Invoking the compiled delegate compared to the normal delegate and the direct call:

|                             Method |     Mean |    Error |   StdDev |   Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|                   DirectLambdaCall | 13.72 ns | 0.274 ns | 0.500 ns | 13.62 ns |  1.05 |    0.06 | 0.0102 |     - |     - |      32 B |
|                     CompiledLambda | 17.12 ns | 1.006 ns | 2.950 ns | 15.78 ns |  1.24 |    0.15 | 0.0102 |     - |     - |      32 B |
|                 FastCompiledLambda | 12.87 ns | 0.164 ns | 0.128 ns | 12.88 ns |  0.97 |    0.03 | 0.0102 |     - |     - |      32 B |
| FastCompiledLambda_LightExpression | 13.11 ns | 0.258 ns | 0.471 ns | 13.01 ns |  1.00 |    0.00 | 0.0102 |     - |     - |      32 B |


### FastExpressionCompiler.LightExpression.Expression vs System.Linq.Expressions.Expression

`FastExpressionCompiler.LightExpression.Expression` is the lightweight version of `System.Linq.Expressions.Expression`. 
It is designed to be a __drop-in replacement__ for the System Expression - just install the __FastExpressionCompiler.LightExpression__ package instead of __FastExpressionCompiler__ and replace the usings

```cs
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
```

with

```cs
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
```

You may look at it as a bare-bone wrapper for the computation operation node which helps you to compose the computation tree (without messing with the IL emit directly).
It __won't validate operations compatibility__ for the tree the way `System.Linq.Expression` does it, and partially why it is so slow.
Hopefully you are checking the expression arguments yourself and not waiting for the `Expression` exceptions to blow-up.

[Sample expression](https://github.com/dadhi/FastExpressionCompiler/blob/6da130c62f6adaa293f34a1a0c19ea4522f9c989/test/FastExpressionCompiler.LightExpression.UnitTests/LightExpressionTests.cs#L167)

Creating the expression:
|                                Method |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
|                      CreateExpression | 4,698.0 ns | 110.77 ns | 317.81 ns | 4,623.0 ns |  7.99 |    0.85 | 0.4501 |     - |     - |    1416 B |
|                 CreateLightExpression |   591.2 ns |  15.42 ns |  44.98 ns |   580.7 ns |  1.00 |    0.00 | 0.1574 |     - |     - |     496 B |
| CreateLightExpression_with_intrinsics |   580.2 ns |  16.95 ns |  48.08 ns |   565.0 ns |  0.98 |    0.10 | 0.1554 |     - |     - |     488 B |

Creating and compiling:

|                                               Method |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------------------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
|                         CreateExpression_and_Compile | 174.412 us | 3.4771 us | 7.5590 us | 37.06 |    2.23 | 0.9766 | 0.4883 |   7.18 KB |        2.93 |
|                     CreateExpression_and_CompileFast |   6.395 us | 0.1265 us | 0.2314 us |  1.36 |    0.07 | 0.5341 | 0.5264 |    3.3 KB |        1.34 |
|                CreateLightExpression_and_CompileFast |   4.703 us | 0.0931 us | 0.1336 us |  1.00 |    0.00 | 0.3967 | 0.3891 |   2.45 KB |        1.00 |
| CreateLightExpression_and_CompileFast_with_intrinsic |   4.430 us | 0.0627 us | 0.0490 us |  0.94 |    0.03 | 0.3891 | 0.3738 |   2.38 KB |        0.97 |

## Difference between FastExpressionCompiler and FastExpressionCompiler.LightExpression

FastExpressionCompiler

- Provides the `CompileFast` extension methods for the `System.Linq.Expressions.LambdaExpression`.

FastExpressionCompiler.LightExpression

- Provides the `CompileFast` extension methods for `FastExpressionCompiler.LightExpression.LambdaExpression`.
- Provides the drop-in expression replacement with the less consumed memory and the faster construction at the cost of the less validation.
- Includes its own `ExpressionVisitor`.
- Supports `ToExpression` method to convert back to the `System.Linq.Expressions.Expression`.

Both FastExpressionCompiler and FastExpressionCompiler.LightExpression

- Support `ToCSharpString()` method to output the compile-able C# code represented by expression.
- Support `ToExpressionString()` method to output the expression construction C# code, so given the expression object you'll get e.g. `Expression.Lambda(Expression.New(...))`.


## Who's using it

[Marten], [Rebus], [StructureMap], [Lamar], [ExpressionToCodeLib], [NServiceBus], [LINQ2DB], [MapsterMapper]

Considering: [Moq], [Apex.Serialization]


## How to use

Install from the NuGet and add the `using FastExpressionCompiler;` and replace the call to the `.Compile()` with the `.CompileFast()` extension method.

__Note:__ `CompileFast` has an optional parameter `bool ifFastFailedReturnNull = false` to disable fallback to `Compile`.

### Examples

Hoisted lambda expression (created by the C# Compiler):

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

__Note:__ You may simplify Expression usage and enable faster refactoring with the C# `using static` statement:

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


## How it works

The idea is to provide the fast compilation for the supported expression types
and fallback to the system `Expression.Compile()` for the not supported types:

### What's not supported yet

**FEC V3 does not support yet:** 

- `Quote`
- `Dynamic`
- `RuntimeVariables`
- `DebugInfo`
- `MemberInit` with the `MemberMemberBinding` and the `ListMemberBinding` binding types
- `NewArrayInit` multi-dimensional array initializer is not supported yet

To find what nodes are not supported in your expression you may use the technic described below in the [Diagnostics](#diagnostics) section. 

The compilation is done by traversing the expression nodes and emitting the IL. 
The code is tuned for the performance and the minimal memory consumption. 

The expression is traversed twice:

- 1st round is to collect the constants and nested lambdas into the closure objects.
- 2nd round is to emit the IL code and create the delegate using the `DynamicMethod`.

If visitor finds the not supported expression node or the error condition, 
the compilation is aborted, and `null` is returned enabling the fallback to System `.Compile()`.


## Diagnostics

FEC V3 adds powerful diagnostics tools.

You may pass the optional `CompilerFlags.EnableDelegateDebugInfo`  into the `CompileFast` methods.

`EnableDelegateDebugInfo` adds the diagnostic info into the compiled delegate including its source Expression and C# code. 
Can be used as following:

```cs
var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
var di = f.Target as IDelegateDebugInfo;
Assert.IsNotNull(di.Expression);
Assert.IsNotNull(di.ExpressionString);
Assert.IsNotNull(di.CSharpString);
```

Those conversion capabilities are also available as the `ToCSharpString` and `ToExpressionString` extension methods.

Besides that, when converting the source expression to either C# code or to the Expression construction code you may find 
the `// NOT_SUPPORTED_EXPRESSION` comments marking the not supported yet expressions by FEC. So you may verify the presence or absence of this comment in a test. 


### ThrowOnNotSupportedExpression and NotSupported cases enum

FEC V3.1 adds to the compiler flags the `CompilerFlags.ThrowOnNotSupportedExpression` 
so that compiling the expression with not supported node will throw the respective exception instead of returning `null`.

To get the actual list of the not supported cases you may check `NotSupported` enum.


## Additional optimizations

1. Using `FastExpressionCompiler.LightExpression.Expression` instead of `System.Linq.Expressions.Expression` for the faster expression creation.  
2. Using `.TryCompileWithPreCreatedClosure` and `.TryCompileWithoutClosure` methods when you know the expression at hand and may skip the first traversing round, e.g. for the "static" expression which does not contain the bound constants. __Note:__ You cannot skip the 1st round if the expression contains the `Block`, `Try`, or `Goto` expressions.

---
<a target="_blank" href="https://icons8.com/icons/set/bitten-ice-pop">Bitten Ice Pop icon</a> icon by <a target="_blank" href="https://icons8.com">Icons8</a>
