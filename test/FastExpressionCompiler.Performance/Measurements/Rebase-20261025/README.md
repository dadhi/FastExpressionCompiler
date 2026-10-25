# Current master, #558 and #559 performance

Only these current source stages are compared:

| Stage | Source commit |
| --- | --- |
| master | 280cbafc8fd5b29b6fec7e672bbe70d221075b30 |
| after558 | dd989683d484bfc4a8c13e65373f528314885f24 |
| after559 | 00ce08cd4c22611faadfbcb6bfc04ee2a20bd3d9 |

The final #559 publication commit changes documentation only. Source and binary hashes bind every measurement to these exact snapshots. System is an independently calibrated, unchanged reference; it has no before/after PR delta.

## Retained performance changes

- Named LightExpression field lookup avoids materializing every declared field, preserving duplicate-name and inheritance behavior.
- Binary left terminals use the existing terminal collector without recursive general dispatch.
- Each nested free parameter establishes capture storage once. Later references use the existing identity-based deduplication list before expensive whole-root private-reference analysis. Explicit quote/runtime-variable cell promotion remains intact. No closure layout or emitted code changes.

[Incremental evidence for the capture fix](../Optimization-20261025/README.md) records its independent measurements and validation. Binary-right dispatch, unchecked scope lookup, and arithmetic operand dispatch candidates were measured but rejected because repeatable benefits were not established. No speculative patch is retained.

## Three-stage comparison

Geometric means across 20 equally weighted workloads; ratios above 1 mean slower. These describe this workload set, not application throughput or statistical significance. All individual results and both process-round medians remain in `summary.json` and `current-three-stage.csv`.

| Runtime / API | Phase | Master | #558 / master | #559 / master | #559 / #558 |
| --- | --- | --- | --- | --- | --- |
| 8.0.28 / standard | construct | 1.000x | 0.912x | 0.935x | 1.025x |
| 8.0.28 / standard | compile | 1.000x | 1.575x | 1.423x | 0.903x |
| 8.0.28 / standard | compile_first | 1.000x | 0.972x | 0.985x | 1.013x |
| 8.0.28 / standard | invoke | 1.000x | 0.959x | 0.975x | 1.016x |
| 8.0.28 / light | construct | 1.000x | 0.985x | 0.969x | 0.984x |
| 8.0.28 / light | compile | 1.000x | 1.451x | 1.347x | 0.928x |
| 8.0.28 / light | compile_first | 1.000x | 1.032x | 1.025x | 0.994x |
| 8.0.28 / light | invoke | 1.000x | 0.970x | 0.970x | 1.000x |
| 10.0.9 / standard | construct | 1.000x | 0.972x | 0.998x | 1.026x |
| 10.0.9 / standard | compile | 1.000x | 1.600x | 1.462x | 0.914x |
| 10.0.9 / standard | compile_first | 1.000x | 0.995x | 1.003x | 1.008x |
| 10.0.9 / standard | invoke | 1.000x | 0.974x | 0.980x | 1.006x |
| 10.0.9 / light | construct | 1.000x | 0.968x | 0.962x | 0.993x |
| 10.0.9 / light | compile | 1.000x | 1.476x | 1.389x | 0.941x |
| 10.0.9 / light | compile_first | 1.000x | 1.026x | 1.018x | 0.992x |
| 10.0.9 / light | invoke | 1.000x | 0.972x | 0.974x | 1.002x |

Shared-host timing variation remains material. Construction of Standard trees and identical emitted delegate bodies are controls; their timing shifts must not be attributed to compiler changes. No universal master-parity claim is made.

## Capture workload

The added stress workload performs 32 distinct updates to one captured string, then reads its length. It exposes repeated analysis costs and is not an application benchmark. Values below are compilation / compilation plus first execution in microseconds:

| Runtime / API | Master compile / + first, us | #558 compile / + first, us | #559 compile / + first, us |
| --- | --- | --- | --- |
| 8.0.28 / standard | 37.85 / 4122.71 | 462.29 / 3397.14 | 89.54 / 3445.67 |
| 8.0.28 / light | 84.01 / 1296.53 | 254.67 / 1467.19 | 98.68 / 1304.32 |
| 10.0.9 / standard | 21.85 / 1684.88 | 227.57 / 1943.60 | 40.57 / 1797.97 |
| 10.0.9 / light | 106.70 / 1691.40 | 299.81 / 1833.81 | 138.81 / 1695.95 |


## Managed allocations

All measured allocations are preserved in the CSV/JSON. Differences up to 0.5 B/op are treated as fractional measurement residue.

