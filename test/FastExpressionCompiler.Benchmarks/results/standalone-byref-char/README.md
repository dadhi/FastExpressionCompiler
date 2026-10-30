# Preserve both bytes of ref char reads and writes

Use ldind.u2/stind.i2 for char managed references. The byte-sized opcodes currently truncate non-ASCII reads and leave the old high byte behind on writes.

Two production opcode replacements. Two regression methods per expression flavor cover all read/write combinations of eight boundary values, including surrogate code units.

Independent extraction from #558, based directly on `dadhi/master` `6702eab6`. It requires neither #558 nor either of the other standalone fixes. All six merge orders of the three standalone fixes were checked without conflicts. #558 is unchanged.

## Validation

Focused regressions: **4 methods passed per runtime**, covering Standard and LightExpression on .NET 8, .NET 10, and .NET Framework 4.7.2. Loading the untouched master compiler with the same regression assembly reproduces the failure on .NET 8. All three Release solution builds pass with zero warnings/errors.

Full .NET 8/.NET 10 native runners were also executed. Each reports the same existing failure in `LightExpressionTests.Binary_operators_with_methods_and_nullable_operands_are_the_same_as_system` (two nullable MultiplyChecked rows produce OverflowException). The identical failure reproduces on untouched master; no new full-run failures were observed. No tests are skipped or weakened to obtain this result.

## Performance and allocations

Windows ARM64, Release, SDK 10.0.301. Before and after compiler assemblies are loaded in separate AssemblyLoadContexts in the same process. Nine alternating-order samples per phase, common operation counts, approximately 35 ms per side, warmed delegates, tiering disabled, and the process pinned to one available logical CPU. GC runs outside samples and a no-GC region isolates allocation throughput. Compilation reuses prebuilt expression trees; execution reuses compiled delegates. This measures CompileFast and delegate invocation, not tree construction. Ratios for previously incorrect/invalid cases are intentionally not claimed.

Allocation counts are unchanged on every shared workload measured, for both compilation and invocation. The raw timing samples and all unchanged controls are retained: small timing differences and code-placement/host variation do not establish universal throughput parity. These are correctness fixes with no new runtime helper calls or storage allocations; no broad performance improvement is claimed.

| Runtime / flavor / workload | Compile ns, master → fix | Invoke ns, master → fix | Compile B/op | Invoke B/op |
| --- | ---: | ---: | ---: | ---: |
| net8.0 / Standard / ref_char_read | 1093.2 → 1104.5 | 1.87 → 1.83 | 872 → 872 | 0 → 0 |
| net8.0 / Standard / ref_char_write | 1250.6 → 1301.6 | 1.86 → 1.92 | 880 → 880 | 0 → 0 |
| net8.0 / Light / ref_char_read | 1526.0 → 1617.0 | 1.89 → 2.02 | 872 → 872 | 0 → 0 |
| net8.0 / Light / ref_char_write | 1452.0 → 1273.2 | 1.88 → 1.90 | 880 → 880 | 0 → 0 |
| net10.0 / Standard / ref_char_read | 1044.4 → 1144.6 | 1.87 → 1.87 | 872 → 872 | 0 → 0 |
| net10.0 / Standard / ref_char_write | 1259.3 → 1341.7 | 1.84 → 1.87 | 880 → 880 | 0 → 0 |
| net10.0 / Light / ref_char_read | 988.6 → 965.8 | 1.91 → 1.87 | 872 → 872 | 0 → 0 |
| net10.0 / Light / ref_char_write | 1214.5 → 1201.1 | 1.96 → 1.98 | 880 → 880 | 0 → 0 |

No affected row has a paired-sample bootstrap interval wholly above parity. These descriptive intervals, stored with the raw data, are based on nine temporal samples and are not a universal performance guarantee.

Raw samples, harness sources, source hashes and validation logs are committed in `test/FastExpressionCompiler.Benchmarks/results/standalone-byref-char/evidence.zip`. Earlier separate-process timing attempts had unstable unchanged controls; their raw results are retained as exploratory evidence and are not the final paired comparison.
