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

[LINQ to DB]: https://github.com/linq2db/linq2db/pull/1277
[Moq]: https://github.com/moq/moq4/issues/504#issuecomment-406714210
[Apex.Serialization]: https://github.com/dbolin/Apex.Serialization


[![latest release](https://img.shields.io/badge/latest%20release-v3.0.0-blue)](https://github.com/dadhi/FastExpressionCompiler/releases/edit/untagged-e67dc3054471d280cc74)
[![Windows build](https://ci.appveyor.com/api/projects/status/4iyhed69l3k0k37o/branch/master?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/fastexpressioncompiler/branch/master)[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)  
Targets .NET Standard 2.0 and .NET 4.5  
NuGet packages:

- FastExpressionCompiler [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler/3.0.0-preview-05)[![fuget.org package api diff](https://www.fuget.org/packages/FastExpressionCompiler/badge.svg?v=3.0.0-preview-05)](https://www.fuget.org/packages/FastExpressionCompiler/3.0.0-preview-05)  
- FastExpressionCompiler.LightExpression [![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler.LightExpression)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression/3.0.0-preview-05)[![fuget.org package last version](https://www.fuget.org/packages/FastExpressionCompiler.LightExpression/badge.svg?v=3.0.0-preview-05)](https://www.fuget.org/packages/FastExpressionCompiler.LightExpression/3.0.0-preview-05)

Originally is a part of the [DryIoc], so check it out ;-)

- [FastExpressionCompiler](#fastexpressioncompiler)
  - [The problem](#the-problem)
  - [The solution](#the-solution)
  - [Difference between FastExpressionCompiler and FastExpressionCompiler.LightExpression](#difference-between-fastexpressioncompiler-and-fastexpressioncompilerlightexpression)
  - [Who's using it](#whos-using-it)
  - [How to use](#how-to-use)
    - [Examples](#examples)
  - [Benchmarks](#benchmarks)
    - [Hoisted expression with the constructor and two arguments in closure](#hoisted-expression-with-the-constructor-and-two-arguments-in-closure)
    - [Hoisted expression with the static method and two nested lambdas and two arguments in closure](#hoisted-expression-with-the-static-method-and-two-nested-lambdas-and-two-arguments-in-closure)
    - [Manually composed expression with parameters and closure](#manually-composed-expression-with-parameters-and-closure)
    - [FastExpressionCompiler.LightExpression.Expression vs System.Linq.Expressions.Expression](#fastexpressioncompilerlightexpressionexpression-vs-systemlinqexpressionsexpression)
  - [How it works](#how-it-works)
    - [What's not supported yet](#whats-not-supported-yet)
  - [Diagnostics](#diagnostics)
  - [Additional optimizations](#additional-optimizations)


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


## Difference between FastExpressionCompiler and FastExpressionCompiler.LightExpression

FastExpressionCompiler

- Provides the `CompileFast` extension methods for the `System.Linq.Expressions.LambdaExpression`.

FastExpressionCompiler.LightExpression

- Provides the `CompileFast` extension methods for `FastExpressionCompiler.LightExpression.LambdaExpression`.
- Provides the drop-in [Expression replacement](#feclightexpressionexpression-vs-expression) with the faster construction and less memory at the cost of less validation.
- Includes its own `ExpressionVisitor`.

Both FastExpressionCompiler and FastExpressionCompiler.LightExpression

- Support `ToExpression` method to convert back to the System Expression.
- Support `ToCSharpString()` method to output the compile-able C# code represented by expression.
- Support `ToExpressionString()` method to output the expression construction C# code, so given the expression object you'll get e.g. `Expression.Lambda(Expression.New(...))`.


## Who's using it

[Marten], [Rebus], [StructureMap], [Lamar], [ExpressionToCodeLib], [NServiceBus]

Considering: [Moq], [LINQ to DB], [Apex.Serialization]


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


## Benchmarks

```ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

```

### Hoisted expression with the constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

|      Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |-----------:|----------:|----------:|------:|--------:|-------:|-------:|-------:|----------:|
|     Compile | 233.935 us | 1.2937 us | 1.1468 us | 47.06 |    0.97 | 0.9766 | 0.4883 |      - |   4.35 KB |
| CompileFast |   4.995 us | 0.0994 us | 0.1184 us |  1.00 |    0.00 | 0.3815 | 0.1907 | 0.0305 |   1.57 KB |


Invoking the compiled delegate (comparing to the direct constructor call):

|                Method |      Mean |     Error |    StdDev | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |----------:|----------:|----------:|------:|-------:|------:|------:|----------:|
| DirectConstructorCall |  5.781 ns | 0.1115 ns | 0.1043 ns |  0.51 | 0.0076 |     - |     - |      32 B |
|        CompiledLambda | 12.581 ns | 0.1318 ns | 0.1169 ns |  1.11 | 0.0076 |     - |     - |      32 B |
|    FastCompiledLambda | 11.338 ns | 0.1075 ns | 0.1005 ns |  1.00 | 0.0076 |     - |     - |      32 B |


### Hoisted expression with the static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

|      Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|------------ |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
|     Compile | 460.63 us | 5.937 us | 5.263 us | 27.47 |    0.67 | 2.4414 | 0.9766 |      - |  11.65 KB |
| CompileFast |  16.77 us | 0.324 us | 0.485 us |  1.00 |    0.00 | 1.1902 | 0.5493 | 0.0916 |   4.86 KB |

Invoking compiled delegate comparing to direct method call:

|              Method |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|    DirectMethodCall |    53.90 ns |  0.982 ns |  0.918 ns |  1.06 |    0.02 | 0.0401 |     - |     - |     168 B |
|     Invoke_Compiled | 1,452.80 ns | 16.283 ns | 15.232 ns | 28.44 |    0.37 | 0.0629 |     - |     - |     264 B |
| Invoke_CompiledFast |    51.11 ns |  0.935 ns |  0.829 ns |  1.00 |    0.00 | 0.0249 |     - |     - |     104 B |


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
|                     Compile | 174.546 us | 3.3113 us | 3.8133 us | 33.86 |    0.99 | 0.9766 | 0.4883 |      - |   4.59 KB |
|                 CompileFast |   5.542 us | 0.1056 us | 0.1037 us |  1.07 |    0.03 | 0.3510 | 0.1755 | 0.0305 |   1.46 KB |
| CompileFast_LightExpression |   5.166 us | 0.0953 us | 0.0892 us |  1.00 |    0.00 | 0.3433 | 0.1678 | 0.0305 |   1.42 KB |


Invoking the compiled delegate compared to the normal delegate and the direct call:

|                             Method |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|                   DirectLambdaCall | 11.07 ns | 0.183 ns | 0.171 ns |  1.02 |    0.02 | 0.0076 |     - |     - |      32 B |
|                     CompiledLambda | 12.31 ns | 0.101 ns | 0.090 ns |  1.13 |    0.01 | 0.0076 |     - |     - |      32 B |
|                 FastCompiledLambda | 10.80 ns | 0.146 ns | 0.137 ns |  1.00 |    0.01 | 0.0076 |     - |     - |      32 B |
| FastCompiledLambda_LightExpression | 10.86 ns | 0.109 ns | 0.096 ns |  1.00 |    0.00 | 0.0076 |     - |     - |      32 B |


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

|                Method |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|      CreateExpression | 3,351.7 ns | 59.81 ns | 55.94 ns | 10.03 |    0.23 | 0.3090 |     - |     - |    1304 B |
| CreateLightExpression |   334.3 ns |  4.85 ns |  4.53 ns |  1.00 |    0.00 | 0.1316 |     - |     - |     552 B |

Creating and compiling:

|                                Method |      Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 |  Gen 2 | Allocated |
|-------------------------------------- |----------:|---------:|---------:|------:|--------:|-------:|-------:|-------:|----------:|
|          CreateExpression_and_Compile | 276.65 us | 5.396 us | 7.565 us | 17.73 |    0.73 | 1.4648 | 0.4883 |      - |   7.01 KB |
|      CreateExpression_and_CompileFast |  21.10 us | 0.418 us | 0.497 us |  1.35 |    0.04 | 1.7700 | 0.8850 | 0.0610 |   7.24 KB |
| CreateLightExpression_and_CompileFast |  15.70 us | 0.306 us | 0.239 us |  1.00 |    0.00 | 1.5564 | 0.7629 | 0.0305 |   6.46 KB |


## How it works

The idea is to provide the fast compilation for the supported expression types
and fallback to the system `Expression.Compile()` for the not supported types:

### What's not supported yet

**FEC V3 does not support yet:** 

- `Quote`
- `Dynamic`
- `RuntimeVariables`
- `DebugInfo`
- `ListInit`
- `MemberInit` with the `MemberMemberBinding` and the `ListMemberBinding` binding types
- `NewArrayInit` multi-dimensional array initializer is not supported yet

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

## Additional optimizations

1. Using `FastExpressionCompiler.LightExpression.Expression` instead of `System.Linq.Expressions.Expression` for the faster expression creation.  
2. Using `.TryCompileWithPreCreatedClosure` and `.TryCompileWithoutClosure` methods when you know the expression at hand and may skip the first traversing round, e.g. for the "static" expression which does not contain the bound constants. __Note:__ You cannot skip the 1st round if the expression contains the `Block`, `Try`, or `Goto` expressions.

---
<a target="_blank" href="https://icons8.com/icons/set/bitten-ice-pop">Bitten Ice Pop icon</a> icon by <a target="_blank" href="https://icons8.com">Icons8</a>
