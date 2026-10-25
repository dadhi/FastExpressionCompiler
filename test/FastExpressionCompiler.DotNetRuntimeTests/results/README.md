# Compatibility results: 18 October 2026

This is the original import snapshot. Subsequent compiler changes and current results are documented in the [fixes report](fixes/README.md); the historical counts and limitations below describe the original measured commit.

Measured with Windows ARM64, .NET SDK 10.0.301, .NET runtime 10.0.9, Release, against FastExpressionCompiler commit `1d2a4bd364bd5271f5594074f76e298f5afa53db`. The upstream System.Linq.Expressions suite is pinned at dotnet/runtime commit `ec0059c08c077e839c6eb8827c846530a891d7e2`.

## Overall results

| Outcome | Cases |
| --- | ---: |
| Passed | 23,322 |
| Failed | 2,571 |
| Did not finish after a method crashed | 9,228 |
| Did not finish after a method timed out | 2 |
| Inconsistent across attempts | 6 |
| Skipped | 4 |
| Total | 35,133 |

There were 91 crashing methods and one timed-out method. Cases remaining in a crashed method are **unexecuted**, not individually confirmed failures. All 21 class groups completed after isolating those methods. The corrected system control passed 35,116 cases, failed 13, and skipped 4.

Both upstream boolean compilation modes exercise strict FEC with fallback disabled. They are repeated FEC executions, not FEC interpreter support. Totals include API-only checks; the `scope` column distinguishes methods observed requesting compilation. Some data rows have indistinguishable TRX IDs/display names and are grouped with explicit counts. Random GUID/date labels are normalized for run matching only. Inconsistent outcomes are retained rather than choosing the more favorable observation.

## What works

Examples of entirely passing classes that include compiler-requesting methods:

| Class | Passing cases in compiler-requesting methods |
| --- | ---: |
| ArrayArrayIndexTests | 160 |
| BinaryEqualTests | 136 |
| BinaryDivideTests | 18 |
| LambdaIdentityTests | 86 |
| NoParameterBlockTests | 108 |
| ParameterBlockTests | 132 |
| TernaryTests | 86 |
| NewWithParameterTests | 20 |
| UnaryPlusTests | 18 |

This is observed coverage of these tests, not a claim that every variation of the corresponding language operation is supported.

## What fails or cannot finish

- Strict rejection: examples include nullable custom increment/decrement, lifted custom multiplication/division, and nullable bitwise not.
- Invalid IL or IL generation errors: examples include some pre-increment/decrement assignment, reference comparisons through interfaces, return/break result handling, and type tests.
- Behavior/exception differences: examples include implicit numeric coalesce conversions, checked conversions to nullable unsigned types, nullable unsigned conversions, compound assignments to members, and invocation of private delegates.
- Runtime implementation assertions: expected IL, closure layout, and interpreter instruction tests retain the upstream assertions and need separate interpretation; different generated IL alone does not establish a behavioral defect.
- Process crashes: the exact method list is in [process-failures.csv](process-failures.csv), including constrained virtual calls, some value-type calls/casts/type tests, nullable arithmetic lambdas, and unboxing.
- Timeout: `ExpressionTests.CompileIrreduciebleExtension` did not finish within its 600-second group timeout. Its group completed after isolating it.
- Inconsistent results: six `Goto.GotoJumps` cases differed across attempts.

The deep-tree stress test passed both system modes and crashed FEC. A representative `CallTests.Call_NoParameters` crash also reproduced alone on .NET 8 ARM64 and .NET 10 x64; those were diagnostic checks, not additional full-platform runs.

## Complete listings

- [methods.csv](methods.csv): all 5,680 methods, with outcome counts, baseline counts, compilation scope, and representative errors.
- [failures.csv](failures.csv): every nonpassing/nonskipped result group, with names, counts, category, and short error.
- [classes.csv](classes.csv): outcome counts for every test class.
- [process-failures.csv](process-failures.csv): all crashing/timed-out methods and affected case counts.
- [summary.json](summary.json): machine-readable aggregate results.

