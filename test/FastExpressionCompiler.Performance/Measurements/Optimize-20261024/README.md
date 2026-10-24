# Current-master performance optimization measurements

Measured source: master `f49acef0d83db3dd2b1059d7fda1a5107ee4eb3a`, after #558 `593c72e23a560c68f973fd238d8376b1b37b38af`, after #559 `ef5779104477c75300edc88eecd523c9d2d377b9`. The #558 snapshot includes the local merge of current master; these are frozen local branch snapshots, not a claim that GitHub has already been updated. No historical original-parent baseline is used.

## Results across all three stages

Each ratio below is the descriptive median of 44 workload-specific ratios. Above 1 is slower. These are not pooled application throughput numbers, and independently computed medians need not multiply. Every absolute timing, allocation value and separate process-round median is in [all-workloads.csv](all-workloads.csv) and [summary.json](summary.json).

| Runtime / backend | Phase | #558 / master | #559 / master | #559 / #558 |
| --- | --- | ---: | ---: | ---: |
| 10.0.9 / standard | construct | 1.0040x | 0.9980x | 0.9977x |
| 10.0.9 / standard | compile | 1.1379x | 1.0988x | 0.9780x |
| 10.0.9 / standard | invoke | 0.9955x | 0.9975x | 1.0005x |
| 10.0.9 / light | construct | 1.0074x | 0.9979x | 0.9973x |
| 10.0.9 / light | compile | 1.1017x | 1.0817x | 0.9911x |
| 10.0.9 / light | invoke | 0.9934x | 0.9937x | 1.0000x |
| 8.0.28 / standard | construct | 1.0019x | 0.9943x | 0.9910x |
| 8.0.28 / standard | compile | 1.1132x | 1.0861x | 0.9777x |
| 8.0.28 / standard | invoke | 0.9966x | 0.9948x | 0.9970x |
| 8.0.28 / light | construct | 0.9985x | 0.9885x | 0.9876x |
| 8.0.28 / light | compile | 1.0984x | 1.0869x | 0.9932x |
| 8.0.28 / light | invoke | 0.9929x | 0.9903x | 1.0001x |

The retained changes improve selected workloads, but do not establish compilation parity with master across this workload set. Round-specific medians, faster/slower workload counts and allocation counts remain in [aggregates.json](aggregates.json). Small timing differences in unchanged construction or identical generated delegate code are measurement/JIT/layout variation, not evidence of a code change to those paths.

## Managed allocations

Counts below are lower / same / higher B/op across the 44 workloads, comparing final #559 with the indicated stage. Differences of at most 0.5 B/op are classified as fractional measurement residue; exact unrounded data is retained. This measures managed allocation on the benchmark thread, not retained memory, native allocations or JIT memory.

| Runtime / backend | Phase | #559 vs master: lower / same / higher | #559 vs #558: lower / same / higher |
| --- | --- | ---: | ---: |
| 10.0.9 / standard | construct | 0 / 44 / 0 | 1 / 43 / 0 |
| 10.0.9 / standard | compile | 14 / 30 / 0 | 0 / 44 / 0 |
| 10.0.9 / standard | invoke | 1 / 43 / 0 | 0 / 44 / 0 |
| 10.0.9 / light | construct | 8 / 36 / 0 | 8 / 36 / 0 |
| 10.0.9 / light | compile | 16 / 28 / 0 | 2 / 42 / 0 |
| 10.0.9 / light | invoke | 1 / 43 / 0 | 0 / 44 / 0 |
| 8.0.28 / standard | construct | 1 / 43 / 0 | 1 / 43 / 0 |
| 8.0.28 / standard | compile | 14 / 30 / 0 | 0 / 44 / 0 |
| 8.0.28 / standard | invoke | 1 / 43 / 0 | 0 / 44 / 0 |
| 8.0.28 / light | construct | 8 / 36 / 0 | 8 / 36 / 0 |
| 8.0.28 / light | compile | 16 / 28 / 0 | 2 / 42 / 0 |
| 8.0.28 / light | invoke | 1 / 43 / 0 | 0 / 44 / 0 |

These allocation counts describe the observations. Standard tree-construction code is unchanged: its isolated lower-allocation controls (56 or 72 B/op in individual configurations) are unexplained control variation, not attributed to a production optimization.

## Retained production commits

### `5e3dd5b0`: named LightExpression field lookup

The existing optimization resolves a requested field directly with `GetDeclaredField`, retaining special-metadata handling. This avoids allocating an array of all declared fields during tree construction. It changes expression construction, rather than parsing text or emitted delegate code. Fresh three-stage measurements for the selected affected shapes:

