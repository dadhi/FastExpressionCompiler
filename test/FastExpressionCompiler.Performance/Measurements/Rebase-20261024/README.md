# Three-stage comparison after rebasing #559 onto #558

## Exact revisions and retained scope

- Before #558: current upstream master without #558, `233ed5adaa9d9ceea67d71e153d0d2457f2cce00`. This includes merged #557/#562 and upstream SmallMap/conversion-helper repairs. It is not the historical original parent of #558; choosing this matched base isolates the current PR's incremental cost.
- After #558: `aaa79fe73f0978e9be8150116d4175f04cb5c971`.
- After #559: rebased production/test source `e300f4e2`.

Rebased the performance layer from `3f682768` onto current #558. The array-classification commit `7b6863bc` was dropped: #558 already uses IsSZArray on modern configured targets and retains its Framework fallback. The remaining production change is `d4a74798`, named LightExpression field lookup using GetDeclaredField with existing special-metadata handling. Native runner conflicts retain all #558 regressions and add the named-field/conditional oracles. No #558 branch rewrite or source change was made. The earlier conditional branch candidate remains rejected.

Historical measurement CSVs are preserved; dropped source does not erase historical evidence. The old Summarize confidence-column correction is retained. Historical System rows are unchanged reference controls, not changes to System caused by this PR. Current results below supersede historical timing claims for this rebased source.

## Compilation comparison

Ratios above 1 mean slower. Each value is the descriptive median of 41 workload-specific time ratios, not an aggregate application result. The ratio columns have independently computed medians, so they do not multiply exactly.

| Runtime/flavor | Before #558 | After #558 / before | After #559 / before | After #559 / #558 |
| --- | ---: | ---: | ---: | ---: |
| 8.0.28 standard | 1.000x | 1.0461x | 1.0890x | 1.0395x |
| 8.0.28 light | 1.000x | 1.1690x | 1.2031x | 1.0441x |
| 10.0.9 standard | 1.000x | 1.1164x | 1.2042x | 1.0375x |
| 10.0.9 light | 1.000x | 1.0746x | 1.1041x | 1.0303x |

#558 still has measured compilation costs relative to current master: these medians are about 4.6–16.9% higher. #559 does not establish a compilation improvement: its medians are 3.0–4.4% higher than #558 in this run. All Standard production source files are byte-identical between #558/#559, so the Standard timing movement cannot be attributed to a Standard compiler code change. Light's compiler/emitter source is also identical; its expression factory implementation differs. These observations retain run-to-run/JIT/layout/host variation and do not establish a causal CPU regression or universal parity. See every workload and raw distribution rather than treating a median ratio as a guarantee.

Adaptive operation counts produced small fractional allocation residuals. A separate equal-count check (200 compilations/sample, 100 warmups, seven samples in each of two opposite-order rounds) confirms for **each runtime/flavor**: #558 allocates less in **14/41** workloads and the same in **27/41**, with **zero increases**; #559 matches #558 in **all 41**. Original adaptive residuals remain in the files rather than being rounded away. `allocation-confirmation.json` contains the equal-count values.

## Remaining optimization: Light tree construction

The named-field change affects construction, not a text parser. The broad construction pass caps at 20,000 operations; short fast samples were therefore confirmed separately with a higher cap (2,000,000) and a 30 ms calibration target. Both datasets remain available. Longer-run medians for the three selected affected shapes follow; unchanged constant/array and additional capture controls are retained in `construction-confirmation.json`.

| Light construction / runtime | After #558, ns | After #559, ns | B/op after #558 → #559 |
| --- | ---: | ---: | ---: |
| new_member / 8.0.28 | 127.0 | 108.2 | 304.01 → 272.01 |
| capture_reference / 8.0.28 | 139.2 | 97.3 | 368.01 → 336.01 |
| capture_mutable_reference_ctor / 8.0.28 | 226.6 | 178.3 | 704.03 → 672.03 |
| new_member / 10.0.9 | 116.5 | 109.6 | 304.01 → 272.01 |
| capture_reference / 10.0.9 | 97.5 | 92.1 | 368.02 → 336.01 |
| capture_mutable_reference_ctor / 10.0.9 | 188.5 | 171.4 | 704.03 → 672.03 |

