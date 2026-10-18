# Strict FEC compatibility results

Draft PR #558 depends on [#557](https://github.com/dadhi/FastExpressionCompiler/pull/557); the parent remains unchanged. The fixes branch is `fix-runtime-expression-compatibility`. [Fixes-only comparison](https://github.com/jogibear9988/FastExpressionCompiler/compare/e4c232da108d98790f0ca73f23ec90756da0cad7...fix-runtime-expression-compatibility). The upstream base remains master because the parent branch exists only in the contributor fork. Performance PR #559 is a separate branch and was not changed by this work.

The [preceding report](https://github.com/jogibear9988/FastExpressionCompiler/blob/3f682768d6b23e445ae46595e456658899e371e4/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/README.md) describes the earlier operator, TypeEqual, ref argument, array, initializer and quote fixes in this same layer.

This round is committed locally and has not been pushed. PR #558 therefore still shows its preceding remote snapshot.

## Latest results

Frozen compiler/test snapshot `22b6fe16`, Windows ARM64, .NET 10 Release, strict FEC with outer fallback disabled: **34,996 passed, 77 failed, 60 skipped** on the fixed **35,133-case** baseline. All 21 batches completed, with zero observed baseline crashes, timeouts or run-to-run instability.

Since the preceding complete snapshot (34,945 passed /152 failed /36 skipped), **51 failing cases now pass** and **24 System representation assertions are explicitly skipped after exact-fixture behavior verification**. These skips are not compiler repairs. There are no newly failing baseline cases. See [changed cases](changed-cases.csv).

The 60 skips comprise four upstream skips, 54 behavior-verified System representation cases, and two modes of the **known, deferred deep-tree crash**. That crash remains open at the user's request; zero observed crashes does not mean it was repaired. xUnit collapses skipped theories to one record, so the report preserves their original baseline case counts using explicit skip evidence.

## Additional fixes in this round

- Reject impossible method arities and duplicate labels with the expected validation exception.
- Preserve the full 16-bit character width through managed references; preserve Light Continue value payloads; validate abstract constructors in nested Light lambdas.
- Validate rethrow/filter scopes and jumps across protected exception regions.
- Invoke runtime Expression<TDelegate> values, preserving evaluation/spill order, reference aliases and property copyback. Their intrinsic Compile operation implements Invoke semantics; the outer lambda remains strict FEC. Load incoming ref delegate/expression receivers as values.
- Bind reused label targets in legal sibling scopes, preserving caller/callee and loop boundaries. Track physical method tail returns without removing their labels; the unchanged legacy Issue237 group passes.
- Support Dynamic nodes through fresh per-occurrence DLR call sites and the existing emitter, including typed results and argument spill ordering.

The exact source commits and their validation notes are recorded in [commits](commits.txt) and git history. The source retains the direct emitter and existing closure layout.

## What remains

| Remaining category | Cases |
| --- | ---: |
| Captured quotation unsupported | 28 |
| Illegal expression/block-entry validation | 14 |
| Non-Exception wrapping / runtime policy differences | 12 |
| Nonempty RuntimeVariables unsupported | 12 |
| Validation / exception contract differences | 4 |
| Upstream compiled / interpreted disagreement (runtime#19094) | 4 |
| Nested exception-filter features unsupported | 3 |

Captured quotes and nonempty RuntimeVariables need live writable variable hoisting, not copied snapshots. Non-Exception throw behavior depends on module wrapping policy. The remaining illegal expression-entry tests need fuller lexical ancestry validation. Irreducible custom nodes retain FEC's documented null/unsupported contract. Interpreter-only filter tests and upstream runtime#19094 differences remain visible rather than being presented as repaired.

A direct filter-rethrow prototype was rejected: its compiled System control crashed with AccessViolationException in isolated .NET 8 subprocesses, before the FEC delegate ran. .NET 10 controls passed. Existing FEC rejection remains. These experimental System crashes are separate from the supported FEC-suite crash count; local evidence is in `rejected-filter-rethrow/` alongside the output report.

Additional probes outside the imported baseline record two limits rather than hiding them as skips. With a reused tail label, System compiled directly returns `true`, its interpreter returns `false`, and literal lambda inlining also returns `false`. FEC Default matches the compiled form in each shape. FEC NoInvocationLambdaInlining uses a physical delegate method and returns `true` for the literal invocation; this is one known deterministic mode difference, not run-to-run instability. A reused method-tail target reached with pending operand values or from protected exception regions remains strictly unsupported (null / Result 1011) where a safe return would require broader stack spilling. See the local `method-tail-diagnostics/` evidence. No imported failure is reclassified for either limitation.

## Validation

- Standard and Light libraries: net472, netstandard2.1, net8.0, net10.0.
- 97 added regression groups / 775 standard-Light .NET 10 executions pass.
- Final test-only snapshot `6d87ca5c` rechecks all added groups on both .NET 8 and .NET 10. Compiler sources are unchanged from frozen snapshot `22b6fe16`. Three new System-oracle helpers were corrected for Framework's older exception/stack-spiller contracts; every FEC assertion remains active, with no test skips. Initial failure and successful rerun logs are preserved in the local `validation-logs/` folder.
- Native .NET 10: 1,712 legacy tests plus registered groups. Native .NET 8: 1,711 legacy tests plus registered groups.
- Framework 4.7.2: 850 TestX executions plus 1,595 legacy tests, using the external en-US wrapper.
- Seventeen exact-fixture System representation behavior proofs pass under strict FEC. Targeted verification commands in System mode pass 37/37 and 21/21 with the original imported assertions enabled; the new behavior proofs still explicitly use FEC in both modes. The established full System baseline remains 35,116 passed / 13 failed / 4 skipped (reused control).
- Imported tests retain their existing xUnit analyzer warnings. No trimming/NativeAOT execution claim.

[Failures and actual errors](failures.csv) · [Categories](remaining-failure-categories.csv) · [Summary](summary.json) · [Validation](validation.json) · [Representation exclusion proofs](layout-exclusions.csv) · [Known deferred issue](known-deferred-issues.csv)

## Commits added in this round

```text
2a460026	fix: reject lambda signatures that exceed the method parameter limit
90930908	Report duplicate label definitions with InvalidOperationException
683055d2	test: separate verified FEC behavior from System representation assertions
5ce778b6	fix: preserve the full character width through managed references
b939a9b6	Preserve value payloads on Light continue gotos
af319eeb	fix: validate abstract constructors in nested Light lambdas
25f62830	test: verify behavior before excluding interpreter representation assertions
4e967361	Validate rethrow and catch filter scopes during collection
6fffee01	Validate jumps across exception region boundaries
e3d34bd0	Support invoking runtime expression-tree values
e81fe091	Load invocation receivers by value before calling them
535d902c	test: register remaining compatibility regressions in both runners
6be85625	fix: spill invocation operands across argument jumps
62aae704	Bind reused label targets within sibling block scopes
3c1249b0	Reject label redefinition inside an active default expression
2a15c195	test: register scoped labels and exercise both invocation modes
05ab6956	Compile dynamic expressions through per-occurrence DLR call sites
fe4cf018	Keep rethrow validation inside inlined lambda boundaries
18e9a8e7	Propagate argument jumps through enclosing calls and constructors
b0b78162	Defer runtime expression compilation for structural argument spills
835d819b	Keep argument-local returns inside inlined invocation wrappers
71d17dee	Keep stateful dynamic sites distinct across nested lambda occurrences
e2750594	Preserve argument-local returns in computed delegate invocations
ba22e531	test: register final dynamic and invocation compatibility regressions
b28a984b	fix: propagate the trimming contract to argument spill metadata
789c6d35	Preserve physical method tail returns across scoped label definitions
22b6fe16	test: run method-tail return regressions in every native runner
18080093	test: keep by-ref spill behavior checks on Framework
6d87ca5c	Make exception validation test oracles portable to Framework
```