| Runtime / Light shape | Construction ns: master / #558 / #559 | Construction B/op: master / #558 / #559 |
| --- | ---: | ---: |
| 10.0.9 / new_member | 121.1 / 122.7 / 107.5 | 304.01 / 304.01 / 272.01 |
| 10.0.9 / capture_reference | 105.8 / 119.2 / 96.2 | 368.02 / 368.02 / 336.01 |
| 10.0.9 / capture_mutable_reference_ctor | 194.8 / 193.9 / 176.6 | 704.03 / 704.03 / 672.03 |
| 8.0.28 / new_member | 116.3 / 120.8 / 108.6 | 304.01 / 304.01 / 272.01 |
| 8.0.28 / capture_reference | 105.2 / 106.3 / 97.8 | 368.01 / 368.01 / 336.01 |
| 8.0.28 / capture_mutable_reference_ctor | 191.3 / 190.7 / 181.9 | 704.03 / 704.03 / 672.03 |

### `fab2d6bd`: return no-argument-constructor signature arrays

This fixes the pre-existing issue [#563](https://github.com/dadhi/FastExpressionCompiler/issues/563), verified in current master and pre-#558 code. A lambda constructing a class with no constructor arguments can still have unused lambda parameters. The root fast path rented its signature but did not return it. `CompileNoArgsNew` now returns the signature after delegate creation, and the nested caller no longer returns it a second time. Zero-parameter lambdas use the shared signature and are unaffected. No new cache, context-layout change or emitted-IL change.

The isolated before/after experiment compares `201db040` with this fix, separate from the requested three-stage comparison. Two opposite-order process rounds, seven samples each, 1,000 warmups, 2,000 compilations/sample. The regression alternates parameter signatures and re-invokes earlier delegates under default, no-inlining and debug-info flags.

| Runtime / Light shape | Compilation ns: before / fixed | Compilation B/op: before / fixed |
| --- | ---: | ---: |
| 10.0.9 / ctor0 | 538.7 / 538.0 | 896.00 / 896.00 |
| 10.0.9 / ctor1 | 578.2 / 565.0 | 944.00 / 904.00 |
| 10.0.9 / ctor2 | 647.3 / 611.7 | 960.00 / 912.00 |
| 10.0.9 / add-control | 663.2 / 636.3 | 872.08 / 872.08 |
| 4.0.30319.42000 / ctor0 | 8122.7 / 8097.6 | 1184.00 / 1184.00 |
| 4.0.30319.42000 / ctor1 | 9191.1 / 8581.7 | 1232.00 / 1192.00 |
| 4.0.30319.42000 / ctor2 | 9009.4 / 8755.9 | 1248.00 / 1200.00 |
| 4.0.30319.42000 / add-control | 10441.1 / 10215.9 | 1208.16 / 1208.16 |
| 8.0.28 / ctor0 | 576.1 / 555.6 | 896.00 / 896.00 |
| 8.0.28 / ctor1 | 627.0 / 632.6 | 944.00 / 904.00 |
| 8.0.28 / ctor2 | 650.2 / 691.7 | 960.00 / 912.00 |
| 8.0.28 / add-control | 657.8 / 655.8 | 872.08 / 872.08 |

The one- and two-unused-parameter signatures save 40 and 48 B/compilation respectively on these hosts. Compilation timing is mixed, including slower .NET 8 samples; no universal timing improvement is claimed. Invocation allocation is unchanged. Construction, compilation and invocation of `ctor0`, `ctor1` and `ctor2` are also present in the fresh three-stage dataset.

### `ef577910`: direct collection of terminal binary operands

The generic binary collector calls the existing terminal collector directly for parameter and constant left operands. It avoids entering the large recursive collector for these leaves. Nested-lambda capture information and Light intrinsic callbacks still pass through the existing terminal helper. Scope restoration and emission are unchanged.

The isolated confirmation compares the immediately preceding commit `fab2d6bd` with the candidate retained in `ef577910`: 41 workloads, all three phases, 21 alternating samples/configuration. Compilation allocation matches exactly in all 164 workload/configuration combinations. Selected results:

| Runtime / backend / shape | Compilation ns: before / fixed | Change |
| --- | ---: | ---: |
| 10.0.9 / standard / balanced256 | 24516.8 / 23877.7 | -2.6% |
| 10.0.9 / standard / blocks32 | 17130.3 / 15254.9 | -10.9% |
| 10.0.9 / standard / blocks128 | 81273.6 / 76354.5 | -6.1% |
| 10.0.9 / light / balanced256 | 22794.3 / 21284.5 | -6.6% |
| 10.0.9 / light / blocks32 | 13236.4 / 12017.3 | -9.2% |
| 10.0.9 / light / blocks128 | 63065.0 / 60596.5 | -3.9% |
| 8.0.28 / standard / balanced256 | 25819.6 / 24036.3 | -6.9% |
| 8.0.28 / standard / blocks32 | 16689.6 / 15507.9 | -7.1% |
| 8.0.28 / standard / blocks128 | 76210.3 / 72331.7 | -5.1% |
| 8.0.28 / light / balanced256 | 22660.8 / 21392.0 | -5.6% |
| 8.0.28 / light / blocks32 | 13687.7 / 12327.6 | -9.9% |
| 8.0.28 / light / blocks128 | 66156.8 / 62022.1 | -6.2% |

The median compilation ratio across all 41 workloads improves about 0.6-1.0%, with mixed individual controls; selected gains are not a claim that every workload got faster. Construction and invocation controls are preserved in [terminal-operand-fix.json](terminal-operand-fix.json).

## Runtime and emitted instructions

The corrected check compares **actual instruction strings**, using `ToILString().ToString()`, for all 44 root delegates per backend/runtime, plus returned nested delegates for closure-factory workloads. It does not compare serialized `StringBuilder` properties.

**176 / 176** root-plus-nested instruction bundles match exactly between #558 and #559, without opcode normalization. 48 bundles include a returned nested delegate. See [il-comparison.json](il-comparison.json); full instruction strings are in the raw run logs. This supports unchanged emitted code in these measured cases, rather than a proof about every possible tree or JIT-generated machine code. Warm invocation timing and allocation remain independently measured.

The earlier Rebase-20261024 IL assertion was invalid because its JSON serializer recorded builder metadata. That report is corrected and its raw historical evidence retained.

## Compilation plus first invocation

Compile-only timing has a different JIT boundary for exception regions. #558 uses the supported anonymous dynamic-method host to preserve System-compatible non-Exception catch matching. Its `restrictedSkipVisibility: true` constructor performs accessibility checking and JIT during `CreateDelegate`. Master's owned host can defer that work until invocation. Identical emitted instructions therefore do not imply equal work inside the compile-only timing boundary. Returning to the old host would undo correctness; it is not retained as an optimization. The existing `NonExceptionThrowPolicyRegressionTests` cover raw/wrapped payloads, filters, private access and explicit compilation paths.

A separate matched three-stage pass measures compilation **plus the first invocation of each newly created delegate**, and invokes the returned inner delegate for closure factories. The expression compiler/process is warm; each generated delegate is new, so this includes its first JIT where deferred. This is not a cold-process startup measurement. Same runtimes, backends, 44 workloads, two opposite-order rounds, seven samples each: 7,392 FEC observations plus 1,232 separately calibrated unchanged System observations. Raw timings and managed allocations are in [first-invocation.csv](first-invocation.csv) and [first-invocation.json](first-invocation.json).

| Runtime / backend | #558 / master | #559 / master | #559 / #558 |
| --- | ---: | ---: | ---: |
| 8.0.28 / standard | 1.0121x | 1.0148x | 1.0141x |
| 8.0.28 / light | 1.0080x | 1.0124x | 0.9995x |
| 10.0.9 / standard | 1.0064x | 1.0025x | 1.0017x |
| 10.0.9 / light | 1.0014x | 1.0012x | 1.0006x |

These are descriptive medians of per-workload ratios, not an aggregate application result. The 32-deep exception-region outlier illustrates the timing boundary; absolute microseconds follow:

| Runtime / backend / nested_eh32 | Compile only: master / #558 / #559, us | Compile + first invoke: master / #558 / #559, us |
| --- | ---: | ---: |
| 8.0.28 / standard | 5.57 / 62.80 / 62.42 | 64.10 / 64.90 / 64.45 |
| 8.0.28 / light | 7.60 / 67.69 / 68.42 | 66.61 / 68.52 / 68.47 |
| 10.0.9 / standard | 5.53 / 69.12 / 69.10 | 72.68 / 73.01 / 74.08 |
| 10.0.9 / light | 6.86 / 72.78 / 73.54 | 76.85 / 76.74 / 77.25 |

Separate System reference for this end-to-end boundary (ratios above 1 mean System took longer):

| Runtime / backend | Median System / #559 | Lowest System / #559 (workload) |
| --- | ---: | ---: |
| 8.0.28 / standard | 1.20x | 0.62x (arithmetic64) |
| 8.0.28 / light | 1.24x | 0.50x (arithmetic64) |
| 10.0.9 / standard | 1.00x | 0.62x (arithmetic64) |
| 10.0.9 / light | 1.05x | 0.52x (arithmetic64) |

System is faster in some end-to-end cases; the complete data retains them. Neither the compile-only ratio nor the end-to-end ratio should be read as a universal compiler or delegate-runtime guarantee.

## Unchanged System compiler reference

System is measured separately once per runtime, pooling two process rounds. It has **no before/after columns**, since these PRs do not change it. Its own operation count is calibrated independently; it is not interleaved into the matched-count FEC runs. The standard-tree reference is also a behavior oracle for all FEC versions. Selected fresh compilation measurements follow:

| Runtime / workload | System ns | #559 Standard ns | #559 Light ns | System / #559 Standard | System / #559 Light |
| --- | ---: | ---: | ---: | ---: | ---: |
| 10.0.9 / add | 10421.1 | 1120.5 | 1150.8 | 9.30x | 9.06x |
| 10.0.9 / new_member | 15078.3 | 1257.7 | 1217.8 | 11.99x | 12.38x |
| 10.0.9 / balanced256 | 225884.9 | 23948.8 | 22227.2 | 9.43x | 10.16x |
| 10.0.9 / capture8 | 162329.2 | 7630.5 | 7153.0 | 21.27x | 22.69x |
| 8.0.28 / add | 8914.5 | 1222.6 | 1064.2 | 7.29x | 8.38x |
| 8.0.28 / new_member | 16377.9 | 1334.4 | 1289.3 | 12.27x | 12.70x |
| 8.0.28 / balanced256 | 224314.8 | 24199.7 | 23420.8 | 9.27x | 9.58x |
| 8.0.28 / capture8 | 213128.5 | 7845.0 | 6897.0 | 27.17x | 30.90x |

The complete System comparison is in the per-workload data. Compiler speed ratios do not imply the same ratios for generated delegate execution. The separate reference has no PR-stage delta. Light arithmetic64 compilation is slower than this System reference on both runtimes (about 21% on .NET 8 and 10% on .NET 10), so FEC is not universally faster. Some other cases are close enough to parity that timing variation matters.

## Method, scope and validation

Windows ARM64, SDK 10.0.301, .NET 8.0.28 and 10.0.9, Release, tiering and ReadyToRun disabled. Original production assemblies are loaded unchanged into separate noncollectible AssemblyLoadContexts. The timed boundary uses shared BCL delegates, not reflection. Snapshots, binaries and sources are hashed.

FEC: 44 common workloads, three versions, three phases, two process rounds in reversed configuration/version order, seven samples/round; 22,176 raw observations. Samples rotate/reverse revision order inside each process and use a common operation count per workload/phase. Warmup is 1,000 operations; calibration targets 15 ms, with documented caps in the host. System: 3,696 independent observations. No correctness tests or other benchmark processes run concurrently. No CPU affinity or isolation from unrelated machine activity is claimed.

Construction builds backend-native expression trees (including applicable reflection lookups); compilation uses a prebuilt tree and includes delegate creation; invocation uses warmed generated delegates. Closure-factory invocation includes activation and invoking the returned delegate. The broad pass measures warmed invocation; the separate compile-plus-first-invocation pass adds newly generated delegate JIT where deferred. No cold-process startup, Framework-wide performance, x64 performance or native-memory claim. The separate constructor experiment includes a net472 target on Framework runtime 4.0.30319.42000.

The 41 existing common shapes are retained and augmented with three constructor shapes. Six previously non-common feature shapes (quotes, runtime variables, byref capture and struct captures) are not timed in this comparison; their correctness coverage remains in the tests. The report does not reinterpret excluded correctness tests as passes.

Validated production/test source `ef577910`: both production projects build across configured targets; .NET 10 / .NET 8 native runners pass 1,718 / 1,717 legacy tests, respectively. Framework passes 1,051 TestX plus 1,601 legacy tests. All 21 imported batches complete on their first attempt: **35,064 executed passes, zero executed failures/crashes/timeouts**, with the existing exclusions unchanged. The deferred deep-tree case stays deferred.

## Candidates not retained

A larger terminal/nonterminal collector split had mixed results, including a slower .NET 10 Standard capture control. A separate nested-lambda-context extraction also had mixed results, including substantially slower new_member and exception-region controls; it was not retained. The frame hypothesis did not explain the exception-host JIT timing boundary. Its patch and screening data remain in the evidence archive; it is not part of the production changes. No new global cache, context-layout change, extra whole-tree pass or expression rewriting was introduced.

## Reproduction and evidence

`evidence.zip` includes the pinned source snapshots, standalone harness projects, build/measurement/analysis scripts, raw samples, actual instruction text, per-fix experiments and full validation logs. Binaries are hashed but not committed. Extract into a fresh directory: `Build-Stack.py <after559-sha>` exports the snapshots from the configured repository; `Measure-Stack.py` verifies hashes before measuring; `Summarize-Stack.py` analyzes them. The scripts intentionally refuse/reject mixed or incomplete runs. Paths point to the documented local repository and can be changed for another checkout. Preserve archived outputs before a replay.
