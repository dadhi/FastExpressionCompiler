# Use unsigned division for uint and ulong expressions

Emit div.un instead of div for unsigned arithmetic. Values above the signed range currently produce incorrect quotients or overflow. The existing arithmetic dispatch and closure layout are unchanged.

One production line. Three regression methods per expression flavor cover uint/ulong, signed-boundary values, divide-by-zero, nullable operands, and compound assignment.

Independent extraction from #558, based directly on `dadhi/master` `6702eab6`. It requires neither #558 nor either of the other standalone fixes. All six merge orders of the three standalone fixes were checked without conflicts. #558 is unchanged.

## Validation

Focused regressions: **6 methods passed per runtime**, covering Standard and LightExpression on .NET 8, .NET 10, and .NET Framework 4.7.2. Loading the untouched master compiler with the same regression assembly reproduces the failure on .NET 8. All three Release solution builds pass with zero warnings/errors.

Full .NET 8/.NET 10 native runners were also executed. Each reports the same existing failure in `LightExpressionTests.Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system` (two nullable MultiplyChecked rows produce OverflowException). The identical failure reproduces on untouched master; no new full-run failures were observed. No tests are skipped or weakened to obtain this result.

## Performance and allocations

Windows ARM64, Release, SDK 10.0.301. Before and after compiler assemblies are loaded in separate AssemblyLoadContexts in the same process. Nine alternating-order samples per phase, common operation counts, approximately 35 ms per side, warmed delegates, tiering disabled, and the process pinned to one available logical CPU. GC runs outside samples and a no-GC region isolates allocation throughput. Compilation reuses prebuilt expression trees; execution reuses compiled delegates. This measures CompileFast and delegate invocation, not tree construction. Ratios for previously incorrect/invalid cases are intentionally not claimed.

Allocation counts are unchanged on every shared workload measured, for both compilation and invocation. The raw timing samples and all unchanged controls are retained: small timing differences and code-placement/host variation do not establish universal throughput parity. These are correctness fixes with no new runtime helper calls or storage allocations; no broad performance improvement is claimed.

| Runtime / flavor / workload | Compile ns, master → fix | Invoke ns, master → fix | Compile B/op | Invoke B/op |
| --- | ---: | ---: | ---: | ---: |
| net8.0 / Standard / uint_divide | 1131.1 → 1170.4 | 2.10 → 1.89 | 880 → 880 | 0 → 0 |
| net8.0 / Standard / ulong_divide | 1317.4 → 1287.6 | 2.63 → 2.26 | 880 → 880 | 0 → 0 |
| net8.0 / Light / uint_divide | 1108.9 → 1091.1 | 2.16 → 1.90 | 880 → 880 | 0 → 0 |
| net8.0 / Light / ulong_divide | 1028.7 → 1119.7 | 1.78 → 1.63 | 880 → 880 | 0 → 0 |
| net10.0 / Standard / uint_divide | 1244.6 → 1207.0 | 2.18 → 1.93 | 880 → 880 | 0 → 0 |
| net10.0 / Standard / ulong_divide | 1336.8 → 1241.1 | 1.87 → 1.56 | 880 → 880 | 0 → 0 |
| net10.0 / Light / uint_divide | 1085.7 → 1034.5 | 2.02 → 1.86 | 880 → 880 | 0 → 0 |
| net10.0 / Light / ulong_divide | 1142.4 → 1061.5 | 1.92 → 1.55 | 880 → 880 | 0 → 0 |

No affected row has a paired-sample bootstrap interval wholly above parity. These descriptive intervals, stored with the raw data, are based on nine temporal samples and are not a universal performance guarantee.

Raw samples, harness sources, source hashes and validation logs are committed in `test/FastExpressionCompiler.Benchmarks/results/standalone-unsigned-division/evidence.zip`. Earlier separate-process timing attempts had unstable unchanged controls; their raw results are retained as exploratory evidence and are not the final paired comparison.
