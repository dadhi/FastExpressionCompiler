# Current master, #558 and #559 performance

Only these current source stages are compared:

| Stage | Source commit |
| --- | --- |
| master | 0f37356fc9ca2c05b085f94592cfaf492f78eacf |
| after558 | dd989683d484bfc4a8c13e65373f528314885f24 |
| after559 | 00af81b4470d1ef158c28d9f6c5a03c5142acb31 |

The #559 production source is unchanged through the publication commit; source and binary SHA-256 hashes bind the snapshots. These are fresh measurements, with no previous-branch values carried forward. System is a separate unchanged reference, with no PR-stage delta.

## Changes retained in #559

Direct named LightExpression field lookup avoids materializing all declared fields. Direct terminal binary-operand collection avoids a recursive collector frame for parameter/constant left operands. The constructor signature-pool repair already belongs to master; #559 retains only its regression coverage. Neither retained optimization changes the emitter.

An additional direct call/constructor-argument shortcut was screened and confirmed separately. Its timing benefit did not consistently reproduce across runtimes and controls, so it was not retained. Exploratory results are excluded from this report.

## Current timing comparison

Geometric means of per-workload ratios across 19 equally weighted shapes; above 1 is slower. These are descriptive summaries, not application-throughput estimates or statistical significance tests. Every individual result and both round medians are retained in `summary.json` and `current-three-stage.csv`.

| Runtime / API | Phase | Master | #558 / master | #559 / master | #559 / #558 |
| --- | --- | --- | --- | --- | --- |
| 8.0.28 / standard | construct | 1.000x | 0.984x | 0.965x | 0.981x |
| 8.0.28 / standard | compile | 1.000x | 1.438x | 1.411x | 0.981x |
| 8.0.28 / standard | compile_first | 1.000x | 1.027x | 1.016x | 0.989x |
| 8.0.28 / standard | invoke | 1.000x | 0.995x | 0.983x | 0.989x |
| 8.0.28 / light | construct | 1.000x | 0.965x | 0.932x | 0.966x |
| 8.0.28 / light | compile | 1.000x | 1.458x | 1.447x | 0.992x |
| 8.0.28 / light | compile_first | 1.000x | 1.040x | 1.029x | 0.990x |
| 8.0.28 / light | invoke | 1.000x | 0.968x | 0.971x | 1.003x |
| 10.0.9 / standard | construct | 1.000x | 1.015x | 1.017x | 1.002x |
| 10.0.9 / standard | compile | 1.000x | 1.512x | 1.500x | 0.992x |
| 10.0.9 / standard | compile_first | 1.000x | 1.037x | 1.028x | 0.992x |
| 10.0.9 / standard | invoke | 1.000x | 0.957x | 0.949x | 0.991x |
| 10.0.9 / light | construct | 1.000x | 0.992x | 0.932x | 0.940x |
| 10.0.9 / light | compile | 1.000x | 1.478x | 1.472x | 0.996x |
| 10.0.9 / light | compile_first | 1.000x | 1.053x | 1.025x | 0.973x |
| 10.0.9 / light | invoke | 1.000x | 0.963x | 0.922x | 0.957x |

Shared-host timing variation remains material despite CPU affinity and opposite ordering. Standard construction is unchanged System-tree construction. Constructor shortcuts and emitted delegate bodies are also controls; movements in those paths are not attributed to source optimizations. No universal speedup or master-parity claim is made. Compile-only cost remains higher than master in this set; compilation plus first invocation is much closer.

## Managed allocations

Compared with #558, #559 compilation, compilation-plus-first-invocation and warm-invocation allocations match in all 19 workloads for every runtime/backend (differences up to 0.5 B/op treated as fractional residue). Light construction allocates less in six shapes and matches in thirteen, with no increases. Named-field lookup saves about 32 B per lookup; the 32-constructor/field workload saves about 1,024 B per build.

| Runtime / Light shape | Master B/op | #558 B/op | #559 B/op |
| --- | --- | --- | --- |
| 10.0.9 / new_member | 304.01 | 304.01 | 272.01 |
| 10.0.9 / capture_reference | 368.02 | 368.02 | 336.01 |
| 10.0.9 / capture_mutable_reference_ctor | 704.03 | 704.03 | 672.03 |
| 10.0.9 / constructors32 | 8320.35 | 8320.35 | 7296.32 |
| 8.0.28 / new_member | 304.01 | 304.01 | 272.01 |
| 8.0.28 / capture_reference | 368.02 | 368.02 | 336.01 |
| 8.0.28 / capture_mutable_reference_ctor | 704.03 | 704.03 | 672.03 |
| 8.0.28 / constructors32 | 8320.39 | 8320.39 | 7296.31 |

