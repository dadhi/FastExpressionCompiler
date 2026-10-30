# Discard non-void list initializer Add results

Discard each non-void Add return value during list initialization and discard the final collection when its enclosing expression ignores the result. HashSet and custom non-void Add methods currently leave invalid values on the evaluation stack.

A small change confined to the existing list emitter, including reuse of adjacent Add-method signatures. Five regression methods per expression flavor cover HashSet, mutable struct collections, ignored results, side-effect order, subsequent member access, and alternating overloads with different return/argument types.

Independent extraction from #558, based directly on `dadhi/master` `6702eab6`. It requires neither #558 nor either of the other standalone fixes. All six merge orders of the three standalone fixes were checked without conflicts. #558 is unchanged.

## Validation

Focused regressions: **10 methods passed per runtime**, covering Standard and LightExpression on .NET 8, .NET 10, and .NET Framework 4.7.2. Loading the untouched master compiler with the same regression assembly reproduces the failure on .NET 8. All three Release solution builds pass with zero warnings/errors.

Full .NET 8/.NET 10 native runners were also executed. Each reports the same existing failure in `LightExpressionTests.Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system` (two nullable MultiplyChecked rows produce OverflowException). The identical failure reproduces on untouched master; no new full-run failures were observed. No tests are skipped or weakened to obtain this result.

## Performance and allocations

Windows ARM64, Release, SDK 10.0.301. Before and after compiler assemblies are loaded in separate AssemblyLoadContexts in the same process. Nine alternating-order samples per phase, common operation counts, approximately 35 ms per side, warmed delegates, tiering disabled, and the process pinned to one available logical CPU. GC runs outside samples and a no-GC region isolates allocation throughput. Compilation reuses prebuilt expression trees; execution reuses compiled delegates. This measures CompileFast and delegate invocation, not tree construction. Ratios for previously incorrect/invalid cases are intentionally not claimed.

Compilation allocations decrease for the repeated Add-method control; invocation allocations are unchanged. No shared workload has an allocation increase. The raw timing samples and all unchanged controls are retained: small timing differences and code-placement/host variation do not establish universal throughput parity. These are correctness fixes with no new runtime helper calls or storage allocations; no broad performance improvement is claimed.

| Runtime / flavor / workload | Compile ns, master → fix | Invoke ns, master → fix | Compile B/op | Invoke B/op |
| --- | ---: | ---: | ---: | ---: |
| net8.0 / Standard / void_add_list_control | 1753.9 → 1635.6 | 22.91 → 17.80 | 1240 → 1176 | 72 → 72 |
| net8.0 / Light / void_add_list_control | 1432.3 → 1322.8 | 11.53 → 11.41 | 1240 → 1176 | 72 → 72 |
| net10.0 / Standard / void_add_list_control | 1490.5 → 1618.3 | 12.07 → 10.88 | 1240 → 1176 | 72 → 72 |
| net10.0 / Light / void_add_list_control | 1895.7 → 1663.5 | 10.02 → 8.57 | 1240 → 1176 | 72 → 72 |

The table uses the already-valid void-Add List<int> control. Its emitted return-value IL is unchanged. The broken non-void path has no valid before/after runtime ratio; it now passes the correctness checks. The standalone pilot records also include the fixed-only HashSet workload. An initial version checked ReturnType for every initializer and cost compilation time. This version reuses the last Add-method signature, avoiding repeated GetParameters arrays and ReturnType lookups. Alternating overloads are covered by a separate regression.

Raw samples, harness sources, source hashes and validation logs are committed in `test/FastExpressionCompiler.Benchmarks/results/standalone-list-initializer/evidence.zip`. Earlier separate-process timing attempts had unstable unchanged controls; their raw results are retained as exploratory evidence and are not the final paired comparison.