| Runtime / API | Phase | Lower B/op than #558 | Matching #558 | Higher B/op than #558 |
| --- | --- | --- | --- | --- |
| 8.0.28 / standard | construct | 0 | 20 | 0 |
| 8.0.28 / standard | compile | 0 | 20 | 0 |
| 8.0.28 / standard | compile_first | 0 | 20 | 0 |
| 8.0.28 / standard | invoke | 0 | 20 | 0 |
| 8.0.28 / light | construct | 6 | 14 | 0 |
| 8.0.28 / light | compile | 0 | 20 | 0 |
| 8.0.28 / light | compile_first | 0 | 20 | 0 |
| 8.0.28 / light | invoke | 0 | 20 | 0 |
| 10.0.9 / standard | construct | 0 | 20 | 0 |
| 10.0.9 / standard | compile | 0 | 20 | 0 |
| 10.0.9 / standard | compile_first | 0 | 20 | 0 |
| 10.0.9 / standard | invoke | 0 | 20 | 0 |
| 10.0.9 / light | construct | 6 | 14 | 0 |
| 10.0.9 / light | compile | 0 | 20 | 0 |
| 10.0.9 / light | compile_first | 0 | 20 | 0 |
| 10.0.9 / light | invoke | 0 | 20 | 0 |


 The per-fix capture comparison found no added allocation in construction, compilation, compilation plus first invocation, or warmed execution. Named Light field lookup saves approximately 32 B per lookup; the 32-constructor/field case saves approximately 1,024 B per tree construction.

| Runtime / Light shape | Master B/op | #558 B/op | #559 B/op |
| --- | --- | --- | --- |
| 10.0.9 / new_member | 304.01 | 304.01 | 272.01 |
| 10.0.9 / capture_reference | 368.01 | 368.01 | 336.01 |
| 10.0.9 / capture_mutable_reference_ctor | 704.03 | 704.03 | 672.03 |
| 10.0.9 / constructors32 | 8320.35 | 8320.35 | 7296.30 |
| 8.0.28 / new_member | 304.01 | 304.01 | 272.01 |
| 8.0.28 / capture_reference | 368.01 | 368.01 | 336.01 |
| 8.0.28 / capture_mutable_reference_ctor | 704.03 | 704.03 | 672.03 |
| 8.0.28 / constructors32 | 8320.33 | 8320.33 | 7296.29 |

Across all 320 runtime/API/workload/phase groups, **0** final #559 allocation results exceed master by more than 0.5 B/op. Allocation movements in unchanged controls are not attributed to the patch. This measures managed allocation on the benchmark thread, not native/JIT allocation, retained memory, or process memory.

## Exception hosting and first execution

#558 uses exception-aware anonymous DynamicMethods to preserve non-Exception catch behavior. JIT/access checks can therefore move into delegate creation. Compare compilation plus first execution as well as compilation alone; changing hosting back would lose required behavior. Nested-32-EH results:

| Runtime / API | Master compile / + first, us | #558 compile / + first, us | #559 compile / + first, us |
| --- | --- | --- | --- |
| 8.0.28 / standard | 10.81 / 102.86 | 95.88 / 112.40 | 99.29 / 112.24 |
| 8.0.28 / light | 14.50 / 73.61 | 76.71 / 75.93 | 74.17 / 77.45 |
| 10.0.9 / standard | 15.55 / 88.80 | 96.08 / 92.49 | 94.83 / 91.93 |
| 10.0.9 / light | 13.55 / 86.81 | 90.86 / 87.65 | 93.81 / 87.93 |

Processes and compiler paths are warm. First execution creates each delegate and calls it once, including a returned nested delegate; this is not cold-process startup.

## Runtime and System reference

All **80/80** measured root-plus-returned-nested instruction bundles match exactly between #558 and #559 (20 shapes, two APIs, two runtimes), without IL normalization. This verifies measured emitted instructions, not all possible expression trees or native JIT bodies. Warmed runtime and allocation are also measured independently. No generated-code execution speedup is claimed.

System is unchanged and is also the untimed behavior oracle. Selected compilation times:

| Runtime / shape | System compile, us | #559 Standard, us | #559 Light, us |
| --- | --- | --- | --- |
| 8.0.28 / add | 25.41 | 1.90 | 1.42 |
| 8.0.28 / balanced256 | 284.16 | 29.31 | 25.95 |
| 8.0.28 / arithmetic64 | 45.47 | 27.52 | 32.00 |
| 10.0.9 / add | 12.04 | 1.27 | 1.41 |
| 10.0.9 / balanced256 | 247.50 | 27.74 | 27.89 |
| 10.0.9 / arithmetic64 | 26.78 | 22.75 | 29.26 |

FEC is not universally faster than System; full reference values remain in the data.

## Method and validation

Windows ARM64, SDK 10.0.301, .NET 8.0.28 and 10.0.9, Release. CPU 5 affinity, High priority, tiering and ReadyToRun disabled. One shared facade and driver compiled against each exact source snapshot. 1,000 warmup operations; seven samples in each of two opposite-order process rounds; approximately 30 ms calibration target. Matching FEC-stage operation counts. Four phases: construction, prebuilt-tree compilation, compilation plus first execution, warmed execution. All behavior comparisons passed. **15,680 observations**, **320 result groups**. System runs separately with independent calibration.

Release solution build and native .NET 8/10/Framework runners pass. Native legacy counters: 1,733 / 1,734; Framework legacy counter: 1,617, plus registered regression fixtures. Framework uses English current/default-thread UI culture for existing English message assertions. Imported runtime tests: **35,088 passed, 59 existing skips, zero failures or crashes**. Deep-tree cases remain deferred. See validation logs.

Extract `current-evidence.zip` and run `python Replay.py` with the matching runtimes to repeat the protocol. It contains only these current stages and the unchanged System reference: immutable source/binaries, hashes, actual IL, raw samples, shared harness, build logs and host details. No previous-branch figures are carried forward.
