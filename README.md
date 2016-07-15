# FastExpressionCompiler

## Summary

Fast ExpressionTree compiler to delegate

More than 10 times faster than `Expression.Compile`.

Intially developed and used in DryIoc v2 (no closure support). 
Then closure support is added into upcoming DryIoc v3 branch. 

Additionally the version with hoisted closure support was contributed by me to ExpressionToCodeLib project. 

__Main idea is to evolve and test the compiler separately targeting more projects.__

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
