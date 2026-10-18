# Local performance work, 2026-10-18

Base: compatibility fixes `3f682768d6b23e445ae46595e456658899e371e4`.
Branch: `perf-expression-compilation-and-runtime`. No push or PR publication authorized.
Host: Windows 10.0.26200, ARM64, .NET 10.0.9, SDK 10.0.301, six logical processors.
Release, workstation concurrent GC, tiering disabled, BenchmarkDotNet 0.15.8 in-process
emit, three warmups and eight 150ms measured iterations. Times in CSV are nanoseconds.
Raw exports include the actual sample distributions. The confidence half-width column
uses BenchmarkDotNet's exported 99.9% interval.

## Baseline

`00-baseline.csv`: production base above; harness introduced by `9ba3fcbb`.
32 measurements: four workloads, System/Light construction, System/standard FEC/Light
compilation and invocation. Standard FEC consumes System trees, so construction is shared.
All invocation setup equivalence checks passed. This is a small screening matrix, not
the complete compatibility suite or a claim about all applications.

## Optimization 1: direct vector-array classification

Use Type.IsSZArray on modern targets instead of getting the element type, constructing
its vector-array type, and comparing types. Retain the original fallback on older targets,
including .NET Framework 4.7.2. No change to emitted array opcodes or closure layout.

Evidence: `01-vector-array.csv` versus `00-baseline.csv`, same benchmark source.
Array compilation: standard FEC 2,751 -> 2,394 ns; Light 2,769 -> 2,186 ns.
System control also moved 46,886 -> 42,646 ns. Raw improvements are 13.0%/21.1%; ratios
against the System control improved approximately 4.3%/13.2%. These short-run figures
need a focused repeat; the entire raw difference must not be attributed to the change.
Construction and invocation were remeasured for all four workloads. No runtime speedup
is claimed: this changes compilation bookkeeping only. Allocation is effectively unchanged.

Validation: standard and Light non-vector/multirank receiver regressions, 20 executions,
zero failures. Existing tests cover lower bounds, vectors, element mutation, by-ref
arguments, covariance, ignored reads and exception-region receivers.
