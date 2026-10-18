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

Code commit: `7b6863bc`. Both libraries build across all four targets with zero warnings/errors.

## Optimization 2: named-field lookup without enumerating every field

Use TypeInfo.GetDeclaredField and recurse to the base type only when necessary. This
avoids allocating/enumerating a declared-field array on the common path. Keep the existing
first-declared-match rule for unusual CLR metadata with duplicate names via an ambiguity
fallback. Keep null-name handling and private/inherited/hidden/static lookup behavior.

Evidence: `02-named-field.csv` versus `01-vector-array.csv`, same benchmark source.
Light construction means (ns): arithmetic 176.4 -> 105.6, array 404.5 -> 189.7,
switch 223.4 -> 134.8, captured constant 242.4 -> 105.5. Allocation drops by exactly
40 bytes per named-field lookup: 360 -> 320, 672 -> 552, 824 -> 784, 272 -> 192 bytes.
These are tree-construction improvements; existing-tree compilation and delegate runtime
are measured as controls, with no speedup attributed to this lookup change.

Validation includes new named-field identity/behavior tests for private, inherited,
hidden and static fields, plus Reflection.Emit same-name/different-type metadata.
One initial native run failed the existing generated FlatExpression roundtrip property
test (reported case seed 864021005, replay seed 6H3IIevvWaa1). Replays with both baseline
and current libraries passed, including the explicit reported seed and default settings;
the failure remains unreproduced. Preserve the failing log rather than masking it.
