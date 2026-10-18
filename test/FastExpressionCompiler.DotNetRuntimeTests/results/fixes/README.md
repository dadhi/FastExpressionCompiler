# Captured quotations, runtime variables and lexical scope

Branch `fix-runtime-expression-compatibility`, draft [PR #558](https://github.com/dadhi/FastExpressionCompiler/pull/558). These commits are local and have not been pushed. Performance PR #559 is unchanged. The preceding local snapshot `05110348` recorded 34,996 passed / 77 failed / 60 skipped.

## Results

Frozen compiler/test snapshot `3cf56a5c`, Windows ARM64 .NET 10 Release, strict FEC with outer fallback disabled: **35,048 passed, 23 failed, 62 skipped** of 35,133 original baseline cases. All 21 batches completed with no observed baseline crashes or timeouts. There are no newly failing baseline cases. 52 previously failing cases now pass; two exact-IL snapshots are newly skipped only after strict FEC/System behavior proofs.

| Repaired category | Cases |
| --- | ---: |
| Captured quotation unsupported | 28 |
| Illegal expression/block-entry validation | 14 |
| Nonempty RuntimeVariables unsupported | 10 |

The 62 skips comprise four upstream cases, 56 behavior-verified representation cases (including two new RuntimeVariables IL-only snapshots), and two modes of the known deep-tree crash. The deep-tree issue remains open and deferred as requested. One previously documented deterministic reused-label inlining-mode difference remains outside this baseline; it is not run-to-run instability.

## Implementation

Captured locals and arguments use shared `StrongBox<T>` cells in the existing object[] closure slots. Outer code, sibling delegates, returned quoted trees and RuntimeVariables all access the same live storage. Cells are initialized separately for each method activation, block entry and catch/filter entry, and forwarded by identity through nested delegates. This replaces copied capture values and supports stable typed addresses for nullable, reference and mutable-struct captures. Uncaptured variables keep their existing direct path. Capturing by-ref parameters, pointers and byref-like values remains rejected because these cannot be stored in heap cells.

Quotes bind only their free variables at execution time. Lambda, block and catch declarations retain lexical shadowing, explicitly typed blocks retain their type on every target, and capture-free quotes retain their original identity. Mixed local/captured RuntimeVariables preserve ordering and duplicate aliases. Label ancestry validation reuses the existing collection pass, allocates persistent ancestry only when labels or jumps occur, preserves the public collection signature and keeps constant-branch pruning unchanged. The outer lambda still compiles directly with FEC; there is no System compilation fallback or whole-tree rewrite.

Additional emitter bugs were repaired: variable scopes now pop by their encoded declaring block index rather than binding-stack length, and intermediate struct field owners no longer leave an extra duplicated managed pointer on the evaluation stack. Arithmetic, unary operators, conversions, casts and type tests clear receiver/assignment address context at by-value operand boundaries and reset address state for computed results. Captured operands therefore load values while writable field owners, explicit by-reference conversion operands and Unbox retain their existing address paths.

The imported quote helper now verifies compiled typed-cell representation for both FEC flag modes. Its System interpreter object-box assertions remain unchanged. System and strict FEC focused runs each pass all 66 cases with zero skips. Obsolete FEC tests that demanded unsupported captures/RuntimeVariables now test live mutations and independent activations instead. The FEC-owned Issue347 opcode assertion was updated to the shared-cell address sequence, after checking the runtime result; it was not skipped.

Framework's System quote rewriter loses an explicitly void block body type. Its control checks the void delegate contract and observable effects, while FEC still must preserve the exact block type; both returned trees execute using System and strict FEC.

System's compiled nested-struct compound assignments have a defensive-copy discrepancy: they return the updated arithmetic result but leave the original field unchanged. FEC's tests require mutation of the original storage, and System controls with explicit once-only owner/value temporaries and plain field assignment verify those results and side effects. This is recorded explicitly rather than reproducing lost mutations.

## Validation

- Standard and Light libraries build for net472, netstandard2.1, net8.0 and net10.0 with zero warnings/errors.
- 102 added regression groups / 829 Standard-Light .NET 10 executions pass, including live sibling writes, independent activations, block reentry, nested quotes, shadowing, reference/nullable/struct cells, filters, 70 captures, aliases, typing, struct operator receivers and legal/illegal lexical jump scopes.
- Native .NET 10: 1,712 legacy tests plus registered regression groups; .NET 8: 1,711 legacy tests plus registered groups.
- Framework 4.7.2: 904 TestX executions plus 1,595 legacy tests, using the existing external en-US wrapper.
- All 19 representation-behavior proof methods pass, including the two new RuntimeVariables expression controls. System mode retains and passes both original IL snapshots. Imported tests retain their existing xUnit analyzer warnings.
- Compiler, imported tests and native/regression tests were rebuilt from the same frozen snapshot `3cf56a5c`; assembly hashes are recorded in validation.json. The native opcode expectation reflects typed shared cells. Framework's DynamicMethod filters remain unsupported: the test asserts both compilers' rejection and exercises live catch cells without a filter, while modern targets retain the filter/handler alias comparison. Earlier opcode, platform-control and quoted-block typing failures are preserved beside the successful fresh run in local `validation-logs/` evidence.

## Remaining failures

| Remaining category | Cases |
| --- | ---: |
| Non-Exception wrapping / runtime policy differences | 12 |
| Validation / exception contract differences | 4 |
| Upstream compiled / interpreted disagreement (runtime#19094) | 4 |
| Nested exception-filter features unsupported | 3 |

Non-Exception throw failures involve module wrapping policy. The custom-extension cases retain FEC's unsupported/null contract; upstream runtime#19094 disagreements and unsupported nested filter features remain visible. No deep-tree refactor was attempted.

[Changed cases](changed-cases.csv) · [All methods](methods.csv) · [Failures and errors](failures.csv) · [Categories](remaining-failure-categories.csv) · [Summary](summary.json) · [Validation](validation.json) · [Commits](commits.txt)

## Commits

```text
76e6500a	fix: restore the declaring variable scope when leaving nested blocks
13d5cdaa	fix: duplicate only the final assignment field address
cdb0b13b	feat: bind captured quotes and runtime variables to shared typed cells
0e59bb2c	test: verify shared struct-cell IL after executing its behavior
69bfb732	test: retain Framework filter rejection while checking catch quote cells
a057bef4	fix: preserve explicit block types when binding quoted captures
6631f514	Revert "fix: preserve explicit block types when binding quoted captures"
e9b97427	fix: keep struct operator operands separate from receiver addresses
de486914	test: account for Framework System quote block typing
0c07d7b2	fix: validate lexical scope of label jumps
aca7dc1d	Verify runtime-variable spill behavior before excluding IL snapshots
3cf56a5c	Keep unary and conversion operands in value contexts
```
