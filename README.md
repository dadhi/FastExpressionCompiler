# FastExpressionCompiler

[DryIoc]: https://bitbucket.org/dadhi/dryioc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode
[Expression Tree]: https://msdn.microsoft.com/en-us/library/mt654263.aspx

## Why

[Expression tree] compilation used by wide range of tools, e.g. IoC/DI containers, Serializers, OO Mappers.
But the performance of compilation with `Expression.Compile()` is just slow, 
Moreover, the resulting compiled delegate may be slower than manually created delegate because of the [reasons](https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/):

_TL;DR;_
> The question is, why is the compiled delegate way slower than a manually-written delegate? Expression.Compile creates a DynamicMethod and associates it with an anonymous assembly to run it in a sandboxed environment. This makes it safe for a dynamic method to be emitted and executed by partially trusted code but adds some run-time overhead.

Fast Expression Compiler is ~20 times faster than `Expression.Compile()`,  
and the result delegate _may be_ ~10 times faster than one produced by `Expression.Compile()`. 

![benchmark](https://ibin.co/2oAik1nHNy3A.jpg)

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
