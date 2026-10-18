# Runtime-expression compatibility fixes

Improve imported runtime-expression compatibility in the existing FEC emitter



Depends on draft parent PR https://github.com/dadhi/FastExpressionCompiler/pull/557

Base branch: port-dotnet-runtime-expression-tests (e4c232da)

Local stacked branch: fix-runtime-expression-compatibility

Validated code snapshot: 6232b047

No push or second remote pull request has been created.



RESULTS — 2026-10-18, Windows ARM64, .NET 10, Release

35,133 imported cases: 34,830 pass; 270 fail; 2 unfinished after 1 crashing method; 31 skipped; 0 inconsistent; 0 timeouts.

Both original upstream compilation-mode rows exercise strict FEC, with fallback disabled.

The verified system-control snapshot from the same machine/runtime is reused (35,116 pass, 13 fail, 4 skip).

The previous full run (7c50c51b) had 34,810 passes, 301 failures, 2 unfinished and 20 skips. Improvements must distinguish passing behavior from explained skips.



WORKING

All 5,933 TypeEqual, 430 reference comparison, 208 unary increment/decrement, and 119 binary logical cases passed focused reruns. Current per-class results are below and in methods.csv.

489 added regression methods passed at this snapshot in the standard/Light .NET 10 focused runner.

Native .NET 10 passes all 1,712 legacy tests and registered TestX groups. Native .NET 8 passes all 1,711 legacy tests and registered groups. Framework 4.7.2 passes 566 TestX executions and all 1,595 legacy tests using the external en-US culture wrapper. Target-specific unsupported runtime APIs are documented; applicable strict FEC behavior checks remain active. Both libraries build net472/netstandard2.1/net8/net10 without warnings or errors. An earlier 7c50c51b native net8 process stalled and was terminated; the final snapshot passes a fresh complete run.



CHANGES

- Correct signed/unsigned and checked conversions, nullable arithmetic/Boolean logic, boxing/unboxing and boxed constant identity.

- Honor explicit unary/binary operators, lifted custom logical operators, increment/decrement methods and coalesce/compound-assignment conversion callbacks.

- Preserve decimal constant sign/scale, including signed zero and scaled integral values.

- Address nonnullable boxed value captures correctly for ref calls; reject capture storage that cannot provide a correctly typed writable address.

- Correct nonvoid Add cleanup, static member bindings, and ignored initializer results.

- Spill array, list, member-initializer and intrinsic constructor operands around exception regions while preserving effects, struct identity and exception order.

- Emit guarded exception filters with correct type selection, variable mutation, throwing-filter behavior and cleanup order; reject unsupported nested EH/jumps inside filters.

- Spill invocation and index operands around exception regions, preserving evaluation order, managed references and struct writeback.

- Correct value-type receivers, readonly defensive copies, null receiver checks, private delegate invocation, ref operands/results, multidimensional element addresses, nullable writeback and array bounds.

- Preserve nullable RHS effects across exception regions; fix truth/unary-plus methods, checked small-integer narrowing, mixed nullable shifts, and integer switch dispatch.

- Preserve typed/void label values and control flow, including backward labels and finally blocks.

- Add TypeEqual directly to the existing shared emitter, including Light conversion, null/nullable and exact runtime type behavior.

- Reject nonprogressing extension reductions and preserve TypeIs operand effects.

- Keep strict compilation: no system fallback, AST preprocessing pipeline, or test-specific production behavior.



STILL OPEN

The remaining failures include unsupported Quote/RuntimeVariables/Dynamic nodes and nested bindings, nested exception handling or jumps inside filters, validation/API differences, and genuine emission defects such as property/indexer ref writeback. These are case counts, not counts of distinct compiler bugs. See failures.csv for individual cases, including their actual error. A failure is not automatically classified as an incorrect result.

The sole known process crash is CompilerTests.CompileDeepTree_NoStackOverflow (two mode rows). Fixing it requires addressing recursive traversal/emission; no stack-size/thread workaround was added.



EXPLAINED SKIPS

31 total skips include the original 4 and 27 system-compiler IL-layout facts. Each added skip names strict FEC behavior coverage. The system-control mode retains the layout assertions. Unverified or behaviorally broken layout cases remain failures. Two TailCallThenReturn theory rows still execute their behavior and omit only System-specific IL/interpreter layout comparisons. Every exclusion and its proof is listed in layout-exclusions.csv. No relative IL speed claim is made.

Nine obsolete native issue #352 opcode assertions are documented as disabled; their strict FEC execution assertions remain active, with additional null/writeback/setter coverage.



VALIDATION LIMITS

The outcome totals belong to 6232b047. The final report records the validated source snapshot; a later documentation-only commit may follow.

Process markers identify a crashing method, not an independently reproduced crashing data row. Unfinished cases are reported separately from failures.



LOCAL LOGICAL COMMITS

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



CLASSES WITH OPEN FAILURES/CRASHES
System.Linq.Expressions.Tests.UnaryQuoteTests: 4 pass, 38 fail, 0 unfinished
System.Linq.Expressions.Tests.ExceptionHandlingExpressions: 141 pass, 28 fail, 0 unfinished
System.Linq.Expressions.Tests.ListBindTests: 15 pass, 28 fail, 0 unfinished
System.Linq.Expressions.Tests.Compiler_Tests: 157 pass, 22 fail, 0 unfinished
System.Linq.Expressions.Tests.MemberBindTests: 12 pass, 16 fail, 0 unfinished
System.Linq.Expressions.Tests.Goto: 83 pass, 12 fail, 0 unfinished
System.Linq.Expressions.Tests.RuntimeVariablesTests: 8 pass, 12 fail, 0 unfinished
System.Linq.Expressions.Tests.CallTests: 105 pass, 10 fail, 0 unfinished
System.Linq.Expressions.Tests.LoopTests: 19 pass, 10 fail, 0 unfinished
System.Linq.Expressions.Tests.StackSpillerTests: 42 pass, 10 fail, 0 unfinished
System.Linq.Expressions.Tests.Break: 83 pass, 8 fail, 0 unfinished
System.Linq.Expressions.Tests.CompilerTests: 1 pass, 8 fail, 2 unfinished
System.Linq.Expressions.Tests.Return: 85 pass, 8 fail, 0 unfinished
System.Linq.Expressions.Tests.LambdaTests: 70 pass, 7 fail, 0 unfinished
System.Linq.Expressions.Tests.ArrayAccessTests: 14 pass, 6 fail, 0 unfinished
System.Linq.Expressions.Tests.Continue: 31 pass, 6 fail, 0 unfinished
System.Linq.Expressions.Tests.ExpressionTests: 127 pass, 6 fail, 0 unfinished
System.Linq.Expressions.Tests.Assign: 71 pass, 4 fail, 0 unfinished
System.Linq.Expressions.Tests.IndexExpressionTests: 36 pass, 4 fail, 0 unfinished
System.Linq.Expressions.Tests.NewWithByRefParameterTests: 8 pass, 4 fail, 0 unfinished
System.Linq.Expressions.Tests.SwitchTests: 83 pass, 4 fail, 0 unfinished
System.Linq.Expressions.Tests.InterpreterTests: 5 pass, 3 fail, 0 unfinished
System.Dynamic.Tests.BinaryOperationTests: 2266 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.ArrayIndexTests: 176 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.BindTests: 31 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.ConstantTests: 108 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.InvocationTests: 32 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.MemberInitTests: 6 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.NewArrayListTests: 103 pass, 2 fail, 0 unfinished
System.Linq.Expressions.Tests.NewTests: 79 pass, 2 fail, 0 unfinished
