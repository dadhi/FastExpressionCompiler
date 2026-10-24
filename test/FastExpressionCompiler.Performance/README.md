# Current rebased comparison

The current report compares upstream master without #558, current #558, and rebased #559. System is shown separately as an unchanged reference, with no before/after claim. See [three-stage results and full evidence](Measurements/Rebase-20261024/README.md). The retained production increment is named LightExpression field lookup; the modern array classification optimization is already in #558. The BenchmarkDotNet workflow below describes the historical screening harness.

# Focused compiler performance measurements

Measure expression-tree construction (the API has no text parser), compilation of an
existing tree, and execution of the resulting delegate separately. System.Linq.Expressions
is the within-run control; standard FEC and LightExpression use strict CompileFast with
no System compiler fallback. Light trees are built directly, without conversion overhead.
The invocation setup checks all three implementations over 256 inputs before timing.

From PowerShell:

```powershell
./test/FastExpressionCompiler.Performance/Measure.ps1 -Label baseline -OutputRoot C:/temp/fec-performance
```

Every run builds Release, copies the complete output to a fresh immutable run folder,
records source/assembly hashes and host metadata, then runs BenchmarkDotNet against those
copied binaries. The in-process emit toolchain prevents project-reference rebuilds from
silently changing an archived baseline. Tiered compilation is disabled for all variants.
Use separate processes and serial runs. Keep the same SDK, runtime, architecture, power
conditions, benchmark source and job settings for comparisons. Re-run archived binaries
with FEC_PERF_OUTPUT pointing at a fresh directory to assess run-to-run drift.

Eight measured 150ms iterations follow three warmups. This is a focused screening suite,
not a universal performance claim. Inspect raw distributions and System control drift;
small differences within noise are inconclusive. Invocation timings include a shared
input/update/checksum loop, amortized across 256 calls. Do not subtract loop overhead or
claim that nanosecond differences necessarily describe real application throughput.
Construction has only two methods because standard FEC consumes System trees.

Retain full JSON, CSV, logs and hashes outside the checkout. Commit compact measurement
tables and a per-change ledger here for the eventual PR description. A code commit can
be measured before committing; record its parent and source hashes, then map it to its
final commit in the ledger. Measurements do not modify existing pull requests.
