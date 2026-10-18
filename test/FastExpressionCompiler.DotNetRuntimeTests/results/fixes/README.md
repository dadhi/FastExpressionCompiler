# Strict FEC compatibility results

## Stack

Depends on https://github.com/dadhi/FastExpressionCompiler/pull/557. Parent head remains `e4c232da108d98790f0ca73f23ec90756da0cad7`; #557 is unchanged.

This draft is hosted in **dadhi/FastExpressionCompiler**. Its base is temporarily `master`: the parent head branch exists only in the contributor fork and this account cannot create an upstream base branch. Rebase this layer after #557 merges, or retarget if the maintainer creates that upstream branch.

**Review this layer:** [fixes-only comparison](https://github.com/jogibear9988/FastExpressionCompiler/compare/e4c232da108d98790f0ca73f23ec90756da0cad7...fix-runtime-expression-compatibility).

## Problem and changes

The imported runtime expression suite exposed wrong results, invalid IL and unsupported cases under strict FEC compilation. Fix these in the existing direct emitter and LightExpression layout, with logical, validated commits. No System compiler fallback, AST rewrite pipeline, global capture cache, stack-size workaround or test-specific production behavior.

- Preserve numeric/nullable/operator semantics, exact TypeEqual behavior, value/reference identities, control flow and exception cleanup.
- Fix array rank-one/non-zero lower bounds and typed element addresses; property/indexer ref argument copyback for methods, constructors and Invoke; readonly defensive copies and writable static field addresses.
- Preserve writable Unbox/array.Get receiver addresses across exception arguments. Use formal-typed temporaries without copyback for reference-compatible ref arguments, retaining exact typed aliases and runtime covariance checks.
- Emit nested member/list bindings, including struct field mutation, cached property getters, side effects and nonvoid Add cleanup.
- Support capture-free quotation, immediate quoted-lambda invocation and empty RuntimeVariables. Preserve quote identities across nested Light compilation; mutable captured quotation and nonempty RuntimeVariables remain unsupported.
- Collect runtime switch test expressions, retain ordered evaluation and void results, preserve TypeDelegator/TypeBuilder identity and reduce custom expression node types through their standard contract.
- Reject abstract constructor calls before returning an invalid delegate; preserve the valid constructor fast paths.
- Add **81 regression groups / 627 standard-Light .NET 10 executions**, registered in modern and Framework runners.

## Measured results

Full strict-FEC run against frozen compiler/test snapshot `cbb68ede`: **35,133 baseline imported cases**, Windows ARM64, .NET 10 Release.

| Outcome | Cases |
| --- | ---: |
| Passed | **34,945** |
| Failed assertions / unsupported or compatibility cases | **152** |
| Skipped | **36** |
| Unfinished after an observed crash | **0** |
| Observed inconsistent results | **0** |
| Timeouts | **0** |

Compared with the preceding complete snapshot (`6232b047`): **115 more passes**, **118 fewer failures**. The two previously unfinished deep-tree cases are now explicitly deferred, not repaired.

**Known crash remains open:** `CompilerTests.CompileDeepTree_NoStackOverflow` is temporarily skipped at the user's request to avoid a broad traversal refactor. Thus this run observes zero crashes; it does **not** establish that the compiler's deep-tree crash is fixed. System control remains enabled. xUnit v2 emits one skipped-theory record; the report accounts for its two original mode rows on the fixed baseline.

The **36 skips** comprise 4 original skips, **30 behavior-verified System-specific IL facts**, and **2 known deep-tree cases**. Two TailCallThenReturn rows still execute behavior and omit only System-specific layout comparisons. Every IL exclusion names strict-FEC behavior coverage. No relative IL speed claim is made.

## What works and what remains open

All **627 added regression executions pass**. The latest isolated full CallTests and SwitchTests classes pass **115/115** and **87/87**; the eight non-zero-bound/rank-one array cases and ten ref-call target rows pass. Earlier coverage includes all 5,933 TypeEqual, 430 reference-comparison, 208 unary increment/decrement and 119 binary logical cases.

Remaining failures include captured quotation/nonempty RuntimeVariables/Dynamic features; nested EH or jumps in filters; legal repeated labels in separate lexical scopes; invalid-tree validation/API differences; and System-specific debugger/interpreter/runtime expectations. The CSV lists every actual remaining assertion and error rather than treating all 152 rows as distinct compiler bugs.

| Remaining category | Cases |
| --- | ---: |
| Validation / exception contract differences | 57 |
| Captured quotation / computed expression invocation unsupported | 30 |
| System expected-invalid-IL static binding assertions | 14 |
| Non-Exception wrapping / runtime policy differences | 12 |
| Nonempty RuntimeVariables unsupported | 12 |
| Valid label scope support gap (deferred) | 8 |
| System private closure / debugger layout expectations | 6 |
| System interpreter / stack trace expectations | 4 |
| Upstream compiled / interpreted disagreement (runtime#19094) | 4 |
| Nested exception-filter features unsupported | 3 |
| Dynamic nodes unsupported | 2 |

Some expected failures describe System behavior that FEC should not reproduce. Static nested-binding tests expect InvalidProgramException even though the new FEC initializer behavior executes correctly. Two boxed-struct assignment tests also fail with the System IL compiler and pass with its interpreter (upstream runtime#19094); a proposed box-mutation change was rejected because it broke compiled semantics. These remain visible compatibility differences, not silently skipped successes.

## Validation

- Both libraries build net472, netstandard2.1, net8.0 and net10.0 without warnings/errors. Imported tests retain their 16 existing xUnit analyzer warnings.
- Native .NET 10: all **1,712** legacy tests and registered TestX groups pass.
- Native .NET 8: all **1,711** legacy tests and registered TestX groups pass.
- Framework 4.7.2: **704 TestX executions + 1,595 legacy tests** pass using the external en-US culture wrapper.
- Framework's invalid System oracles use explicit FEC value/mutation/effect assertions; unsupported DynamicMethod ref-return signatures explicitly assert rejection from both compilers. No FEC exception is suppressed.
- Full imported suite completes all 21 recoverable batches; the deferred stress theory records its explicit skip reason. No observed inconsistent outcomes or timeouts.
- The reused verified System control on the same machine/runtime has 35,116 passes, 13 failures and 4 skips. This is the established control, not a claim of a new full System run.

[Detailed report](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/README.md) · [Failures and actual errors](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/failures.csv) · [Summary](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/summary.json) · [Validation](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/validation.json) · [IL exclusion proofs](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/layout-exclusions.csv) · [Deferred crash](https://github.com/jogibear9988/FastExpressionCompiler/blob/fix-runtime-expression-compatibility/test/FastExpressionCompiler.DotNetRuntimeTests/results/fixes/known-deferred-issues.csv)

## Logical commits

```text
9952f0df fix: preserve numeric conversion signedness and overflow checks
c747288e fix: emit nullable Boolean operators with three-valued logic
6acf6a99 fix: reject extension reductions that make no progress
7bd9d97f fix: box value operands before emitting type tests
b1fbd154 test: run compatibility regressions on .NET Framework
f7b6cfdd docs: record compatibility fixes and fresh full-suite outcomes
c9e5e702 fix: box constants declared as reference base types and interfaces
95f8f01c fix: unbox values converted from ValueType and nullable interfaces
e3a1b653 fix: load nullable arithmetic locals as values
172d7f97 fix: preserve null when boxing nullable values to reference types
2188ecb4 fix: normalize nullable and wide array bounds before allocation
989fea1d fix: resolve lifted arithmetic operators on underlying operand types
d933190e fix: emit unsigned division for unsigned arithmetic results
3029fc0f fix: emit lifted and method-based ones complement safely
01f1455a fix: dereference by-ref operands when emitting conversions
cebb5e6b fix: emit typed and lifted increment and decrement operations
3dde6d9f fix: honor explicit methods for increment and decrement assignments
d2e26dfb fix: emit valid instance calls and index access on struct receivers
71e6a2b1 fix: preserve nullable and unsigned numeric comparison semantics
76dd92b0 fix: convert both nullable coalesce branches to the result type
da9cb6b8 fix: discard values and preserve control flow for void label targets
0f114f15 test: use a portable explicit conversion oracle for coalesce
b12baa1c fix: reuse lifted operator emission for unary increment and decrement
7a0dec97 fix: route typed jumps through their actual label targets
3b8cecd9 fix: preserve reference identity in equality expressions
d267064c fix: emit user-defined conditional logical operators
76a96e2d fix: invoke coalesce conversion callbacks on non-null operands
533dadba fix: honor custom Not methods in conditionals and interpretation
5197b237 fix: lift method-based Not and OnesComplement operands
b6a745ea fix: preserve original boxed constant identity in reference contexts
23377d0d fix: honor explicit binary methods in arithmetic and compound assignment
1be4d838 feat: compile exact runtime TypeEqual tests in the existing emitter
4bf73bde fix: apply compound assignment conversions before nullable writeback
21cf1fe6 refactor: reuse comparison operand emission for reference identity
286e36f1 test: skip compiler-layout assertions only after strict behavior validation
c6b107c0 fix: spill compound assignment targets around complex right operands
92d8e8da fix: preserve checked overflow and nullable negation semantics
f5876161 fix: evaluate nullable arithmetic right operands before null propagation
177d037d fix: preserve pending arithmetic values around nested exception blocks
677081b2 fix: honor truth operator methods and nullable IsFalse semantics
5a12cd63 fix: narrow checked short arithmetic results with overflow detection
75d9e735 fix: constrain integer switch tables to valid unique 32-bit indices
4e027b83 fix: preserve comparison methods and identity through conditional reductions
ac26916b fix: lift shifts with nullable counts and nonnullable left operands
fea3afc6 fix: resolve nonpublic delegate Invoke methods
d40efbb3 fix: check null reference receivers for nonvirtual instance calls
25eb7ab3 fix: copy readonly value fields before calling mutating members
6faf8891 fix: address multidimensional array elements for value member access
89012bd8 fix: invoke custom unary-plus methods instead of returning the operand
6e6fa763 fix: pass addresses to explicit ref-parameter conversion methods
6cc1876a fix: make invocation results addressable for ref arguments
66e3af8b fix: preserve direct value-type calls through ref receivers
0a8c33d5 fix: preserve decimal constant sign and scale
9a713fd3 fix: lift user-defined conditional logical operators
8de23f5d feat: emit guarded exception filters in the existing try emitter
01852648 fix: spill delegate targets and arguments around exception regions
7c50c51b fix: spill index receivers and arguments around exception regions
ffd3cfa9 docs: record full compatibility checkpoint at 7c50c51b
119bda8d fix: discard unused list initializer Add results
68c03a81 fix: preserve strict filter rejection on .NET Framework
a35253b1 test: make regression oracles portable to .NET Framework
fb3fdc53 fix: load writable boxed capture addresses for ref arguments
8436e668 fix: initialize static members without instance operands
e01e364d fix: spill array bounds and initializer values around exception regions
44c5181f fix: spill list initializer operands around exception regions
08d3461c fix: spill member initializer receivers and binding values around exception regions
2c9c4b98 fix: share constructor argument spilling for intrinsic expression nodes
6232b047 test: exclude proven system compiler layout assertions with reasons
b4d9fc2b docs: report validated compatibility results at 6232b047
684f1296 fix: emit actual array accessors for rank-one non-vector arrays
baf75516 feat: emit capture-free quotes and empty runtime-variable views
946e9c22 fix: compile direct quoted lambda invocations through existing emitters
0d6cba40 feat: emit nested member and list initializer bindings
6a39519c fix: preserve typed ref arguments and property copyback across calls
16fe2ebc fix: preserve readonly copies and typed static field addresses
46a54c14 fix: preserve non-runtime Type constant identities
d15b6b83 fix: reduce custom expression node types through the standard contract
7a0bcc3e test: cover quoted lambda indexer arguments
6dc2d46a fix: preserve runtime switch test evaluation and void results
1158c932 test: temporarily defer the known deep-expression stack crash
7ce406f2 test: exclude the now-verified inline-ref IL layout oracle
ec6da89a fix: reject abstract constructor calls before returning a delegate
f41d0443 fix: preserve quotation identity across nested Light compilation
3fecc464 test: retain Framework FEC behavior checks when its system oracle is invalid
e86133f3 test: verify Framework ref-call semantics without invalid system oracles
c8171b51 test: verify nested initializer behavior before excluding system IL oracles
4a23fd27 fix: preserve writable Unbox and runtime array receiver storage
ac403d05 docs: describe partial quotation and runtime-variable support
cbb68ede fix: prepare ref arguments using their formal storage types
```
