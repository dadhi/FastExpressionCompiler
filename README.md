# FastExpressionCompiler

[DryIoc]: https://bitbucket.org/dadhi/dryioc
[ExpressionToCodeLib]: https://github.com/EamonNerbonne/ExpressionToCode

## Summary

Fast ExpressionTree compiler to delegate

More than 10 times faster than `Expression.Compile`:

![benchmark](https://ibin.co/2oAik1nHNy3A.jpg) 

Initially developed and used in [DryIoc] v2 (no closure support). 
Then closure support is added into upcoming DryIoc v3 branch. 

Additionally the version with hoisted closure support was contributed by me to [ExpressionToCodeLib]. 

__Main idea is to evolve and test the compiler separately targeting more projects.__

## Idea

__Compile faster for selected/supported expressions, 
and fallback to `Expression.Compile`for the rest.__ 

Compilation is done by visiting expression nodes and emitting the IL. 
The supporting code preserved as minimalistic as possible for perf. 

Expression visited in two rounds:

- first to collect constants to create closure (for composed expression),
or to find generated closure object (for the hoisted one) 
- second round to actually emit the IL.

If any compilation round visits not supported node, 
the compilation is aborted, and null is returned enabling the fallback. 


## Current state

- the source files are copied from other projects and may not compile, though they should not depend on anything specific. 
- no projects,  just sources from [DryIoc] and [ExpressionToCodeLib]
- not tests except some integrated in above projects
- FastExpressionCompiler is a newer version, __but__ without support
for hoisted expression with closure `Expression<Func<T>> e = () => blah`. Only hand composed expressions are supported. 
- Benchmark for perf comparison vs `Expression.Compile`

## To do

__Everything is up-for-grabs__

- Create project and test structure
- Add CI
- Add unit tests
- Combine compilers for hoisted and composed expressions
- Benchmark and improve performance even more
