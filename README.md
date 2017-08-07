# FastExpressionCompiler

[DryIoc]: https://bitbucket.org/dadhi/dryioc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[Expression Tree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx

[![NuGet Badge](https://buildstats.info/nuget/FastExpressionCompiler)](https://www.nuget.org/packages/FastExpressionCompiler)
[![license](https://img.shields.io/github/license/dadhi/FastExpressionCompiler.svg)](http://opensource.org/licenses/MIT)

Supported platforms: __.NET 4.5.2+__, __.NET Standard 1.3__

## Why

[Expression tree] compilation is used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But the performance of compilation with `Expression.Compile()` is just slow. 
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

 |                Method |        Mean |     StdDev | Scaled | Scaled-StdDev |  Gen 0 |  Gen 1 | Allocated |
 |---------------------- |------------ |----------- |------- |-------------- |------- |------- |---------- |
 |     ExpressionCompile | 426.1452 us | 10.8108 us |  29.91 |          1.15 |      - |      - |   4.36 kB |
 | ExpressionCompileFast |  14.2593 us |  0.4461 us |   1.00 |          0.00 | 1.0579 | 0.2035 |   2.72 kB |

Invoking compiled delegate comparing to direct constructor call:

 |                Method |       Mean |    StdErr |    StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
 |---------------------- |----------- |---------- |---------- |------- |-------------- |------- |---------- |
 | DirectConstructorCall |  9.7089 ns | 0.1423 ns | 0.5692 ns |   0.70 |          0.04 | 0.0202 |      32 B |
 |        CompiledLambda | 15.8753 ns | 0.2077 ns | 1.2113 ns |   1.15 |          0.09 | 0.0198 |      32 B |
 |    FastCompiledLambda | 13.8102 ns | 0.0963 ns | 0.3473 ns |   1.00 |          0.00 | 0.0195 |      32 B |
 
 
### Hoisted expression with static method and two nested lambdas and two arguments in closure

```csharp
    var a = new A();
    var b = new B();
    Expression<Func<X>> getXExpr = () => CreateX((aa, bb) => new X(aa, bb), new Lazy<A>(() => a), b);
```

Compiling expression:

 |                Method |        Mean |     StdDev | Scaled | Scaled-StdDev |  Gen 0 |  Gen 1 | Allocated |
 |---------------------- |------------ |----------- |------- |-------------- |------- |------- |---------- |
 |     ExpressionCompile | 885.8788 us | 14.5933 us |  17.07 |          0.37 |      - |      - |  12.28 kB |
 | ExpressionFastCompile |  51.9052 us |  0.7952 us |   1.00 |          0.00 | 4.0616 | 1.3761 |      8 kB |


Invoking compiled delegate comparing to direct method call:

 |             Method |          Mean |     StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
 |------------------- |-------------- |----------- |------- |-------------- |------- |---------- |
 |   DirectMethodCall |   166.9818 ns |  3.0175 ns |   0.86 |          0.02 | 0.1111 |     184 B |
 |     CompiledLambda | 2,547.4770 ns | 46.7880 ns |  13.08 |          0.27 | 0.0900 |     280 B |
 | FastCompiledLambda |   194.8093 ns |  2.2769 ns |   1.00 |          0.00 | 0.1399 |     240 B |


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

 |                Method |        Mean |     StdDev |      Median | Scaled | Scaled-StdDev |  Gen 0 |  Gen 1 | Allocated |
 |---------------------- |------------ |----------- |------------ |------- |-------------- |------- |------- |---------- |
 |     CompileExpression | 397.5570 us | 27.6319 us | 386.6312 us |  27.46 |          1.92 |      - |      - |   4.72 kB |
 | CompileFastExpression |  14.4785 us |  0.1752 us |  14.5392 us |   1.00 |          0.00 | 1.3086 | 0.5762 |   2.26 kB |


Invoking compiled delegate comparing to normal delegate:

 |             Method |       Mean |    StdErr |    StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
 |------------------- |----------- |---------- |---------- |------- |-------------- |------- |---------- |
 |          RawLambda | 12.5377 ns | 0.0839 ns | 0.3249 ns |   0.93 |          0.03 | 0.0196 |      32 B |
 |     CompiledLambda | 14.2297 ns | 0.1640 ns | 0.6135 ns |   1.06 |          0.05 | 0.0195 |      32 B |
 | FastCompiledLambda | 13.4183 ns | 0.0352 ns | 0.1317 ns |   1.00 |          0.00 | 0.0195 |      32 B |


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
- Property and member access
- Equality and `?:` operators

v1 does not support:

- `??`, `?.`, bitwise, and ariphmetic operators
- Code blocks, assignments and whatever added since .NET 4.0

__Note:__ The current limitations may be lifted by wrapping not yet supported expression into method or property.


## How

The idea is to provide fast compilation of selected/supported expression types,
and fall back to normal `Expression.Compile()` for the not (yet) supported types.

Compilation is done by visiting expression nodes and __emitting the IL__. 
The supporting code preserved as minimalistic as possible for perf. 

Expression is visited in two rounds:

1. To collect constants and nested lambdas into closure(s) for manually composed expression,
or to find generated closure object for the hoisted expression
2. To emit the IL

If the processing round visits not supported expression node, 
the compilation is aborted, and null is returned enabling the fallback to normal `Expression.Compile()`.
