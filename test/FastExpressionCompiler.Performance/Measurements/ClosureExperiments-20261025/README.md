# Local closure experiments above #559

Branch: `perf/closure-experiments-250-350`. Published as a draft for evaluation above #559; the parent #559 branch is unchanged. These are fresh paired measurements against the current local #559 production code, without previous-branch numbers or parent-history baselines. System is an untimed behavior oracle, not a before/after timing series.

| Stage | Commit |
| --- | --- |
| #559 baseline | 4a933287c45552ec208a24752c8b82fb75470f79 |
| #350 prototype | db7168ff792698092a32eff6ee6f26b84def7cd9 |
| #250 initial prototype | ddfc9e123938f85a376f303298bd2bb2f7e65059 |
| Current pooled revision | 5a0d565c3d0d865d1a05e47272c7ae41cb530f3a |

## Assessment

- #350 is the stronger candidate: a deterministic 24 B saving per created captured delegate and approximately 9% lower root/factory execution geometric means in this profile. Small compile-time and cached-call differences remain noisy; slower individual results are retained.
- #250 is functional but not a general performance improvement. The pooled flat and sibling cases allocate less, while the single nested 64-constant case allocates about 1,200 B more than ordinary #559 compilation. For eight siblings, compile plus first execution is about 19–62% slower than #559 across the four measured configurations. Compile-only timings are dominated by the different JIT boundary and must not be used alone.
- The shared manual target retains unrelated constants after one child escapes; the final pooled revision reproduces the same lifetime result. Keep this path experimental and explicitly selected. The combined draft presents the experiments for review; merging them together or replacing the default path is not recommended yet.

## Issue #350: combine captured storage and delegate target

The open delegate now resides in a typed target deriving from `ArrayClosureWithNonPassedParams`; its `Invoke` passes itself to the open method. This removes the separate Curry display class. Existing open signatures, capture cells and custom-delegate adaptation remain. Modern .NET uses the exact existing open-delegate type; Framework preserves its existing signature choices, including variance where needed. Public constant-array layout is unchanged.

The retained version saves **24 B for each created captured delegate** in every captured workload/configuration measured. Cached invocation allocates the same as before. Compilation allocations also decrease; complete values are in the data. Ratios below are geometric means across 14 equally weighted shapes (above 1 means slower), not application-throughput predictions or significance tests:

| Runtime / API | Compile | Compile + first | Root/factory execution | Cached inner execution | Construction control |
| --- | --- | --- | --- | --- | --- |
| 8.0.28 / standard | 0.976x | 0.947x | 0.903x | 0.989x | 0.984x |
| 8.0.28 / light | 0.990x | 0.909x | 0.909x | 0.989x | 0.964x |
| 10.0.9 / standard | 0.993x | 0.904x | 0.912x | 0.975x | 0.982x |
| 10.0.9 / light | 0.961x | 0.913x | 0.909x | 0.973x | 0.980x |

Root execution for factory shapes creates a returned delegate and invokes it once. Cached-inner execution obtains that delegate once outside timing and calls it repeatedly. Scalar controls use the same root driver in both execution phases.

Timing variation affects unchanged construction and execution controls. No universal compilation or cached-runtime speedup is established. All pooled medians and both round medians are retained, including slower results. The initial all-variance candidate was screened, then replaced by the exact-type version; its figures are not mixed into retained-version results. Custom delegates still use the pre-existing extra canonical-delegate adaptation, so that additional allocation remains an opportunity rather than a claimed fix.

## Issue #250: collection-free precreated constants with nested lambdas

The explicit `TryCompileWithPreCreatedClosure` path can now compile capture-free nested lambdas during emission. It splits the nested-delegate table from the supplied constants, binds every nested delegate to one target, reuses a lambda instance's compiled delegate, and omits constant-usage metadata. It retains direct indexed constant loads rather than counting uses or automatically hoisting constants. Ordinary `CompileFast` keeps its existing collector and closure layout.

Support probes verified all four nested workloads on both APIs and runtimes. The old manual path rejects all four; its existing flat case works before and after. This is **partial #250 support**: a nested lambda's own parameters (including a custom byref delegate) work, but captures of outer parameters or block variables return null and require ordinary `CompileFast`. The existing manual-path label/goto and unsupported-feature limitations remain. It does not claim to resolve all of #250.

