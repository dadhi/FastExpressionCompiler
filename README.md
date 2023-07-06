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

**Updated to .NET 6**

```ini
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=6.0.201
  [Host]     : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
  DefaultJob : .NET Core 6.0.3 (CoreCLR 6.0.322.12309, CoreFX 6.0.322.12309), X64 RyuJIT
```

### Hoisted expression with the constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

|      Method |       Mean |     Error |     StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |-----------:|----------:|-----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     Compile | 272.904 us | 5.4074 us | 11.8694 us | 50.84 |    3.34 | 1.4648 | 0.4883 |      - |   4.49 KB |
| CompileFast |   5.379 us | 0.1063 us |  0.2048 us |  1.00 |    0.00 | 0.4959 | 0.2441 | 0.0381 |   1.52 KB |


Invoking the compiled delegate (comparing to the direct constructor call):

|                Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
| DirectConstructorCall |  7.736 ns | 0.2472 ns | 0.6336 ns |  7.510 ns |  0.57 |    0.05 | 0.0102 |     - |     - |      32 B |
|        CompiledLambda | 13.917 ns | 0.2723 ns | 0.3818 ns | 13.872 ns |  1.03 |    0.04 | 0.0102 |     - |     - |      32 B |
|    FastCompiledLambda | 13.412 ns | 0.2355 ns | 0.4124 ns | 13.328 ns |  1.00 |    0.00 | 0.0102 |     - |     - |      32 B |


### Hoisted expression with the static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

|      Method |      Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     Compile | 641.72 us | 12.785 us | 26.117 us | 28.87 |    1.78 | 3.9063 | 1.9531 |      - |  12.05 KB |
| CompileFast |  22.31 us |  0.444 us |  0.876 us |  1.00 |    0.00 | 1.7700 | 0.8850 | 0.1221 |   5.45 KB |

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

|                                                Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------------------------------------------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|                          CreateExpression_and_Compile | 541.65 us | 16.585 us | 47.048 us | 520.79 us | 33.98 |    3.97 | 1.9531 | 0.9766 |      - |   7.26 KB |
|                      CreateExpression_and_CompileFast |  23.51 us |  0.724 us |  2.102 us |  23.08 us |  1.47 |    0.17 | 1.2207 | 0.6104 | 0.0305 |   3.79 KB |
|                 CreateLightExpression_and_CompileFast |  16.03 us |  0.430 us |  1.227 us |  15.50 us |  1.00 |    0.00 | 0.9155 | 0.4578 | 0.0305 |   2.84 KB |
| CreateLightExpression_and_CompileFast_with_intrinsics |  13.94 us |  0.629 us |  1.845 us |  13.37 us |  0.88 |    0.13 | 0.8545 | 0.4272 | 0.0305 |   2.64 KB |


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
