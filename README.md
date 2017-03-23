# FastExpressionCompiler

[DryIoc]: https://bitbucket.org/dadhi/dryioc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[Expression Tree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/FastExpressionCompiler.svg)]()
[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)]()

## Why

[Expression tree] compilation is used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But the performance of compilation with `Expression.Compile()` is just slow, 
Moreover, the compiled delegate may be slower than manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> The question is, why is the compiled delegate way slower than a manually-written delegate? Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sandboxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

Fast Expression Compiler is ~20 times faster than `Expression.Compile()`,  
and the result compiled delegate _may be_ ~10 times faster than the one produced by `Expression.Compile()`. 

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

 | Method |        Mean |    StdDev |
 |------- |------------ |---------- |
 |   Expr | 366.8057 us | 2.6807 us |
 |   Fast |  12.3820 us | 0.3382 us |

Invoking compiled delegate with direct constructor call as baseline:

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

 | Method |        Mean |    StdDev |
 |------- |------------ |---------- |
 |   Expr | 686.9673 us | 7.7669 us |
 |   Fast |  33.5210 us | 0.1899 us |


Invoking compiled delegate with direct method call as baseline:

 |             Method |          Mean |     StdDev | Scaled | Scaled-StdDev |
 |------------------- |-------------- |----------- |------- |-------------- |
 |             Method |   144.8640 ns |  2.6944 ns |   1.00 |          0.00 |
 | ExprCompiledLambda | 2,275.7026 ns | 49.6164 ns |  15.71 |          0.44 |
 | FastCompiledLambda |   136.2695 ns |  2.4605 ns |   0.94 |          0.02 |


## Current state

Initially developed and used in [DryIoc] since v2.  
Additinally, contributed to [ExpressionToCodeLib] project.

Supports:

- Manually created or hoisted lambda expressions __with closure__
- Nested lambdas
- Constructor and method calls, lambda invocation
- Property and member access, operators
- and pretty much all from .NET 3.5 Expression Trees

Does not support now, but may be added later:

- Code blocks, assignments and whatever added since .NET 4.0

## How

The idea is to provide fast compilation of selected/supported expression types,
and fall back to normal `Expression.Compile()` for the not (yet) supported types.

Compilation is done by visiting expression nodes and __emitting the IL__. 
The supporting code preserved as minimalistic as possible for perf. 

Expression is visited in two rounds:

1. To collect constants and nested lambdas into closure(s) for manually composed expression,
or to find generated closure object (for the hoisted expression) 
2. To emit the IL.

If any round visits not supported expression node, 
the compilation is aborted, and null is returned enabling the fallback to normal `Expression.Compile()`.