The precreated constant inventory is supplied by the caller. The benchmark prepares that inventory outside compilation timing; each timed public API call still allocates/copies its value array. Construction measures the same tree builder as a control, not the cost of discovering the constant inventory. An implementation that first discovers constants with a full visitor must include that extra work in its application-level measurement.

The initial shared prototype used the existing manual path's unpooled IL generator. It consequently allocated more than ordinary FEC despite sharing constants. A follow-up reuses FEC's existing IL-generator pool, including failure cleanup. The pooled revision is the current prototype. One remaining limit is that parent and child compilation overlap: the existing one-slot generator pool cannot supply both active generators. The large nested-child case still allocates more than ordinary compilation; extending the pool is deferred pending its retention and default-path implications. These fresh three-role measurements isolate that change:

| Runtime / API | Shape | #559 normal compile us | Initial manual us | Pooled manual us | #559 normal / pooled + first us | Normal B/op | Initial manual B/op | Pooled manual B/op | Pooled / #559 cached runtime |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 10.0.9 / standard | flat_constants64 | 40.01 | 1417.53 | 1368.43 | 1296.65 / 1703.52 | 7464 | 8504 | 6248 | 0.875x |
| 10.0.9 / standard | nested_constants64 | 44.29 | 1432.94 | 1426.39 | 1168.55 / 1317.76 | 8657 | 10097 | 9857 | 0.969x |
| 10.0.9 / standard | nested_siblings8 | 35.26 | 1144.63 | 1055.49 | 752.37 / 914.85 | 16377 | 18937 | 15521 | 1.002x |
| 10.0.9 / light | flat_constants64 | 44.82 | 1218.83 | 1203.01 | 1280.83 / 1203.84 | 7464 | 8504 | 6248 | 1.001x |
| 10.0.9 / light | nested_constants64 | 52.46 | 1427.37 | 1325.61 | 1104.65 / 1132.80 | 8657 | 10097 | 9857 | 1.085x |
| 10.0.9 / light | nested_siblings8 | 40.69 | 1046.76 | 993.78 | 738.98 / 878.41 | 16377 | 18937 | 15521 | 0.993x |
| 8.0.28 / standard | flat_constants64 | 46.02 | 1031.31 | 969.59 | 1016.48 / 1045.00 | 7464 | 8504 | 6248 | 1.038x |
| 8.0.28 / standard | nested_constants64 | 51.44 | 1272.52 | 1233.04 | 1056.28 / 1097.76 | 8656 | 10096 | 9856 | 0.954x |
| 8.0.28 / standard | nested_siblings8 | 46.53 | 1515.86 | 1569.32 | 863.98 / 1400.05 | 16377 | 18937 | 15521 | 1.009x |
| 8.0.28 / light | flat_constants64 | 50.37 | 935.58 | 963.94 | 866.72 / 848.02 | 7464 | 8504 | 6248 | 0.994x |
| 8.0.28 / light | nested_constants64 | 44.54 | 869.69 | 760.72 | 743.14 / 765.73 | 8657 | 10097 | 9857 | 1.002x |
| 8.0.28 / light | nested_siblings8 | 36.30 | 932.96 | 957.04 | 626.67 / 856.03 | 16377 | 18937 | 15521 | 0.946x |


The initial unpooled experiment is retained below to show what did not work:

The new manual path conservatively uses exception-aware anonymous DynamicMethods because collection is skipped. This can move JIT/access work into delegate creation. Compare **compile + first invocation**, not compile-only alone:

