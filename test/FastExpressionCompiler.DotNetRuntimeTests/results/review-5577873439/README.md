# Review 5577873439: decisions and measured follow-ups

Review: https://github.com/dadhi/FastExpressionCompiler/pull/558#pullrequestreview-5577873439

Baseline: `8f12292f0baa596ed160c0e070ebb8329a623429`. This follow-up changes source comments and adds two boundary test methods, run for Standard and LightExpression. **None of the experimental packing, visitor or inlining changes below is merged.** Compiler behavior, emitted code and supported-tree limits remain at the baseline. The existing anonymous-host fix and measurements remain documented in the preceding PR report.

## Changes included

- `ed431c7f`: explain why frame growth is outlined, and why logarithmic scope-pool buckets reduce bookkeeping without weakening length checks.
- `35622f32`: protect opaque quoted extensions from reduction, and compare constant-pruned Try/Goto invocation-argument evaluation order with System. Standard strict rejection and Light's unsupported opaque-System-node conversion are tested separately; this does not claim Light conversion support for opaque nodes.
- This report and the archived experiments preserve both benefits and observed costs of rejected candidates.

## All seven review comments

| Comment | Decision |
| --- | --- |
| [4318955513: bool flags](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4318955513) | Tried two packed-flag implementations. Both save 8 B of context layout, but measurements do not support compilation/allocation parity. Keep existing bools. |
| [4318977043: target/index struct](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4318977043) | Keep full-width IDs and current tuples. Actual target+int tuples occupy 16 B on all tested configurations; renaming them to sequential structs does not remove reference-alignment padding. |
| [4318998212: NoInlining](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4318998212) | Keep the small aggressively inlined repeated-kind path and outlined append/growth helper. Removing NoInlining produced no consistent win. Add rationale. |
| [4319030709: scope/frame ints](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4319030709) | Keep unbounded int scope indices and compressed-frame counts. ScopeInfo is 12 B and LabelScopeFrame 16 B. The 1024 pool retention bound is not a valid-tree limit. |
| [4319042922: region flag bit](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4319042922) | Tested lossless packing into the int sign bit rather than narrowing to short. Frame 8â†’4 B and context âˆ’16 B; typically 48 B less cold allocation, but mixed compilation timings. Hold. |
| [4319058643: logarithmic buckets](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4319058643) | Clarify the source: 4,9,19,39,â€¦ lengths occupy distinct floor(log2) buckets. A 1024 retention limit needs 11 reference slots, not a table indexed by every length. |
| [4319124960: consolidate visitors](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4319124960) | Preserve stage-specific traversal. Tested lazy free-variable storage, a compile/runtime quote split, and a shared lazy argument scan. Allocation savings exist, but first-use/compilation costs preclude merging these candidates under the requested performance constraint. Add semantic boundary tests. |