One unchanged Standard construction control (`constant_object`, .NET 8) measured 464.02 / 536.02 / 464.02 B/op for master/#558/#559. This unexplained control movement is preserved, not claimed as a production allocation improvement. Measurements cover managed allocation on the benchmark thread, not retained memory, native/JIT allocation or total process memory.

## First-invocation boundary

#558 uses anonymous DynamicMethods for exception-aware compilation to preserve non-Exception catch semantics. Accessibility checking/JIT can therefore move from first invocation into delegate creation. Compile-only ratios must not be read as end-to-end regression ratios. The current nested-32-EH example illustrates this; both boundaries are measured independently:

| Runtime / API | Master compile / + first, us | #558 compile / + first, us | #559 compile / + first, us |
| --- | --- | --- | --- |
| 8.0.28 / standard | 6.11 / 78.93 | 71.09 / 75.37 | 69.31 / 77.95 |
| 8.0.28 / light | 11.14 / 78.85 | 102.15 / 90.19 | 102.68 / 81.92 |
| 10.0.9 / standard | 7.08 / 90.62 | 106.37 / 100.10 | 109.85 / 89.85 |
| 10.0.9 / light | 9.01 / 88.79 | 98.55 / 94.14 | 115.75 / 85.50 |

These processes/compiler paths are warm; `compile_first` creates each delegate and invokes it once, including returned inner delegates. It is not cold-process startup.

## Generated code and System reference

All **76/76 root-plus-returned-nested instruction bundles match exactly** between #558 and #559: 19 shapes x two APIs x two runtimes. Actual instruction strings are compared without normalization; see `il-comparison.json` and the archived logs. This covers the measured delegates, not every possible tree or native JIT body. Runtime timing/allocation is measured independently; no delegate execution speedup is claimed.

System is measured in two separate processes per runtime, calibrated independently, and is not changed by either PR. It is also the untimed behavior oracle. Selected current compilation values:

| Runtime / shape | System compile, us | #559 Standard, us | #559 Light, us |
| --- | --- | --- | --- |
| 8.0.28 / add | 15.92 | 1.50 | 1.24 |
| 8.0.28 / balanced256 | 335.13 | 24.27 | 24.77 |
| 8.0.28 / arithmetic64 | 32.28 | 23.81 | 29.55 |
| 10.0.9 / add | 12.61 | 1.11 | 1.84 |
| 10.0.9 / balanced256 | 266.67 | 40.82 | 28.48 |
| 10.0.9 / arithmetic64 | 29.90 | 38.91 | 31.87 |

FEC is not universally faster than System; for example the .NET 10 arithmetic64 case is slower here. Full System values remain in the CSV/JSON.

## Method and validation

Windows ARM64, SDK 10.0.301, .NET 8.0.28 and 10.0.9, Release. Processes pinned to logical CPU 5 at High priority; tiering and ReadyToRun disabled. Frozen source assemblies; identical shared facade/driver; 1,000 warmup operations; seven samples in each of two opposite-order process rounds; approximately 10 ms calibration target. FEC stages share operation counts within a comparison. Four phases: construction, prebuilt-tree compilation, compilation plus first invocation, warm invocation. All behavior controls passed. **14,896 current-stage/reference observations**; 304 runtime/API/shape/phase result groups.

Full solution build and native .NET 8/10/Framework runners pass. Framework: 1,051 TestX and 1,617 legacy cases, using English current/default-thread UI culture for existing English exception-message assertions. Native legacy counters: 1,733 (.NET 8) / 1,734 (.NET 10), plus registered regression fixtures. Imported runtime: **35,088 passed, 59 documented skips, zero failures/crashes**. Deep-tree cases remain deferred. See `validation-logs.zip` and `validation.json`.

`current-evidence.zip` contains only these three stages and the unchanged System reference: full raw samples, actual IL logs, immutable source/binary snapshots, shared harness, hashes, build logs and host metadata. Extract and run `python Replay.py` with the matching installed runtimes to repeat the protocol. No obsolete branch report is included.