These snapshots omit raw logs and generated HTML. Reproduction instructions and report generation are in the [project README](../README.md). The build and final compiler smoke test passed; this compatibility suite intentionally exposes failures and can fail or abort a direct solution test run. No compiler implementation changes are included. Published trimming, NativeAOT, Roslyn, and LightExpression are outside the measured comparison.

## Entirely passing classes (including API-only classes)

| Class | Passed |
| --- | ---: |
| `System.Dynamic.Tests.BindingRestrictionsProxyTests` | 5 |
| `System.Dynamic.Tests.CallInfoTests` | 19 |
| `System.Dynamic.Tests.ConvertBinderTests` | 35 |
| `System.Dynamic.Tests.DynamicObjectDefaultBehaviorTests` | 17 |
| `System.Dynamic.Tests.DynamicObjectTests` | 36 |
| `System.Dynamic.Tests.ExpandoObjectProxyTests` | 26 |
| `System.Dynamic.Tests.ExpandoObjectTests` | 8 |
| `System.Dynamic.Tests.GetIndexBinderTests` | 13 |
| `System.Dynamic.Tests.GetMemberBinderTests` | 19 |
| `System.Dynamic.Tests.InvokeBinderTests` | 11 |
| `System.Dynamic.Tests.InvokeMemberBindingTests` | 47 |
| `System.Dynamic.Tests.SetIndexBinderTests` | 13 |
| `System.Dynamic.Tests.SetMemberBinderTests` | 18 |
| `System.Dynamic.Tests.UnaryOperationTests` | 187 |
| `System.Linq.Expressions.Tests.ActionTypeTests` | 104 |
| `System.Linq.Expressions.Tests.ArrayArrayIndexTests` | 167 |
| `System.Linq.Expressions.Tests.ArrayArrayLengthTests` | 173 |
| `System.Linq.Expressions.Tests.ArrayBoundsOneOffTests` | 8 |
| `System.Linq.Expressions.Tests.ArrayLengthTests` | 193 |
| `System.Linq.Expressions.Tests.AsNullableTests` | 35 |
| `System.Linq.Expressions.Tests.BinaryAndTests` | 22 |
| `System.Linq.Expressions.Tests.BinaryDivideTests` | 27 |
| `System.Linq.Expressions.Tests.BinaryEqualTests` | 146 |
| `System.Linq.Expressions.Tests.BinaryExclusiveOrTests` | 22 |
| `System.Linq.Expressions.Tests.BinaryModuloTests` | 27 |
| `System.Linq.Expressions.Tests.BinaryNullableAndTests` | 16 |
| `System.Linq.Expressions.Tests.BinaryNullableExclusiveOrTests` | 16 |
| `System.Linq.Expressions.Tests.BinaryNullableOrTests` | 16 |
| `System.Linq.Expressions.Tests.BinaryOrTests` | 22 |
| `System.Linq.Expressions.Tests.BlockFactoryTests` | 4 |
| `System.Linq.Expressions.Tests.CallFactoryTests` | 29 |
| `System.Linq.Expressions.Tests.ConstantArrayTests` | 86 |
| `System.Linq.Expressions.Tests.ConstantNullableTests` | 46 |
| `System.Linq.Expressions.Tests.DebugInfoExpressionTests` | 9 |
| `System.Linq.Expressions.Tests.DebugViewTests` | 41 |
| `System.Linq.Expressions.Tests.DefaultTests` | 22 |
| `System.Linq.Expressions.Tests.DynamicExpressionTests` | 126 |
| `System.Linq.Expressions.Tests.ElementInitTests` | 19 |
| `System.Linq.Expressions.Tests.ExpressionDebuggerTypeProxyTests` | 151 |
| `System.Linq.Expressions.Tests.Expression_Tests` | 22 |
| `System.Linq.Expressions.Tests.FuncTypeTests` | 104 |
| `System.Linq.Expressions.Tests.GetDelegateTypeTests` | 105 |
| `System.Linq.Expressions.Tests.IndexExpressionVisitorTests` | 2 |
| `System.Linq.Expressions.Tests.InvokeFactoryTests` | 6 |
| `System.Linq.Expressions.Tests.LabelTargetTests` | 15 |
| `System.Linq.Expressions.Tests.LabelTests` | 12 |
| `System.Linq.Expressions.Tests.LambdaAddTests` | 20 |
| `System.Linq.Expressions.Tests.LambdaIdentityNullableTests` | 46 |
| `System.Linq.Expressions.Tests.LambdaIdentityTests` | 86 |
| `System.Linq.Expressions.Tests.LambdaModuloTests` | 18 |
| `System.Linq.Expressions.Tests.LambdaMultiplyTests` | 18 |
| `System.Linq.Expressions.Tests.LambdaSubtractTests` | 18 |
| `System.Linq.Expressions.Tests.LambdaUnaryNotTests` | 16 |
| `System.Linq.Expressions.Tests.MakeGoto` | 16 |
| `System.Linq.Expressions.Tests.MakeUnaryTests` | 83 |
| `System.Linq.Expressions.Tests.NewWithParameterTests` | 20 |
| `System.Linq.Expressions.Tests.NewWithTwoParametersTests` | 9 |
| `System.Linq.Expressions.Tests.NoParameterBlockTests` | 545 |
| `System.Linq.Expressions.Tests.NonLiftedComparisonEqualNullableTests` | 26 |
| `System.Linq.Expressions.Tests.NonLiftedComparisonNotEqualNullableTests` | 26 |
| `System.Linq.Expressions.Tests.NullableArrayIndexTests` | 92 |
| `System.Linq.Expressions.Tests.NullableArrayLengthTests` | 92 |
| `System.Linq.Expressions.Tests.NullableNewArrayListTests` | 46 |
| `System.Linq.Expressions.Tests.ParameterBlockTests` | 561 |
| `System.Linq.Expressions.Tests.ReadOnlyCollectionBuilderTests` | 142 |
| `System.Linq.Expressions.Tests.SymbolDocumentInfoTests` | 5 |
| `System.Linq.Expressions.Tests.TernaryArrayNullableTests` | 42 |
| `System.Linq.Expressions.Tests.TernaryArrayTests` | 86 |
| `System.Linq.Expressions.Tests.TernaryNullableTests` | 46 |
| `System.Linq.Expressions.Tests.TernaryTests` | 86 |
| `System.Linq.Expressions.Tests.TrimCompatibilityTests` | 1 |
| `System.Linq.Expressions.Tests.Trimming.BinaryOperatorTests.EntryPointTests` | 1 |
| `System.Linq.Expressions.Tests.Trimming.ConvertOperatorTests.EntryPointTests` | 1 |
| `System.Linq.Expressions.Tests.Trimming.EqualOperatorTests.EntryPointTests` | 1 |
| `System.Linq.Expressions.Tests.Trimming.ExpressionPropertyTests.EntryPointTests` | 1 |
| `System.Linq.Expressions.Tests.Trimming.GetHelperTypeTests.EntryPointTests` | 1 |
| `System.Linq.Expressions.Tests.UnaryArithmeticNegateTests` | 16 |
| `System.Linq.Expressions.Tests.UnaryBitwiseNotTests` | 19 |
| `System.Linq.Expressions.Tests.UnaryIsTrueNullableTests` | 2 |
| `System.Linq.Expressions.Tests.UnaryPlusNullableTests` | 18 |
| `System.Linq.Expressions.Tests.UnaryPlusTests` | 19 |
| `System.Linq.Expressions.Tests.UnaryUnboxTests` | 3 |
| `System.Linq.Expressions.Tests.VariableTests` | 171 |
| `System.Linq.Expressions.Tests.VisitorTests` | 24 |
| `System.Runtime.CompilerServices.Tests.CallSiteBinderDefaultBehaviourTests` | 3 |
| `System.Runtime.CompilerServices.Tests.CallSiteCachingTests` | 6 |
| `System.Runtime.CompilerServices.Tests.CallSiteTests` | 4 |
| `System.Runtime.CompilerServices.Tests.DynamicAttributeTests` | 9 |
