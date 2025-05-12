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

[![Latest Release Notes](https://img.shields.io/badge/latest%20release%20notes-v5.2.0-blue)](https://github.com/dadhi/FastExpressionCompiler/releases/tag/v5.2.0)[![License](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)[![Build Windows,Ubuntu](https://github.com/dadhi/FastExpressionCompiler/actions/workflows/build.yml/badge.svg)](https://github.com/dadhi/FastExpressionCompiler/actions/workflows/build.yml)

Targets .NET 6+, .NET 4.7.2+, .NET Standard 2.0+

NuGet packages:

- FastExpressionCompiler [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler)
  * sources package: FastExpressionCompiler.src [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler.src)](https://www.nuget.org/packages/FastExpressionCompiler.src)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler.src)
  * sources with the public code made internal: FastExpressionCompiler.Internal.src [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler.Internal.src)](https://www.nuget.org/packages/FastExpressionCompiler.Internal.src)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler.Internal.src)
- FastExpressionCompiler.LightExpression [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler.LightExpression)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler.LightExpression)  
  * sources package: FastExpressionCompiler.LightExpression.src [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler.LightExpression.src)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression.src)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler.LightExpression.src)  
  * sources with the public code made internal: FastExpressionCompiler.LightExpression.Internal.src [![NuGet Version](https://img.shields.io/nuget/v/FastExpressionCompiler.LightExpression.Internal.src)](https://www.nuget.org/packages/FastExpressionCompiler.LightExpression.Internal.src)![NuGet Downloads](https://img.shields.io/nuget/dt/FastExpressionCompiler.LightExpression.Internal.src)

The project was originally a part of the [DryIoc], so check it out ;-)

## The problem

[ExpressionTree] compilation is used by the wide variety of tools, e.g. IoC/DI containers, Serializers, ORMs and OOMs.
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

**Updated to .NET 9.0**

```ini
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4391/23H2/2023Update/SunValley3)
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.100
  [Host]     : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
```

### Hoisted expression with the constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

| Method      |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ----------- | ---------: | --------: | --------: | ----: | ------: | -----: | -----: | --------: | ----------: |
| Compile     | 151.570 us | 3.0196 us | 6.7538 us | 44.27 |    2.13 | 0.7324 |      - |   4.49 KB |        2.92 |
| CompileFast |   3.425 us | 0.0676 us | 0.0664 us |  1.00 |    0.03 | 0.2441 | 0.2365 |   1.54 KB |        1.00 |

Invoking the compiled delegate (comparing to the direct constructor call):

| Method                |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| --------------------- | -------: | --------: | --------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| DirectConstructorCall | 6.920 ns | 0.2007 ns | 0.3462 ns | 7.051 ns |  0.86 |    0.06 | 0.0051 |      32 B |        1.00 |
| CompiledLambda        | 8.095 ns | 0.2195 ns | 0.5216 ns | 7.845 ns |  1.01 |    0.08 | 0.0051 |      32 B |        1.00 |
| FastCompiledLambda    | 8.066 ns | 0.2206 ns | 0.3234 ns | 8.156 ns |  1.00 |    0.06 | 0.0051 |      32 B |        1.00 |


### Hoisted expression with the static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

| Method      |      Mean |    Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ----------- | --------: | -------: | --------: | --------: | ----: | ------: | -----: | -----: | --------: | ----------: |
| Compile     | 421.09 us | 8.382 us | 18.221 us | 413.02 us | 36.29 |    2.09 | 1.9531 | 0.9766 |  12.04 KB |        2.61 |
| CompileFast |  11.62 us | 0.230 us |  0.464 us |  11.42 us |  1.00 |    0.06 | 0.7324 | 0.7019 |   4.62 KB |        1.00 |


Invoking compiled delegate comparing to direct method call:

| Method              |        Mean |     Error |    StdDev |      Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| ------------------- | ----------: | --------: | --------: | ----------: | ----: | ------: | -----: | --------: | ----------: |
| DirectMethodCall    |    43.45 ns |  0.922 ns |  1.905 ns |    44.13 ns |  1.09 |    0.08 | 0.0268 |     168 B |        1.62 |
| Invoke_Compiled     | 1,181.25 ns | 23.664 ns | 56.240 ns | 1,161.87 ns | 29.66 |    2.24 | 0.0420 |     264 B |        2.54 |
| Invoke_CompiledFast |    39.96 ns |  0.856 ns |  2.442 ns |    38.96 ns |  1.00 |    0.08 | 0.0166 |     104 B |        1.00 |


### Manually composed expression with parameters and closure

```cs
var a = new A();
var bParamExpr = Expression.Parameter(typeof(B), "b");
var expr = Expression.Lambda(
    Expression.New(_ctorX,
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);
```

Compiling expression:

| Method                       |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ---------------------------- | --------: | --------: | --------: | --------: | ----: | ------: | -----: | -----: | --------: | ----------: |
| Compile_SystemExpression     | 89.076 us | 2.6699 us | 7.6605 us | 85.180 us | 28.12 |    3.05 | 0.7324 | 0.4883 |   4.74 KB |        3.41 |
| CompileFast_SystemExpression |  3.138 us | 0.0550 us | 0.0565 us |  3.118 us |  0.99 |    0.03 | 0.2213 | 0.2136 |   1.39 KB |        1.00 |
| CompileFast_LightExpression  |  3.180 us | 0.0602 us | 0.0591 us |  3.163 us |  1.00 |    0.00 | 0.2213 | 0.2136 |   1.39 KB |        1.00 |


Invoking the compiled delegate compared to the normal delegate and the direct call:

| Method                        |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| ----------------------------- | -------: | --------: | --------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| DirectCall                    | 8.388 ns | 0.2655 ns | 0.7575 ns | 8.092 ns |  1.00 |    0.07 | 0.0051 |      32 B |        1.00 |
| Compiled_SystemExpression     | 9.474 ns | 0.1870 ns | 0.4105 ns | 9.381 ns |  1.10 |    0.05 | 0.0051 |      32 B |        1.00 |
| CompiledFast_SystemExpression | 8.575 ns | 0.1624 ns | 0.1440 ns | 8.517 ns |  1.00 |    0.02 | 0.0051 |      32 B |        1.00 |
| CompiledFast_LightExpression  | 8.584 ns | 0.0776 ns | 0.0862 ns | 8.594 ns |  1.00 |    0.00 | 0.0051 |      32 B |        1.00 |


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

| Method                                 |       Mean |    Error |   StdDev |     Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ---------: | -------: | -------: | ---------: | ----: | ------: | -----: | --------: | ----------: |
| Create_SystemExpression                | 1,110.9 ns | 22.19 ns | 62.23 ns | 1,086.1 ns |  7.25 |    0.56 | 0.2060 |    1304 B |        2.63 |
| Create_LightExpression                 |   153.7 ns |  3.14 ns |  8.61 ns |   150.5 ns |  1.00 |    0.08 | 0.0789 |     496 B |        1.00 |
| Create_LightExpression_with_intrinsics |   161.0 ns |  2.80 ns |  2.19 ns |   161.0 ns |  1.05 |    0.06 | 0.0777 |     488 B |        0.98 |

Creating and compiling:

| Method                                               |       Mean |     Error |     StdDev | Ratio | RatioSD |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ---------------------------------------------------- | ---------: | --------: | ---------: | ----: | ------: | -----: | -----: | --------: | ----------: |
| Create_SystemExpression_and_Compile                  | 212.157 us | 4.2180 us | 11.4036 us | 44.77 |    3.31 | 0.9766 | 0.4883 |   7.15 KB |        2.95 |
| Create_SystemExpression_and_CompileFast              |   6.656 us | 0.1322 us |  0.3065 us |  1.40 |    0.10 | 0.5188 | 0.4883 |   3.27 KB |        1.35 |
| Create_LightExpression_and_CompileFast               |   4.751 us | 0.0947 us |  0.2411 us |  1.00 |    0.07 | 0.3815 | 0.3662 |   2.42 KB |        1.00 |
| CreateLightExpression_and_CompileFast_with_intrinsic |   4.604 us | 0.0918 us |  0.1915 us |  0.97 |    0.06 | 0.3815 | 0.3662 |   2.35 KB |        0.97 |


## Difference between FastExpressionCompiler and FastExpressionCompiler.LightExpression

FastExpressionCompiler

- Provides the `CompileFast` extension methods for the `System.Linq.Expressions.LambdaExpression`.

FastExpressionCompiler.LightExpression

- Provides the `CompileFast` extension methods for `FastExpressionCompiler.LightExpression.LambdaExpression`.
- Provides the drop-in expression replacement with the less consumed memory and the faster construction at the cost of the less validation.
- Includes its own `ExpressionVisitor`.
- Supports `ToExpression` method to convert back **to the** `System.Linq.Expressions.Expression`.
- Supports `ToLightExpression` conversion method to convert **from the** `System.Linq.Expressions.Expression` to `FastExpressionCompiler.LightExpression.Expression`.

Both FastExpressionCompiler and FastExpressionCompiler.LightExpression

- Support `ToCSharpString()` method to output the compilable C# code represented by the expression.
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
    Expression.New(_ctorX,
        Expression.Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var f = expr.CompileFast();
var x = f(new B());
```

__Note:__ You may simplify Expression usage and enable faster refactoring with the C# `using static` statement:

```cs
using static System.Linq.Expressions.Expression;
// or
// using static FastExpressionCompiler.LightExpression.Expression;

var a = new A();
var bParamExpr = Parameter(typeof(B), "b");
var expr = Lambda(
    New(_ctorX, Constant(a, typeof(A)), bParamExpr),
    bParamExpr);

var f = expr.CompileFast();
var x = f(new B());
```


## How it works

The idea is to provide the fast compilation for the supported expression types
and fallback to the system `Expression.Compile()` for the not supported types:

### What's not supported yet

**FEC does not support yet:** 

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


## Diagnostics and Code Generation

FEC V3 has added powerful diagnostics and code generation tools.

### Diagnostics

You may pass the optional `CompilerFlags.EnableDelegateDebugInfo`  into the `CompileFast` methods.

`EnableDelegateDebugInfo` adds the diagnostic info into the compiled delegate including its source Expression and C# code. 
Can be used as following:

```cs
var f = e.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
var di = f.Target as IDelegateDebugInfo;
Asserts.IsNotNull(di.Expression);
Asserts.IsNotNull(di.ExpressionString);
Asserts.IsNotNull(di.CSharpString);
```

### ThrowOnNotSupportedExpression and NotSupported_ flags

FEC V3.1 has added the compiler flag `CompilerFlags.ThrowOnNotSupportedExpression`.
When passed to `CompileFast(flags: CompilerFlags.ThrowOnNotSupportedExpression)` and the expression contains not (yet) supported Expression node the compilation will throw the exception instead of returning `null`.

To get the whole list of the not yet supported cases you may check in `Result.NotSupported_` enum values.


### Code Generation

The Code Generation capabilities are available via the `ToCSharpString` and `ToExpressionString` extension methods.

**Note:** When converting the source expression to either C# code or to the Expression construction code you may find 
the `// NOT_SUPPORTED_EXPRESSION` comments marking the not supported yet expressions by FEC. So you may test the presence or absence of this comment.


## Additional optimizations

1. Using `FastExpressionCompiler.LightExpression.Expression` instead of `System.Linq.Expressions.Expression` for the faster expression creation.  
2. Using `.TryCompileWithPreCreatedClosure` and `.TryCompileWithoutClosure` methods when you know the expression at hand and may skip the first traversing round, e.g. for the "static" expression which does not contain the bound constants. __Note:__ You cannot skip the 1st round if the expression contains the `Block`, `Try`, or `Goto` expressions.

---
<a target="_blank" href="https://icons8.com/icons/set/bitten-ice-pop">Bitten Ice Pop icon</a> icon by <a target="_blank" href="https://icons8.com">Icons8</a>