The earlier [anonymous-module comment](https://github.com/dadhi/FastExpressionCompiler/pull/558#discussion_r4318926508) was already answered and fixed before this baseline. Its reply and measurements remain linked in the PR.

## Candidate results, including costs

Ratios below are **median paired candidate/baseline time**; above 1 is slower. They are bounded observations, not universal causal claims or a new comparison against pre-PR/System performance.

| Candidate | Benefit measured | Reason not included |
| --- | --- | --- |
| Packed flags V1 | Context âˆ’8 B on all six configurations | Modern EH compilation ratios 1.043â€“1.123; no general speedup. |
| Packed flags V2, single-load setters | Same layout saving; setter IL reduced by 5 B each, from 32/33/36 to 27/28/31 B | Framework Light capture compilation **+192 B/op**, time 1.188; compile+first **+192 B/op**, time 1.155. Std .NET 8 EH compile 1.138. Exact allocating mechanism unresolved. |
| Exception-region sign bit | Frame 8â†’4 B; context âˆ’16 B; typically âˆ’48 B fresh compile+first allocation | Std .NET 10 nested-EH compile 1.043/first 1.030; Light 1.026/1.049; Light scalar and small-EH compile controls around 1.11. |
| Remove helper NoInlining | No demonstrated target benefit | Across 42 compile comparisons median 1.015, range 0.921â€“1.149; Light .NET 8 label/goto 1.149. |
| Lazy quote free-variable list V1 | Captured activation âˆ’32 B modern / âˆ’40 B Framework; closed quote compilation also allocates less | Captured compile+first ratios 1.124â€“1.185 across all six configurations. |
| Separate compile-only quote collector V2 | Captured activation âˆ’40 B modern / âˆ’48 B Framework, including smaller binder object | Captured compile+first ratios 1.021â€“1.250; five marginal median ratios also 1.151â€“1.236. Framework Light activation 1.069. |
| Shared lazy argument scan | Terminal runtime-expression/Dynamic compile and compile+first âˆ’24 B on all six configurations | Mixed timings, including Light runtime-goto compile 1.075/1.105 on .NET 8/10 and disturbed scalar controls. |

V1 and V2 are independently paired against the same baseline in separate process runs. Comparing their reported ratios does not constitute a directly paired V2/V1 experiment. The flag IL inspection did **not** establish failed setter inlining as the cause of the original slowdown. A smaller context is a layout saving, not automatically a managed-heap allocation saving.

Region boundaries 0, 32768, 65536 and int.MaxValue, both assignment directions and flag toggles passed. Its 258 lexical/EH cases passed; all 18 finalized delegate instruction/local/EH/signature/host records were known and equal. Five cold allocation pairs per configuration saved exactly 48 B except .NET 10 Light, whose deltas were **âˆ’48, âˆ’12384, âˆ’12384, +12288, âˆ’48 B**. The Â±12336 B mode is retained; do not substitute a tidy 48 B result for that configuration's actual data.

## Why traversal remains separate

The quote binder runs during execution with the current activation's live capture cells. Compile-time collection cannot supply those cells. Quote syntax is returned data: its lambda/block/catch scopes, RuntimeVariables merges, identity, and opaque extension behavior must survive without executable lowering, pruning or reduction.

The argument spill scanner intentionally sees Try/Goto even in constant-pruned branches. That affects whether runtime expression compilation or Dynamic target reads precede argument effects and jumps. Executable collection prunes those branches; running it again can duplicate Dynamic reductions and extension callbacks. Sharing lexical helpers is possible, but blindly moving these visitors into the ordinary collector changes semantics. The new tests guard these boundaries without asserting a particular visitor or allocation implementation.

## Reproducibility and limits

`review-experiments.zip` contains frozen source/project snapshots, build/check logs, scripts, raw CSV samples, per-group analyses, source/runner hashes, and held patches. `manifest.json` records every archive entry's SHA-256 and the archive hash. Build binaries are excluded; their hashes and reproducible source projects are retained. Raw C# snapshots are archived to avoid SDK test compile globs.

All elapsed stages ran sequentially with no competing benchmark/build/probe. Tiering and ReadyToRun were disabled through DOTNET and COMPlus settings in the orchestration scripts. Standard/Light ran on .NET 8, .NET 10 and .NET Framework 4.7.2. These studies contain **13,596 warm observations**, plus **60 fresh-process allocation-only observations** for region packing. No cold elapsed-performance result is claimed.

Expression construction and Light conversion are outside the timed compilation phases; compilation measures FEC collection/emission/delegate creation. Compile+first additionally invokes the newly compiled delegate, so deferred JIT and activation costs can appear there.

Each warm stage used 11 alternating paired samples and common operation counts calibrated toward at least 20 ms. Actual durations are retained, including shorter samples:

| Stage | Rows | Samples below 20 ms | Minimum ms |
| --- | ---: | ---: | ---: |
| Flags V1 | 1848 | 126 | 16.785 |
| Flags V2 | 1848 | 117 | 11.850 |
| Region packing | 1188 | 22 | 15.786 |
| NoInlining removal | 3036 | 89 | 16.540 |
| Quote V1 | 1452 | 60 | 10.888 |
| Quote V2 | 1452 | 57 | 13.878 |
| Argument scan | 2772 | 53 | 12.746 |

Allocation values are current-thread managed bytes, not RSS, total process memory or native/JIT memory. Warm pool behavior, fresh-process behavior, compile-only, compile+first call, activation and preactivated nested invocation are separate phases. Fractional batch-event allocations, outliers, disagreement between paired and marginal medians, and timing variations for unchanged delegate code are preserved. System compilation is used as a correctness oracle here, not timed anew; prior System/pre-PR measurements remain in the existing PR report. No new emitter, host, fallback, cache, input limit or exclusion is introduced.

## Final validation

See `validation.json` and `validation-logs.zip` for the exact code head, commands, counters, frozen imported-test assembly hashes and full logs. The imported suite retains its prior exclusions, including the deferred deep-tree crash; excluded tests are not reported as passing.

Final result at `35622f327afd42603e83403b3b5724750108e7d2`: **35,064 imported cases pass, zero executed failures/crashes; 69 existing exclusions unchanged.** Both production builds and .NET 8/10 native suites pass. Framework passes 1,041 TestX and 1,595 legacy tests.
