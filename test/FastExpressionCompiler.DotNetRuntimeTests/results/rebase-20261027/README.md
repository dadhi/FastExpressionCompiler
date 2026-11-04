# Rebase onto upstream master, 27 October 2026

Base: fetched `dadhi/master` at `ca552c9f9b8ac95dbc3e6b58534347aa979ad51e`.
Previously published PR #558 head: `dd989683d484bfc4a8c13e65373f528314885f24`.

The rebased branch contains 188 linear commits, with no merge commits. The pre-rebase branch is retained locally as `backup/pr558-before-rebase-20261027`. `commit-map.csv` maps all 189 previously published commits to the rewritten history; the final documentation commit is amended to include this report, without changing the validated code. Its mapped ID therefore identifies the documentation snapshot before that amendment.

## History cleanup

Master has incorporated the lifted arithmetic, Not/OnesComplement, increment/decrement, lifted Not/complement method, exact TypeEqual, checked/nullable negation, truth/nullable IsFalse, and unary plus implementations originally carried here. The eight corresponding commits now retain their additional regression tests without replaying superseded emitter implementations. The code-only reference-comparison refactor (`ce6ba472`) is folded into the original reference-identity fix and removed as a separate commit.

Master's optimized unary and TypeIs/TypeEqual helpers, selected-operator fast path, and LightExpression lifting metadata remain in use. The remaining compatibility cases are adapted to those helpers. Rebase corrections are folded into their owning commits:

- Restore value/address tracking for Unbox and nullable byref type tests; static value calls must not receive an unbox address merely because Call is set.
- Preserve explicit selected binary/comparison methods through the common arithmetic fallback and consume ordinary comparison operands as values. Existing operand spilling remains in use. Parameter metadata caching starts only once its helper is introduced in the history.
- Preserve method-parameter byref conversion storage and remove a conflicting obsolete address block.
- Use ordinary floating opcodes for checked float/double add, subtract and multiply. Extend the checked arithmetic regression fixture with System-oracle finite, infinity, NaN and nullable cases.
- Compare mixed nullable shift metadata with System rather than assuming both operands determine IsLifted. Result, null and evaluation-order checks remain.

No new test exclusions were added. No global closure layout change, full-tree lowering, or stack-size workaround was introduced. Stacked PR #559 is untouched and will require its own rebase.

## Validation

Windows ARM64, Release, SDK 10.0.301:

| Check | Result |
| --- | --- |
| Full solution build | Passed |
| Native Standard/Light, .NET 10 | All registered TestX cases and 1,749 legacy cases passed |
| Native Standard/Light, .NET 8 | All registered TestX cases and 1,748 legacy cases passed |
| .NET Framework 4.7.2 | 1,045 TestX and 1,632 legacy cases passed |
| Imported runtime suite, .NET 10 | 35,088 passed; 59 skipped; 0 failures, errors, timeouts or aborts |

The modern native runner's legacy total excludes its separate registered TestX run. The imported suite covers strict Standard FEC compilation, with System fallback disabled; the native suites cover both Standard and LightExpression. The imported total is 35,147, including earlier added behavior proofs; it is not the original frozen import's total.

The Framework runner uses en-US current/default-thread culture and UI culture for existing assertions on English exception-message text. Those assertions and production code were not changed for this invocation.

The 59 existing documented exclusions remain, including the deferred deep-tree stress test and exact System IL/layout or interpreter expectations. Zero crashes describes this executed suite with those exclusions, not the deferred deep-tree case.

`validation.json` records the code revision before the documentation-only amendment and aggregate TRX counters. `source-sha256.json` records tracked source/project file hashes. `validation-logs.zip` contains the fresh successful build and four runner logs, with SHA-256 hashes in the summary. Reproduction uses the solution Release build, the native runner's net10.0/net8.0 binaries, the Framework runner under the culture described above, and `dotnet test test/FastExpressionCompiler.DotNetRuntimeTests -c Release --no-build --no-restore`.

No new timing or allocation measurement was performed for this rebase. Historical benchmark reports retain their original revision IDs and do not describe this head's performance. NativeAOT and trimming publication were not validated.