These affected workloads save approximately **32 B per lookup** on this host/runtime, rather than reusing the old report's 40 B figure. Observed time reductions are roughly 5.6–30.1% across the six entries; unchanged controls also move, so this is not a universal percentage claim. Other workload construction values, including slower/noisy ones, remain in the full data.

## Generated delegate execution

After #558 vs after #559, all **82 root delegate IL bodies** match for 41 workloads in both flavors on .NET 10. This checks root IL, not every nested delegate body or JIT-generated machine code. The compiler source is unchanged. No delegate runtime speedup is claimed. Broad runtime medians (after559/after558) range about 0.994–1.039x; the raw samples and each workload's allocation results are in the phase files. For factory workloads, `invoke` includes outer activation plus invocation of the resulting inner delegate, rather than only a scalar call. All generated delegates pass an untimed System behavior oracle for the tested inputs.

## System: separate unchanged reference

System was previously measured in multiple process runs as a timing-drift control. Those varying numbers did not represent patches to System; presenting them as optimization before/after columns was misleading. Here the main comparison has exactly three FEC revisions. System appears only as **one separately measured unchanged reference per runtime**, pooled over two reference process repetitions. It has no before/after delta, and no optimization claim is normalized to it. The standalone reference executable uses only System, without a FEC library dependency. There are no System construction rows in this reference. Sample compilation references:

| Unchanged System reference | Workload | Compile us |
| --- | --- | ---: |
| 8.0.28 | new_member | 17.118 |
| 8.0.28 | array | 30.947 |
| 8.0.28 | capture8 | 194.630 |
| 8.0.28 | balanced256 | 246.676 |
| 10.0.9 | new_member | 14.402 |
| 10.0.9 | array | 29.845 |
| 10.0.9 | capture8 | 154.196 |
| 10.0.9 | balanced256 | 220.502 |

## Method and validation

Windows ARM64, .NET 8.0.28 / 10.0.9, SDK 10.0.301, Release, tiering and ReadyToRun disabled. Separate frozen source/binary snapshots, serial processes, forward/reverse FEC revision order, seven samples per phase per round. 41 common workloads cover scalars, arrays, switches, blocks, scopes/EH, invocation and captures. Construction, compilation of a prebuilt tree (including delegate creation), and warmed execution are separate. Light trees are constructed directly. Allocation uses the current thread's managed allocated-byte counter. There are 20,664 broad FEC observations plus 2,296 System-reference observations; 6,888 equal-count compilation observations and 1,176 longer construction observations are retained separately. Six non-common/new-feature benchmark shapes are listed in source-manifest.json and were not timed. They do not change the imported test exclusions. No fresh cold/first-invocation, Framework or x64 timing, retained-memory or native/JIT allocation claim is made.

Validated source `e300f4e2`: all production targets build; full .NET 10/.NET 8 ARM64 native suites pass (1,714 / 1,713 legacy cases plus registered regressions); Framework passes 1,049 TestX + 1,597 legacy cases. All 21 imported batches pass: **35,064 executed passes, zero executed failures/crashes, 69 existing exclusions unchanged**. The deferred deep-tree modes remain deferred. The retained BenchmarkDotNet project builds. The evidence-only commit changes no validated production/test code.

## Files

- `three-stage.csv`: absolute timings, allocations and three-stage ratios for every workload/phase/configuration.
- `phase-medians.csv`: all FEC phase medians and the separate System references, including ranges/sample operation counts.
- `allocation-confirmation.json`: equal-count compilation allocation proof.
- `construction-confirmation.json`: longer construction controls.
- `analysis.json`, `summary.csv`: full analysis; adaptive allocation counts are screening observations, superseded by equal-count confirmation for allocation conclusions.
- `validation.json`: revisions, counters and source/assembly hashes; `il-comparison.json`: root delegate comparisons.
- `source-manifest.json`, `binary-manifest.json`, `system-reference-manifest.json`: immutable snapshot identities.
- `range-diff.txt`: old/rebased performance-layer mapping.
- `evidence.zip` and `evidence-manifest.json`: complete sources, harnesses/scripts, project files, native/imported validation logs, raw measurements, confirmation data and manifests. Binaries are hashed and retained locally, not committed. Runners refuse to mix existing output with a new run; preserve/relocate archived output before replaying in a fresh directory. The initial System-reference build error is retained; the corrected reference project was then built and measured successfully.
