# Follow-up merge of dadhi/master 233ed5ad

Source comment in `f089ff5b` distinguishes runtime System expression values from LightExpression AST nodes. The review reply points to `Parameter_and_concrete_expression_types_preserve_runtime_delegate_signatures`. No behavior changes were made for that comment.

Merge `7949ee5d` integrates upstream `233ed5ad`. Conflict resolution keeps #558's reference-comparison helper and method-aware shared arithmetic path, avoiding the standalone comparison emitter that this PR supersedes. Upstream's iterative conversion stripping, node-type predicate helper and editor setting are retained. #558's intrinsic-chain handling and regression tests remain in place.

Fresh validation at `7949ee5d1d7253ba50c49744980c69d72db489fb`: both production projects build across configured targets; full .NET 10 ARM64 / .NET 8 ARM64 suites pass (1,714 / 1,713 legacy tests, plus registered regressions). Framework passes 1,041 TestX and 1,597 legacy cases. All 21 imported batches pass on their first attempt: **35,064 executed passes, zero executed failures/crashes, the same 69 exclusions**. The deferred deep-tree modes remain deferred. The native tests include runtime-expression invocation and the intrinsic conversion regressions in both flavors. No fresh performance measurements are claimed for this merge; previous measurements remain source-specific.

`validation.json` records exact source/assembly hashes and counters. `validation-logs.zip` contains full build/test logs, batch results, and the validation scripts; `manifest.json` records all archive entry hashes. This evidence-only commit leaves validated code and tests unchanged.
