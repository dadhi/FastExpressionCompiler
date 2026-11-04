# Rebase onto current dadhi/master (20 October 2026)

PR #558 was rebased from `187b9b8f5505b9b69708170a4da13f440ca7a12f` onto `3aefb5e58cd3bdd92d5f2fb00e72692ab3c08c67`. A local backup branch preserves the former history. The upstream base now includes the merged #557 and dadhi's map-to-set cleanup. #559 was not rewritten.

Conflict resolutions preserve the existing exception/invocation compatibility behavior while adopting `SmallSet4` for both complex-argument and invocation-jump membership tracking. They do not restore the removed `NoValue` type. The Issue476 regression test uses upstream's renamed `AddSureAbsentDefaultAndGetValueRef` helper. The already-upstream Light null-conversion preservation commit was dropped as redundant; upstream round-trip fixes/tests and README changes are retained.

The full range comparison is in [range-diff.txt](range-diff.txt). The final source integration delta versus the previous published head is in [source-delta.diff](source-delta.diff), with the overall file summary in [rebase-summary.txt](rebase-summary.txt). A normalized-line-ending comparison confirms that the core compiler is exactly the former source plus the specified set type/member substitutions; [integration-check.json](integration-check.json) records the hashes and replacement counts. Earlier reports retain their original measured/validated commit IDs; the mapping makes the rewritten history traceable. No experimental packed-flag, region-packing, quote-visitor or inlining proposal from the prior review study was added.

Fresh validation at `5ce561748e4eaf53f63242e50050bdf8429a518b`:

- Both production projects build across their configured targets.
- .NET 10 native suites pass, including 1,714 legacy cases.
- .NET 8 native suites pass, including 1,713 legacy cases.
- Framework passes 1,041 TestX and 1,597 legacy cases.
- All 21 imported batches complete on the first attempt: **35,064 cases pass, zero executed failures/crashes**. The **69 existing exclusions** are unchanged; the deferred deep-tree crash remains excluded.

[validation.json](validation.json) records exact source/assembly hashes and counters. [validation-logs.zip](validation-logs.zip) contains all build/test logs, batch results and validation scripts; [manifest.json](manifest.json) records the archive and every entry's SHA-256. The final evidence-only commit does not alter validated production or test source.

Previous performance/allocation measurements remain source-specific historical results. This rebase was correctness-validated; no new timing result or claim of performance parity on the new base is made.
