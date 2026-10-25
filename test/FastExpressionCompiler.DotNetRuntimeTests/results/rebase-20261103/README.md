# Rebase onto upstream master, 3 November 2026

Base: fetched `dadhi/master` at `4683482e929f98a528b12a1c7fee8e232ecc36c2`.
Previously published #558 head: `caef6b45d2d77bc52b47f9e6df9601601601a709`.

The rebased history was subsequently consolidated from 188 to **115 nonempty linear commits**, with no merge commits. The backup is retained locally as `backup/pr558-before-rebase-20261103`. `commit-map.csv` records the original rebase mapping. `history-consolidation.csv` maps that 188-commit history onto the current 115 commits; the final documentation commit's mapped ID precedes the documentation-only amendment.

## Superseded code and integration

- Use master's reference-typed boxing, original boxed-object identity, unsigned division, byref character width and nonvoid list-initializer result handling. Keep the additional boundary/identity/effect regressions in renamed test commits. These commits are not empty: each retains useful coverage.
- Keep master's normal TypeIs/TypeEqual implementation and checked small-integer narrowing; retain the PR's void/pointer type-test handling and floating-point checked arithmetic corrections.
- Remove the duplicate nested-initializer emitter. Extend upstream's shared `EmitBinding`/`EmitElementInit` helpers for exception-region spilling, readonly struct copies, and null-receiver evaluation order. Preserve known-nonnull constructor receiver calls. Master's writable struct-property extension stays supported; additional regressions check one setter writeback for reference and struct owners, with and without EH on the RHS.
- Master removed `CompilerFlags.ValidateLabels`. Remove obsolete flag references and use mandatory integrated lexical/redefinition/ambiguity checks. The PR's broader scope metadata still supports inlined lambdas and exception boundaries; remove redundant upstream metadata rather than run two collection/validation traversals. A goto value is an operand scope, satisfying the new upstream diagnostic tests.
- Preserve master's new LightExpression intrinsic scope argument and parallel native runner, including shared Standard/Light parity registrations.
- Preserve modern non-Exception unwrapping filters. Framework's DynamicMethod cannot emit filters, so retain the existing anonymous-host direct-payload path there. Caller-owned Framework IL methods retain their original wrapping policy.

Unique regressions remain even when production implementations are now upstream. Rebase integration changes are incorporated into the retained history. No new skips, stack-size workarounds or closure-layout refactorings were introduced. #559 and the separate PRs are untouched.

## History consolidation

The second cleanup combines 73 commit boundaries: related implementation follow-ups, test-only regressions for now-upstream implementations, runner registrations, Framework oracle adaptations and integration corrections are incorporated into their associated feature groups. Distinct fixes and separately measured optimizations remain separate where they still provide useful review boundaries. Combined commit messages preserve all original messages, including validation details.

Before the documentation-only amendment, the complete final Git tree is identical to published head `796c135699d917db6a27ec70351001665e0df8c9`. Every consolidated commit has exactly the tree of its group's last original commit; each is nonempty. This cleanup changes history and report metadata only, with no production, test, runner or benchmark changes. The successful validation below therefore still covers the exact source and test bytes. No new performance measurements were run for this history-only operation. The pre-cleanup history is retained locally as `backup/pr558-before-history-cleanup-20261103`.

## Fresh validation

Windows ARM64, Release, SDK 10.0.301:

| Check | Result |
| --- | --- |
| Full solution build | Passed; zero warnings/errors |
| Native Standard/Light, .NET 10 | 1,173 TestX + 1,724 legacy cases passed |
| Native Standard/Light, .NET 8 | 1,173 TestX + 1,723 legacy cases passed |
| .NET Framework 4.7.2 | 1,133 TestX + 1,607 legacy cases passed |
| Imported runtime suite, .NET 10 | **35,088 passed; 59 existing skips; zero failures/errors/timeouts/aborts** |

Master moved legacy parity cases into shared TestX coverage, so the native categories are recorded separately. The imported suite uses strict Standard FEC compilation with System fallback disabled; its 35,147 total includes existing behavior proofs. Framework is invoked under Windows PowerShell/.NET Framework with en-US current/default-thread culture and UI culture for existing Issue261 English exception assertions.

`validation.json` identifies the validated code revision, counters and log hashes. `source-sha256.json` hashes tracked source/project files. `validation-logs.zip` contains the successful solution build, three native runs, imported-suite log and full TRX. The final amendment changes documentation only.

The 59 existing exclusions, including deferred deep-tree stress and System-specific IL/layout or interpreter assertions, are unchanged. Zero crashes applies to the executed suite; the deferred deep-tree issue remains open. Nested exception handling in filters remains rejected, as by System's IL compiler. No new performance or allocation claim is made. Historical benchmark records do not describe this head. NativeAOT/trimming publication was not tested.
