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

[![Latest Release Notes](https://img.shields.io/badge/latest%20release%20notes-v5.3.1-blue)](https://github.com/dadhi/FastExpressionCompiler/releases/tag/v5.3.1)[![License](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)[![Build Windows,Ubuntu](https://github.com/dadhi/FastExpressionCompiler/actions/workflows/build.yml/badge.svg)](https://github.com/dadhi/FastExpressionCompiler/actions/workflows/build.yml)

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
BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4061/24H2/2024Update/HudsonValley)
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.203
[Host]     : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
DefaultJob : .NET 9.0.4 (9.0.425.16305), X64 RyuJIT AVX2
```

### Hoisted expression with the constructor and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

| Method      |       Mean |     Error |    StdDev | Ratio | RatioSD | Rank |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ----------- | ---------: | --------: | --------: | ----: | ------: | ---: | -----: | -----: | --------: | ----------: |
| CompileFast |   3.183 us | 0.0459 us | 0.0407 us |  1.00 |    0.02 |    1 | 0.1984 | 0.1945 |   1.23 KB |        1.00 |
| Compile     | 147.312 us | 1.9291 us | 1.8946 us | 46.28 |    0.81 |    2 | 0.4883 | 0.2441 |   4.48 KB |        3.65 |

Invoking the compiled delegate (comparing to the direct constructor call):

| Method                |     Mean |     Error |    StdDev | Ratio | RatioSD | Rank |   Gen0 | Allocated | Alloc Ratio |
| --------------------- | -------: | --------: | --------: | ----: | ------: | ---: | -----: | --------: | ----------: |
| DirectConstructorCall | 6.055 ns | 0.0632 ns | 0.0560 ns |  1.00 |    0.01 |    1 | 0.0051 |      32 B |        1.00 |
| CompiledLambda        | 7.853 ns | 0.2013 ns | 0.1681 ns |  1.30 |    0.03 |    2 | 0.0051 |      32 B |        1.00 |
| FastCompiledLambda    | 7.962 ns | 0.2186 ns | 0.4052 ns |  1.31 |    0.07 |    2 | 0.0051 |      32 B |        1.00 |


### Hoisted expression with the static method and two nested lambdas and two arguments in closure

```cs
var a = new A();
var b = new B();
Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

| Method      |      Mean |    Error |   StdDev | Ratio | RatioSD | Rank |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ----------- | --------: | -------: | -------: | ----: | ------: | ---: | -----: | -----: | --------: | ----------: |
| CompileFast |  11.12 us | 0.189 us | 0.158 us |  1.00 |    0.02 |    1 | 0.6104 | 0.5798 |   3.77 KB |        1.00 |
| Compile     | 415.09 us | 4.277 us | 3.571 us | 37.34 |    0.60 |    2 | 1.9531 | 1.4648 |  12.04 KB |        3.19 |

Invoking compiled delegate comparing to direct method call:

| Method              |        Mean |     Error |    StdDev | Ratio | RatioSD | Rank |   Gen0 | Allocated | Alloc Ratio |
| ------------------- | ----------: | --------: | --------: | ----: | ------: | ---: | -----: | --------: | ----------: |
| DirectMethodCall    |    40.29 ns |  0.549 ns |  0.487 ns |  1.00 |    0.02 |    1 | 0.0268 |     168 B |        1.00 |
| Invoke_CompiledFast |    40.59 ns |  0.157 ns |  0.123 ns |  1.01 |    0.01 |    1 | 0.0166 |     104 B |        0.62 |
| Invoke_Compiled     | 1,142.12 ns | 11.877 ns | 14.586 ns | 28.35 |    0.48 |    2 | 0.0420 |     264 B |        1.57 |


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

| Method                       |       Mean |     Error |    StdDev | Ratio | RatioSD | Rank |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| ---------------------------- | ---------: | --------: | --------: | ----: | ------: | ---: | -----: | -----: | --------: | ----------: |
| CompileFast_LightExpression  |   3.107 us | 0.0562 us | 0.0498 us |  0.99 |    0.02 |    1 | 0.1755 | 0.1678 |   1.08 KB |        1.00 |
| CompileFast_SystemExpression |   3.126 us | 0.0288 us | 0.0256 us |  1.00 |    0.01 |    1 | 0.1755 | 0.1678 |   1.08 KB |        1.00 |
| Compile_SystemExpression     | 103.948 us | 1.9593 us | 2.5477 us | 33.26 |    0.84 |    2 | 0.7324 | 0.4883 |   4.74 KB |        4.40 |

Invoking the compiled delegate compared to the normal delegate and the direct call:

| Method                        |     Mean |    Error |   StdDev | Ratio | Rank |   Gen0 | Allocated | Alloc Ratio |
| ----------------------------- | -------: | -------: | -------: | ----: | ---: | -----: | --------: | ----------: |
| DirectCall                    | 10.19 ns | 0.108 ns | 0.085 ns |  1.00 |    1 | 0.0051 |      32 B |        1.00 |
| CompiledFast_LightExpression  | 10.70 ns | 0.089 ns | 0.070 ns |  1.05 |    2 | 0.0051 |      32 B |        1.00 |
| CompiledFast_SystemExpression | 10.91 ns | 0.071 ns | 0.066 ns |  1.07 |    2 | 0.0051 |      32 B |        1.00 |
| Compiled_SystemExpression     | 11.59 ns | 0.098 ns | 0.081 ns |  1.14 |    3 | 0.0051 |      32 B |        1.00 |


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

| Method                  |       Mean |    Error |   StdDev |     Median | Ratio | RatioSD | Rank |   Gen0 | Allocated | Alloc Ratio |
| ----------------------- | ---------: | -------: | -------: | ---------: | ----: | ------: | ---: | -----: | --------: | ----------: |
| Create_LightExpression  |   156.6 ns |  3.19 ns |  8.18 ns |   151.9 ns |  1.00 |    0.07 |    1 | 0.0827 |     520 B |        1.00 |
| Create_SystemExpression | 1,065.0 ns | 14.24 ns | 11.89 ns | 1,069.3 ns |  6.82 |    0.34 |    2 | 0.2060 |    1304 B |        2.51 |

Creating and compiling:

| Method                                  |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD | Rank |   Gen0 |   Gen1 | Allocated | Alloc Ratio |
| --------------------------------------- | ---------: | --------: | --------: | ---------: | ----: | ------: | ---: | -----: | -----: | --------: | ----------: |
| Create_LightExpression_and_CompileFast  |   4.957 us | 0.0986 us | 0.2362 us |   4.913 us |  1.00 |    0.07 |    1 | 0.3510 | 0.3052 |   2.15 KB |        1.00 |
| Create_SystemExpression_and_CompileFast |   6.518 us | 0.1889 us | 0.5541 us |   6.300 us |  1.32 |    0.13 |    2 | 0.4578 | 0.4272 |   2.97 KB |        1.38 |
| Create_SystemExpression_and_Compile     | 205.000 us | 4.0938 us | 7.3819 us | 206.353 us | 41.44 |    2.45 |    3 | 0.9766 | 0.4883 |   7.15 KB |        3.33 |


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

`EnableDelegateDebugInfo` adds the diagnostic info into the compiled delegate including its source Expression and compiled IL code. 

It can be used as following:

```cs
System.Linq.Expressions.Expression<Func<int, Func<int>>> e = 
  n => () => n + 1;
var f = e.CompileFast(flags: CompilerFlags.EnableDelegateDebugInfo);
var d = f.TryGetDebugInfo();
d.PrintExpression();
d.PrintCSharp();
d.PrintIL(); // available in NET8+
```

<details><summary>Expand to see the output of the above code...</summary>


Output of `d.PrintExpression()` is the valid C#:

```cs
var p = new ParameterExpression[1]; // the parameter expressions
var e = new Expression[3]; // the unique expressions
var expr = Lambda<Func<int, Func<int>>>(
  e[0]=Lambda<Func<int>>(
      e[1]=MakeBinary(ExpressionType.Add,
          p[0]=Parameter(typeof(int), "n"),
          e[2]=Constant(1)), new ParameterExpression[0]),
  p[0 // (int n)
      ]);
```

Output of `d.PrintCSharp()` is the valid C#:

```cs
var @cs = (Func<int, Func<int>>)((int n) => //Func<int>
    (Func<int>)(() => //int
        n + 1));
```

Output of `d.PrintIL()` (includes the IL of the nested lambda):

```
<Caller>
0   ldarg.0
1   ldfld object[] ExpressionCompiler.ArrayClosure.ConstantsAndNestedLambdas
6   stloc.0
7   ldloc.0
8   ldc.i4.0
9   ldelem.ref
10  stloc.1
11  ldloc.1
12  ldc.i4.1
13  newarr object
18  stloc.2
19  ldloc.2
20  stfld object[] ExpressionCompiler.NestedLambdaForNonPassedParams.NonPassedParams
25  ldloc.2
26  ldc.i4.0
27  ldarg.1
28  box int
33  stelem.ref
34  ldloc.1
35  ldfld object ExpressionCompiler.NestedLambdaForNonPassedParams.NestedLambda
40  ldloc.2
41  ldloc.1
42  ldfld object[] ExpressionCompiler.NestedLambdaForNonPassedParamsWithConstants.ConstantsAndNestedLambdas
47  newobj ExpressionCompiler.ArrayClosureWithNonPassedParams(System.Object[], System.Object[])
52  call Func<int> ExpressionCompiler.CurryClosureFuncs.Curry(System.Func`2[FastExpressionCompiler.LightExpression.ExpressionCompiler+ArrayClosure,System.Int32], ArrayClosure)
57  ret
</Caller>
<0_nested_in_Caller>
0   ldarg.0
1   ldfld object[] ExpressionCompiler.ArrayClosureWithNonPassedParams.NonPassedParams
6   ldc.i4.0
7   ldelem.ref
8   unbox.any int
13  ldc.i4.1
14  add
15  ret
</0_nested_in_Caller>
```

</details>


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
