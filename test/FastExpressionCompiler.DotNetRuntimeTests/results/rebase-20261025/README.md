# Rebase onto upstream master, 25 October 2026

Base: dadhi/master `0f37356fc9ca2c05b085f94592cfaf492f78eacf`.
Previously published PR #558 head: `aaa79fe73f0978e9be8150116d4175f04cb5c971`.
The local pre-rewrite integration is retained as `backup/pr558-before-clean-rebase-20261025` (`593c72e2`).

The branch is linear. The three integration merges are removed; their remaining invocation/metadata and comparison corrections are folded into the owning feature changes. The reverted explicit-quote-block experiment is removed. Upstream #562 and SmallMap implementation commits are not replayed. Additional collision coverage is retained against upstream's SmallMap implementation, with its helper-name correction folded into the test commit. Runtime-expression invocation includes the direct-lambda handling and source explanation from the previous merge/review followups.

Upstream's nullable-Boolean emitters remain the fast paths for the exact supported Boolean shapes. The broader compatibility emitters handle additional mixed operands and selected methods. Master's LightExpression constructor signature-pool return is preserved. Regression tests that already existed in upstream are retained; the PR adds only its extra coverage. Historical reports are consolidated into one documentation commit and keep their original source IDs: their timings are not fresh measurements of this rebase.

## Validation

Windows ARM64, Release, SDK 10.0.301:

| Check | Result |
| --- | --- |
| Full solution build | Passed |
| Native Standard/Light runner, .NET 10 | 1,734 passed |
| Native Standard/Light runner, .NET 8 | 1,733 passed |
| .NET Framework 4.7.2 TestX | 1,041 passed |
| .NET Framework 4.7.2 legacy runner | 1,617 passed |
| Imported runtime suite, .NET 10 | 35,088 passed; 59 skipped; 0 failed; 0 crashes |

The Framework runner contains existing assertions on English exception-message text. On this German-language host the ordinary run fails in Issue261's Standard/Light fixture. The successful validation sets both current and default-thread culture/UI culture to `en-US` before invoking the unchanged executable in Windows PowerShell/.NET Framework. The initial localized failure and successful run logs are both preserved. No unrelated test-message assertion was modified.

The imported-suite total is 35,147, including the PR's additional behavior proofs; it should not be compared as a one-to-one count with the frozen 35,133-case original import. Existing documented exclusions, including the deferred deep-tree stress cases, remain in place.

`validation-logs.zip` contains the full solution build, native runners, and imported-suite output. `commit-map.csv` maps the earlier PR commits to retained rewritten commits (or records that the commit was consolidated, superseded, or reverted). No new timing/allocation claim is made for this history cleanup.
