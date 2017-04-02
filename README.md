# FastExpressionCompiler

[DryIoc]: https://bitbucket.org/dadhi/dryioc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[Expression Tree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx

[![NuGet Pre Release](https://img.shields.io/nuget/v/FastExpressionCompiler.svg)](https://www.nuget.org/packages/FastExpressionCompiler/1.0.0)
[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)

Supported platforms: __.NET 4.5.2__, __.NET Standard 1.3__

## Why

[Expression tree] compilation is used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But the performance of compilation with `Expression.Compile()` is just slow, 
Moreover, the compiled delegate may be slower than manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> The question is, why is the compiled delegate way slower than a manually-written delegate? Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sandboxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

Fast Expression Compiler is ~20 times faster than `Expression.Compile()`.  
The compiled delegate __may be in some cases???__ ~15 times faster than the one produced by `Expression.Compile()`.

## Benchmarks

```ini
BenchmarkDotNet=v0.10.3.0, OS=Microsoft Windows 10.0.14393
Processor=Intel(R) Core(TM) i5-6300U CPU 2.40GHz, ProcessorCount=4
Frequency=2437493 Hz, Resolution=410.2576 ns, Timer=TSC
dotnet cli version=1.0.0-preview2-1-003177
  [Host]     : .NET Core 4.6.24628.01, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.24628.01, 64bit RyuJIT
```


### Hoisted expression with constructor and two arguments in closure

```csharp
    var a = new A();
    var b = new B();
    Expression<Func<X>> e = () => new X(a, b);
```

Compiling expression:

 | Method      |        Mean |    StdDev |
 |------------ |------------ |---------- |
 | Compile     | 366.8057 us | 2.6807 us |
 | CompileFast |  12.3820 us | 0.3382 us |

Invoking compiled delegate comparing to direct constructor call:

 |              Method |       Mean |    StdDev | Scaled | Scaled-StdDev |
 |-------------------- |----------- |---------- |------- |-------------- |
 |         Constructor |  7.0878 ns | 0.0480 ns |   1.00 |          0.00 |
 |      CompiledLambda | 10.7929 ns | 0.1323 ns |   1.52 |          0.02 |
 |  FastCompiledLambda |  9.6521 ns | 0.1556 ns |   1.36 |          0.02 |
 
 
### Hoisted expression with static method and two nested lambdas and two arguments in closure

```csharp
    var a = new A();
    var b = new B();
    Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

 | Method      |        Mean |    StdDev |
 |------------ |------------ |---------- |
 | Compile     | 686.9673 us | 7.7669 us |
 | CompileFast |  33.5210 us | 0.1899 us |


Invoking compiled delegate comparing to direct method call:

 |             Method |          Mean |     StdDev | Scaled | Scaled-StdDev |
 |------------------- |-------------- |----------- |------- |-------------- |
 |             Method |   144.8640 ns |  2.6944 ns |   1.00 |          0.00 |
 |     CompiledLambda | 2,275.7026 ns | 49.6164 ns |  15.71 |          0.44 |
 | FastCompiledLambda |   136.2695 ns |  2.4605 ns |   0.94 |          0.02 |


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

 | Method      |        Mean |    StdDev |
 |------------ |------------ |---------- |
 | Compile     | 269.9465 us | 3.9580 us |
 | CompileFast |  11.3810 us | 0.1435 us |


Invoking compiled delegate comparing to normal delegate:

 |             Method |       Mean |    StdDev | Scaled | Scaled-StdDev |
 |------------------- |----------- |---------- |------- |-------------- |
 |             Lambda | 12.0089 ns | 0.2025 ns |   1.00 |          0.00 |
 |     CompiledLambda | 12.9169 ns | 0.3836 ns |   1.08 |          0.04 |
 | FastCompiledLambda | 12.6380 ns | 0.2724 ns |   1.05 |          0.03 |


## Usage

Hoisted lambda expression (created for you by compiler):
```chsarp
    var a = new A(); var b = new B();
    Expression<Func<X>> expr = () => new X(a, b);
    var getX = ExpressionCompiler.Compile(expr);
    var x = getX();
```

Manually composed lambda expression:
```chsarp
    var a = new A();
    var bParamExpr = Expression.Parameter(typeof(B), "b");
    var expr = Expression.Lambda(
        Expression.New(typeof(X).GetTypeInfo().DeclaredConstructors.First(),
            Expression.Constant(a, typeof(A)), bParamExpr),
        bParamExpr);
    var getX = ExpressionCompiler.Compile<Func<B, X>>(expr);
    var x = getX(new B());
```


## Status

Initially developed and currently used in [DryIoc].

Additionally contributed to [ExpressionToCodeLib].

v1 supports:

- Manually created or hoisted lambda expressions __with closure__
- Nested lambdas
- Constructor and method calls, lambda invocation
- Property and member access, operators

v1 does not support now:

- `??`, `?.` operators, bitwise operators, ariphmetical operators
- `ulong` and `long` constants
- Code blocks, assignments and whatever added since .NET 4.0

__Note:__ The above limitations may be removed by wrapping not supported epression into method or property.

## How

The idea is to provide fast compilation of selected/supported expression types,
and fall back to normal `Expression.Compile()` for the not (yet) supported types.

Compilation is done by visiting expression nodes and __emitting the IL__. 
The supporting code preserved as minimalistic as possible for perf. 

Expression is visited in two rounds:

1. To collect constants and nested lambdas into closure(s) for manually composed expression,
or to find generated closure object for the hoisted expression
2. To emit the IL.

If the processing round visits not supported expression node, 
the compilation is aborted, and null is returned enabling the fallback to normal `Expression.Compile()`.