| Runtime / API | Shape | #559 normal / new manual compile, us | #559 normal / new manual + first, us | Compile B/op before / manual | Manual / current normal cached runtime |
| --- | --- | --- | --- | --- | --- |
| 8.0.28 / standard | nested_constants64 | 72.97 / 2020.05 | 1364.62 / 1723.83 | 8657 / 10097 | 0.946x |
| 8.0.28 / standard | nested_siblings8 | 110.83 / 1934.16 | 863.16 / 1819.88 | 16377 / 18937 | 1.056x |
| 8.0.28 / standard | nested_reused8 | 16.74 / 741.16 | 199.18 / 517.82 | 3728 / 5176 | 0.959x |
| 8.0.28 / light | nested_constants64 | 75.32 / 1542.43 | 1031.83 / 953.59 | 8656 / 10097 | 1.228x |
| 8.0.28 / light | nested_siblings8 | 63.76 / 1699.93 | 1203.57 / 1626.23 | 16377 / 18937 | 1.352x |
| 8.0.28 / light | nested_reused8 | 12.27 / 767.44 | 273.49 / 605.77 | 3728 / 5176 | 1.073x |
| 10.0.9 / standard | nested_constants64 | 84.39 / 2727.66 | 1714.06 / 1731.85 | 8657 / 10097 | 0.815x |
| 10.0.9 / standard | nested_siblings8 | 67.08 / 1390.35 | 720.42 / 825.77 | 16377 / 18937 | 1.044x |
| 10.0.9 / standard | nested_reused8 | 11.86 / 378.29 | 164.57 / 250.42 | 3728 / 5176 | 1.056x |
| 10.0.9 / light | nested_constants64 | 65.70 / 2217.50 | 2214.90 / 1760.00 | 8657 / 10097 | 0.918x |
| 10.0.9 / light | nested_siblings8 | 49.51 / 1599.89 | 908.09 / 1024.69 | 16377 / 18937 | 0.853x |
| 10.0.9 / light | nested_reused8 | 12.95 / 482.26 | 273.92 / 434.31 | 3728 / 5176 | 1.041x |

Every row also has #350 and current normal `CompileFast` measurements in the full JSON/CSV. New manual runtime can be slower because constants are loaded by stable array index with no usage analysis, and nested delegates are loaded through the shared table. Shared storage is an opt-in tradeoff, not a default replacement.

The flat manual API provides a like-for-like old/new manual comparison with the same hosting policy:

| Runtime / API | Old manual compile, us | New manual compile, us | Compile B/op before / after |
| --- | --- | --- | --- |
| 10.0.9 / standard | 1311.39 | 1349.18 | 8664 / 8512 |
| 10.0.9 / light | 1358.26 | 1407.29 | 8664 / 8512 |
| 8.0.28 / standard | 886.43 | 843.31 | 8661 / 8509 |
| 8.0.28 / light | 1009.01 | 866.25 | 8661 / 8509 |

## Lifetime tradeoff

Keeping one escaped nested delegate alive also retains **all supplied constants and the shared nested-delegate table**, including unrelated siblings. A controlled probe keeps only the first child, then runs three full GC/finalizer rounds. An unrelated 1 MiB byte-array constant is collected with #559, #350 and current normal `CompileFast`; it stays alive with the new shared precreated path on both runtimes and both APIs. The returned child still returns the same result. See `lifetime-probes.json` and the archived no-inline probe. Lower allocation does not establish lower retained memory. This ownership/lifetime choice needs review before treating this as a general replacement.

## Correctness and method

Release solution builds passed. Standard/Light native runners passed on .NET 8/10 and Framework 4.7.2; Framework uses en-US current/default-thread culture for pre-existing message assertions. Imported runtime tests: **35,088 passed, 59 existing skips, zero failures or crashes**. Known deep-tree exclusions remain. New tests cover every existing Func/Action arity 0–10, observable mutation, independent targets, custom delegates, nested target sharing, reused delegates, custom byref parameters, exception regions, unsupported outer captures and concurrent compilation. No correctness tests were skipped to accept the new layouts; two inspection helpers now recognize direct storage targets while preserving the value/cell assertions.

Windows ARM64; SDK 10.0.301; .NET 8.0.28 and 10.0.9; Release. Frozen assemblies from exact source snapshots. CPU 5 affinity, High priority; tiering and ReadyToRun disabled. One common facade per experiment, identical drivers between stages, 1,000 warmup operations, 30 ms calibration target, two opposite-order process rounds with seven samples each. Per-phase operation counts match between compared roles. Five boundaries: tree construction, prebuilt-tree compilation, compile + first invocation, root/factory execution, cached-inner execution. Raw samples and round medians are retained; shared-host drift remains material.

`evidence.zip` includes raw measurements, manifests, source/binary hashes, actual IL, shared harnesses, capability/lifetime probes, build logs, validation logs and a replay script. #350: 7,840 observations; #250 initial mixed roles: 5,600; flat old/new manual: 840; pooled follow-up: 2,520. Pooled and unpooled figures remain distinct. The report does not combine obsolete candidate numbers with retained versions.
