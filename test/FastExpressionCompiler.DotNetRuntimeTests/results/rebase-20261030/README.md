# Rebase onto upstream master, 30 October 2026

Base: fetched `dadhi/master` at `6702eab6` (full SHA in `validation.json`).
Previously published PR #558 head: `02a794a9ae45395027372bd71022411cab06b64c`.

The branch contains 188 nonempty linear commits and no merge commits. The pre-rebase branch is retained locally as `backup/pr558-before-rebase-20261030`. `commit-map.csv` maps the previous published history to the rewritten commits. The final documentation commit is amended to include this report; its mapped ID is the snapshot before that documentation-only amendment.

## What was removed or adapted

- Master now supplies the numeric conversion and typed/lifted increment/decrement implementations. Their replacement implementations are removed from this PR; the additional boundary, nullable, custom numeric, pre/post, ref-target regressions and runner failure reporting are retained. These commits are renamed to describe their remaining scope.
- Coalesce value conversion uses master's implementation. The redundant general conversion helper is removed. Captured conversion callbacks still use the PR's existing nested-lambda path; operand value-context and result-address fixes remain. The unreachable callback alternatives in master's value-conversion lowering are removed from that path.
- Switch duplicate detection uses master's implementation. The PR retains the restriction on using 64-bit values as IL switch-table indices. Switch test values are collected once, using the scope-aware collector, rather than both upstream's and the PR's loops.
- Master fixes basic typed and void jump cases. The remaining scoped-label, exception-region and physical method-tail behavior still requires the PR's broader implementation and regression tests. The duplicate standalone label-validation traversal and the superseded duplicate-label tracking are removed when the existing integrated scope metadata takes over.
- Preserve master's **opt-in** `CompilerFlags.ValidateLabels` contract for lexical/redefinition/ambiguity diagnostics. Rejection tests explicitly request the flag; valid lexical cases exercise both settings. Exception-region safety and undefined/cross-lambda binding diagnostics remain enforced. No second label-validation tree walk is added.
- Keep master's shared unary implementation and adapt explicit unary method metadata and address/value flags to it. Prevent double dereferencing nullable byref operands that the PR's value-context emitter has already loaded.
- Preserve required small-integer checked narrowing when a same-TypeCode conversion is requested after arithmetic; master's same-type shortcut remains for wider types. Keep the existing checked arithmetic regressions, including floating-point cases.

The rebase corrections are folded into their owning commits. The primitive Not value-context correction, previously carried with the first conversion commit, now belongs to the Not regression/fix commit. There are no standalone merge-fix commits, new skips, stack-size workarounds, or new closure-layout refactorings. Unique regressions keep their commits even when the corresponding implementation is now upstream. Stacked PR #559 is unchanged.

## Fresh validation

Windows ARM64, Release, SDK 10.0.301:

| Check | Result |
| --- | --- |
| Full solution build | Passed; zero warnings/errors |
| Native Standard/Light, .NET 10 | All registered TestX cases and 1,760 legacy cases passed |
| Native Standard/Light, .NET 8 | All registered TestX cases and 1,759 legacy cases passed |
| .NET Framework 4.7.2 | 1,045 TestX and 1,643 legacy cases passed |
| Imported runtime suite, .NET 10 | **35,088 passed; 59 existing skips; zero failures/errors/timeouts/aborts** |

Modern legacy totals exclude their separate TestX run. The native runners cover Standard and LightExpression. The imported suite covers strict Standard FEC compilation with fallback disabled, including upstream's ValidateLabels flag in the adapter. Its total remains 35,147, including previously added behavior proofs.

The Framework runner is invoked with en-US current/default-thread culture and UI culture because existing Issue261 assertions require English exception-message text. Those assertions and production code are unchanged for this invocation.

The 59 documented exclusions are unchanged, including deferred deep-tree stress and System IL/layout or interpreter-specific assertions. Zero crashes describes the executed suite with these exclusions; it does not claim the deferred deep-tree crash is fixed.

`validation.json` records the validated code revision before the documentation-only amendment, TRX counters, and log hashes. `source-sha256.json` hashes tracked source/project files. `validation-logs.zip` contains the fresh successful solution build, three native runner logs, and imported-suite log. Source bytes are unchanged by the history rewrite, apart from preserving the regression file's original line endings; the amendment changes only report files.

Reproduction: build the solution in Release, execute the native runner's net10.0/net8.0 binaries, execute the Framework runner with the culture described above, and run `dotnet test test/FastExpressionCompiler.DotNetRuntimeTests -c Release --no-build --no-restore`.

No timing/allocation claims were made for this rebase. Older benchmark records remain historical and are not measurements of this head. NativeAOT/trimming publication was not tested.
