# Incremental optimization evidence

These measurements isolate each retained fix within the current #559 series. They are not older-branch comparisons. The final stack comparison uses only current master, #558, and the final #559 source.

## Deduplicate captured variables before storage analysis

The existing per-lambda free-parameter list now prevents repeated whole-root private-reference proofs for the same capture. No closure fields, pools, or emitted instructions change. Scope checks still precede deduplication, and quote/runtime-variable requests still explicitly require cells.

Pinned Windows ARM64, CPU 5, high priority; .NET 8.0.28 and 10.0.9; tiering and ReadyToRun disabled; three process rounds, alternating version order, 11 samples per round, 30 ms calibration target, 1,000 warmup operations. Both versions use identical operation counts per phase.

| Runtime / API | Workload | Compile ratio | Compile + first execution ratio | Warm execution ratio | Allocation delta B/op (compile / first / warm / construction) |
| --- | --- | ---: | ---: | ---: | --- |
| 8.0.28 / standard | capture_mutable_reference_ctor | 0.931x | 1.016x | 1.017x | 0.00 / 0.00 / 0.00 / 0.00 |
| 8.0.28 / standard | capture_mutable_reference32 | 0.162x | 0.862x | 0.992x | 0.00 / 0.00 / 0.00 / 0.00 |
| 8.0.28 / light | capture_mutable_reference_ctor | 0.940x | 1.002x | 1.004x | 0.00 / 0.00 / 0.00 / 0.00 |
| 8.0.28 / light | capture_mutable_reference32 | 0.388x | 0.892x | 1.027x | 0.00 / 0.00 / 0.00 / 0.00 |
| 10.0.9 / standard | capture_mutable_reference_ctor | 0.904x | 0.935x | 1.015x | 0.00 / 0.00 / 0.00 / 0.00 |
| 10.0.9 / standard | capture_mutable_reference32 | 0.159x | 0.880x | 0.992x | 0.00 / 0.00 / 0.00 / 0.00 |
| 10.0.9 / light | capture_mutable_reference_ctor | 0.946x | 1.013x | 1.007x | 0.00 / 0.00 / 0.00 / 0.00 |
| 10.0.9 / light | capture_mutable_reference32 | 0.411x | 0.916x | 0.991x | 0.00 / 0.00 / 0.00 / 0.00 |

The repeated-reference stress workload performs 32 distinct updates to one captured string; it is intentionally sensitive to repeated analysis, not an application-throughput forecast. Smaller timing shifts, including unchanged controls, remain noisy. All 48 root/nested IL comparisons across workloads, APIs, runtimes and rounds match exactly. No repeatable warmed-runtime regression or additional allocation was established.

The adjacent archive contains raw samples, exact IL, source snapshots, the common harness and binary hashes for this incremental experiment. Rejected candidate data is excluded.

Validation: full Release solution build; native runners on .NET 8 and 10; .NET Framework 4.7.2 runner with en-US culture for existing message assertions; imported runtime tests: **35,088 passed, 59 skipped, zero failures**. Known deep-tree exclusions remain in place.

To replay the incremental archive, extract it, build `candidate-host/Host.csproj` in Release, set `BENCH_BASE=screen-base`, and run `python Screen-Candidate.py capture-dedup capture_mutable_reference_ctor capture_mutable_reference32 capture_reference add`. The archive includes the frozen measured binaries.
