# Rebase onto upstream master, 3 November 2026 (second update)

Base: freshly fetched `dadhi/master` at `750e1e8ea602a7e72a90072ee80b0c1376f5b72d`.
Previous #558 head: `901d1e0e2565997d549936b22320aa2cc03e75c0`.
Validated code revision before the documentation-only amendment: `47e512f698601b4785b6e8a9a01aa19501d1016a`.

The branch contains **114 nonempty linear commits**, with no merges, down from 115. The pre-rebase branch is retained locally as `backup/pr558-before-rebase-20261103-late`. `commit-map.csv` records retained and dropped commits; the final documentation commit maps to its ID before this report was added.

## Cleanup and integration

- Drop `049db111` entirely: master merged #578's nonprogressing-reduction guards, with regression coverage equivalent to or broader than the older ExtensionReductionRegressionTests. Remove the duplicate fixture and runner registrations; retain master's ReductionProgressRegressionTests.
- Preserve master's #573 initializer parameter-info cache and cleared address state. Keep #558's exception-safe initializer evaluation on top of those helpers.
- Preserve #577's direct-call rule for methods declared on the receiver struct and constrained calls for inherited/interface methods through #558's existing shared receiver dispatcher. Keep the upstream explanatory comment in that helper.
- Preserve upstream RuntimeAccessorContractTests, CallTests, shared parity additions, and their runner registrations.
- Clarify the null-reference receiver commit: private-delegate lookup is already upstream, so only its extra regression coverage remains; the production change in that commit is null-receiver semantics.
- Rebase conflict fixes are incorporated into their owning commits. #559, #579 and #580 are unchanged by this rebase.

Compared with the previous #558 source, the production delta consists of master's initializer parameter caching and the explanatory dispatch comment. The reduction guards already matched upstream and remain present through master. No new exclusions, stack-size changes, or performance experiments were introduced.

## Fresh validation

Windows ARM64, Release, SDK 10.0.301. Native tests cover Standard and LightExpression; imported tests use strict Standard FEC with System fallback disabled.

| Check | Result |
| --- | --- |
| Full solution build | Passed; zero warnings/errors |
| Native .NET 10 | 1,175 TestX + 1,756 legacy passed |
| Native .NET 8 | 1,175 TestX + 1,755 legacy passed |
| Framework 4.7.2 | 1,135 TestX + 1,639 legacy passed |
| Imported runtime suite, .NET 10 | **35,088 passed; 59 existing skips; zero failures/errors/timeouts/aborts** |

Framework uses Windows PowerShell/.NET Framework with en-US culture. The 59 existing exclusions, including deferred deep-tree stress, are unchanged. Zero crashes applies to the executed suite. No new performance or allocation claim is made; older measurements do not describe this rebased head.

`validation.json` records counters and log hashes; `source-sha256.json` identifies source bytes. `validation-logs.zip` contains the build logs, three native runs, imported-suite log and full TRX.
