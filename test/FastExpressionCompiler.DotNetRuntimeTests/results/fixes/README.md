# Expression compatibility: final failure triage

Draft [PR #558](https://github.com/dadhi/FastExpressionCompiler/pull/558), branch `fix-runtime-expression-compatibility`. PRs #557 and #559 are unchanged. This report supersedes the `79ac2c74` snapshot (35,048 passed / 23 failed / 62 skipped).

## Results

Frozen source/test snapshot `d01d4c2e`, Windows ARM64 .NET 10 Release, strict FEC with outer fallback disabled: **35,064 passed, 0 failed, 69 skipped** of the fixed **35,133 original cases**. All 21 batches completed; no observed crashes, timeouts or unstable outcomes. Sixteen previous failures now pass; seven upstream assertions are excluded only after direct behavior controls. These exclusions do not add support for interpreter-only behavior.

| Previous category | Cases | Resolution |
| --- | ---: | --- |
| Non-Exception throw wrapping | 12 | Fixed generated-method hosting; original assertions pass. |
| Irreducible custom expression nodes | 4 | Test FEC's null/throwing unsupported contract directly; System's ArgumentException assertions retained. |
| Boxed/value-type assignment, runtime#19094 | 4 | Excluded interpreter expectations; fresh controls verify identical System-compiled/FEC copy behavior and the interpreter's different mutation. |
| Nested exception handling inside filters | 3 | Excluded interpreter-only expectations; exact-tree controls verify interpreter success and rejection by both IL compilers. |

The 69 skips comprise **4 upstream + 56 verified representation + 7 verified compiled/interpreter differences + 2 deferred deep-tree cases**. The known deep-tree crash remains open, as requested. One deterministic literal-Invoke reused-label inlining-mode difference remains outside this baseline: direct compiled return true versus interpreter/inlined false; FEC Default follows each compiled shape, while NoInlining uses a separate physical method and returns true. Reused method-tail returns with pending operands or protected exception regions remain strictly rejected (null / result 1011) where broader stack spilling would be needed. Zero active baseline failures does not mean these limits were implemented.

## Latest changes

The CLR applies the generated method host's non-Exception wrapping policy during catch matching. Methods containing exception regions now use anonymous DynamicMethods, matching System's policy. Raw and wrapped non-Exception payloads reach the correct handlers. Ordinary methods and the no-argument constructor fast path retain FEC's owned hosting. Both collection-skipping APIs use anonymous hosting because their exception regions are unknown before emission. Anonymous hosting adds eager JIT work during delegate creation. `CompileFastToIL` retains the caller's method-host policy. Framework also emits the required casts when reading typed constants, nested delegates and derived closure fields, so anonymous methods pass CLR verification. Framework also caches an Unbox receiver as its original object and recreates the writable address after spilling arguments, preserving eager null/type checks and once-only receiver evaluation. These verification adaptations are guarded by NETFRAMEWORK. No catch dispatcher, wrapper-unwrapping rewrite, alternate closure layout, or System fallback was added.

New native regression methods cover raw/wrapped payload identity, throwing assemblies with both wrapping policies, filter ordering and variable mutation, rethrow identity versus explicit throws, private-member access through exception regions/nested delegates/quotes, no-collection APIs, caller-provided IL, and alternating owned/anonymous methods with pooled IL and private struct signatures. Standard and Light share these controls; Framework retains explicit unsupported-filter controls while exercising supported paths.

A deterministic additional regression exposed string reference identity loss, including mixed string/interface constants. FEC now preserves every non-interned string constant's original reference through the existing closure, independently of declared or surrounding block type. Already interned literals retain ldstr emission. A read-only intern-table check occurs during compilation; delegate execution adds no lookup. No string is interned by this classification and no global cache is added. Literal string fields retain their ordinary literal semantics. The no-closure API rejects strings requiring an original bound reference. System's IL compiler can replace a string-typed constant with its interned equivalent; semantic controls use the modern System interpreter and an explicit object-bound System compiled expression. Framework uses the latter because its Compile(true) path still uses compilation. The original upstream reference assertions stay active; no additional cases are excluded.

The four invalid-node cases now assert `CompileFast(true)` returns null and the throwing unsupported flag reports `UnknownExpression`, across Default, NoInlining and Debug strict flags. They remain passing cases rather than skips; System's original ArgumentException expectations remain unchanged.

Five additional compatibility proof cases are outside the fixed original denominator. Two use fresh boxed values to prove System's compiled implementation and FEC preserve value-copy behavior, whereas the interpreter mutates the box; writable-local controls still require mutation. Three use the exact nested-filter shapes to verify System interpretation succeeds and both IL compilers reject them; simple filter controls still execute. FEC-only skip metadata names each reason. Original System data and assertions remain: its two compiled assignment rows still fail the upstream interpreter-oriented assertion, while its two interpreted rows and three nested-filter rows pass.

## Earlier implementation retained


Captured locals and arguments use shared `StrongBox<T>` cells in the existing object[] closure slots. Outer code, sibling delegates, returned quoted trees and RuntimeVariables all access the same live storage. Cells are initialized separately for each method activation, block entry and catch/filter entry, and forwarded by identity through nested delegates. This replaces copied capture values and supports stable typed addresses for nullable, reference and mutable-struct captures. Uncaptured variables keep their existing direct path. Capturing by-ref parameters, pointers and byref-like values remains rejected because these cannot be stored in heap cells.

Quotes bind only their free variables at execution time. Lambda, block and catch declarations retain lexical shadowing, explicitly typed blocks retain their type on every target, and capture-free quotes retain their original identity. Mixed local/captured RuntimeVariables preserve ordering and duplicate aliases. Label ancestry validation reuses the existing collection pass, allocates persistent ancestry only when labels or jumps occur, preserves the public collection signature and keeps constant-branch pruning unchanged. The outer lambda still compiles directly with FEC; there is no System compilation fallback or whole-tree rewrite.

Additional emitter bugs were repaired: variable scopes now pop by their encoded declaring block index rather than binding-stack length, and intermediate struct field owners no longer leave an extra duplicated managed pointer on the evaluation stack. Arithmetic, unary operators, conversions, casts and type tests clear receiver/assignment address context at by-value operand boundaries and reset address state for computed results. Captured operands therefore load values while writable field owners, explicit by-reference conversion operands and Unbox retain their existing address paths.

The imported quote helper now verifies compiled typed-cell representation for both FEC flag modes. Its System interpreter object-box assertions remain unchanged. The earlier System and strict FEC quote-focused controls each passed all 66 cases with zero skips. Obsolete FEC tests that demanded unsupported captures/RuntimeVariables now test live mutations and independent activations instead. The FEC-owned Issue347 opcode assertion was updated to the shared-cell address sequence, after checking the runtime result; it was not skipped.

Framework's System quote rewriter loses an explicitly void block body type. Its control checks the void delegate contract and observable effects, while FEC still must preserve the exact block type; both returned trees execute using System and strict FEC.

System's compiled nested-struct compound assignments have a defensive-copy discrepancy: they return the updated arithmetic result but leave the original field unchanged. FEC's tests require mutation of the original storage, and System controls with explicit once-only owner/value temporaries and plain field assignment verify those results and side effects. This is recorded explicitly rather than reproducing lost mutations.


## Validation

- Standard and Light libraries build for net472, netstandard2.1, net8.0 and net10.0 with zero warnings/errors.
- 105 added regression groups / 851 Standard-Light .NET 10 method executions pass.
- Native .NET 10: 1,712 legacy tests plus registered groups; .NET 8: 1,711 legacy tests plus registered groups.
- Framework 4.7.2: 926 TestX executions plus 1,595 legacy tests, using the existing external en-US wrapper.
- All 19 representation proofs pass; System retains and passes both original runtime-variable IL snapshots.
- Five new compatibility proof cases and four invalid-node contract cases pass separately in both harness modes. Each proof executes direct backend controls; the original baseline count stays 35,133.
- Frozen imported/compiler assembly hashes and test logs are recorded in validation.json and validation-logs. Existing imported xUnit analyzer warnings remain.

[Changed cases](changed-cases.csv) · [All original cases](all-tests.csv) · [Methods](methods.csv) · [Failures](failures.csv) · [Compatibility exclusions](compatibility-exclusions.csv) · [Representation exclusions](layout-exclusions.csv) · [Summary](summary.json) · [Validation](validation.json) · [Commits](commits.txt)

## New commits

```text
931fa0fc	Test FEC rejection contracts for irreducible expression nodes
6774772f	Classify interpreter-specific runtime assertions with compiled behavior proofs
63fa42d4	Fix non-Exception catch policy for generated methods
2043b6f7	Preserve original string constant reference identity
d01d4c2e	Keep Framework boxed receivers verifiable during argument spills
```
