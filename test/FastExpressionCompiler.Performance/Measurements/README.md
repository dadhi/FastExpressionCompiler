# Historical screening results (original stack)

These measurements belong to the original revisions below. For current three-stage FEC results after rebasing onto #558, see [Rebase-20261024](Rebase-20261024/README.md). System rows in these historical runs are unchanged reference controls; their variation does not mean System was modified. The original array optimization is already present in #558 and is no longer a #559 production change.

# Local performance work, 2026-10-18

Base: compatibility fixes `3f682768d6b23e445ae46595e456658899e371e4`.
Branch: `perf-expression-compilation-and-runtime`. Measurements were collected locally;
draft PR publication was requested afterward. Previous pull requests remain unchanged.
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

Focused frozen-binary repeat: `00-array-repeat.csv` versus `01-vector-array-repeat.csv`.
System compilation moved 44,092 -> 58,548 ns, standard FEC 2,621 -> 3,416 ns, Light
2,425 -> 2,964 ns. Absolute times worsened with the host conditions. Relative to System,
standard FEC improved about 1.8% and Light about 8.0%, in the same direction as the first
run. The small standard-FEC difference remains uncertain; do not advertise the first
run's absolute percentages as an established universal gain.

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

Code commit: `a9b98445`. Comparing the original baseline directly with this final production
code gives Light construction improvements of 40.7%, 52.7%, 35.7% and 55.6%, respectively.
The more useful per-change comparison above uses its immediate parent as requested.

## Rejected runtime candidate: direct conditional comparison branches

Baseline benchmark commit: `37d445aa`; production baseline remains `a9b98445`.
Candidate: replace equality/inequality materialization followed by Brfalse with Bne_Un/Beq
for simple non-lifted, non-overloaded operands. Keep complex/EH cases on the existing path.
The candidate passes the six standard/Light regression methods, including primitive and
reference input matrices, NaN, nullable and overloaded exclusions, side effects and EH.

Evidence: `03-conditional-before.csv` / `03-conditional-rejected.csv` and their repeat files.
Standard compilation: 2,642 -> 1,937 ns initially, but 2,293 -> 2,943 ns on repeat.
Light compilation: 2,314 -> 2,002 ns initially, 2,475 -> 2,036 ns on repeat. System controls
also moved considerably. Standard invocation was 1.196 -> 1.096 ns initially and
1.103 -> 1.093 ns on repeat; these tiny loop-inclusive timings do not establish a runtime win.
Construction was measured in both runs as well. The inconsistent compilation results
do not justify retaining this production change. It is removed from the branch.
Retain the added workload and regression oracles for future investigations. The candidate
patch and its exact frozen binaries/hashes remain in the local measurement archive.

## Final validation and runtime interpretation

The two kept changes emit exactly the same IL as the original baseline for all five
benchmark workloads, in both standard and Light modes (ten delegate bodies compared).
No delegate runtime speedup is claimed. Original/final timings for the first four
workloads are in `00-baseline.csv` and `02-named-field.csv`; the conditional workload's
final production timing is `03-conditional-before.csv`. Candidate timing files are not
final production results. No compiler layout changes or System fallbacks were introduced.

All library targets (`net472`, `netstandard2.1`, `net8.0`, `net10.0`) build with zero warnings
and errors. Native .NET 10 and .NET 8 suites pass (1,712 / 1,711 legacy tests plus registered
regressions). Framework passes 712 TestX executions and 1,595 legacy tests, using the
existing external en-US runner. One initial unreproduced property-test failure is recorded above.

The full imported compatibility suite was rerun against frozen final compiler binaries:
34,945 pass, 152 fail, 36 normalized skips, zero observed crashes/timeouts. All outcome
groups match the fixes baseline exactly: zero newly failing or unmatched groups. The
152 pre-existing failures remain open. Skips include two compilation-mode cases of the
one known deep-tree crash, which remains deferred; its explicit Skip discovery was
verified without executing the tree body. See `validation.json` for counts and hashes.

144 benchmark measurements were collected across nine serial runs. Each kept optimization
has a separate code commit with measurements of construction, compilation and runtime.
These commits and evidence support a separate stacked performance PR. Publishing this
branch does not change the previous pull requests or the recorded measurement baseline.
