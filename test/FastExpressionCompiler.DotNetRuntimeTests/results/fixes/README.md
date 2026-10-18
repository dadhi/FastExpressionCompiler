# Focused compatibility fixes

These results describe the local follow-up to draft PR [#557](https://github.com/dadhi/FastExpressionCompiler/pull/557), based on `e4c232da`. The original 35,133-case snapshot in the parent directory remains historical; a fresh full imported suite run is recorded below.

All changes stay in the existing emitter, reduction loops, and LightExpression factory layout. No system-compiler fallback, preprocessing pass, or test-specific compiler behavior was added. Nullable Boolean support follows issue [#496](https://github.com/dadhi/FastExpressionCompiler/issues/496); new regressions use the existing `ITestX` infrastructure.

## Results

| Imported slice | Cases | Before | After |
| --- | ---: | --- | --- |
| Numeric conversions and coalesce | 3,407 | 3,054 pass, 353 fail | 3,368 pass, 39 fail |
| Nullable Boolean operators | 10 | 10 fail | 10 pass |
| Adjacent binary logical tests | 119 | 79 pass, 40 fail | 79 pass, 40 fail |
| Extension assertions | 4 | Two irreducible-extension cases timed out; two other assertions failed | All finish; four assertions still fail |
| Focused value/nullable `TypeIs` tests | 28 | 8 fail, 20 unfinished due to crashes | 28 pass |

Across these 3,568 cases, the follow-up yields **3,485 passes and 83 failures**. There are **352 additional passing cases** compared with the parent snapshot, including 20 previously affected by crashes. These totals cover only the listed slices; broader `TypeIs` classes still crash.

## What changed

1. Numeric conversions use source signedness for checked overflow opcodes, unsigned widening, and floating-point conversion. Checked conversion is preserved when constructing nullable targets, including enum conversions.
2. Nullable `Not`, `And`, `Or`, `AndAlso`, and `OrElse` emit three-valued Boolean logic. Short-circuit operators evaluate each operand at most once. LightExpression factories preserve the nullable result type. The original issue #480 reproductions compile strictly.
3. Extension reduction rejects null or unchanged results through the existing unsupported-expression path, preventing irreducible nodes from looping forever.
4. `TypeIs` boxes value operands before `isinst`, handles void operands/targets and pointer targets, and preserves operand side effects.

## Validation

Environment: Windows ARM64, .NET SDK 10.0.301, .NET runtime 10.0.9; Release builds.

- Standard and LightExpression libraries build for every configured target: net472, netstandard2.1, net8.0, net10.0; zero warnings/errors.
- All 37 new regression methods pass on .NET 8 and .NET 10, covering both Standard and LightExpression where supported. Matrix assertions exercise many combinations within each method.
- Native .NET 10 runner passes all 1,712 legacy tests and its registered `ITestX` groups.
- Native .NET Framework runner passes 1,595 legacy tests and 138 `ITestX` methods with English UI culture. With the machine's German UI culture, two pre-existing issue #261 assertions compare localized exception text against English strings. Culture was set only by an external validation wrapper.
- The full native .NET 8 runner still crashes in LightExpression issue #346 (`Check_assignment_to_by_ref_float_parameter_PostIncrement_Returning`). The same crash was reproduced from the unchanged parent commit `e4c232da`; focused new regression methods pass on .NET 8.

## What remains

- The 39 numeric/coalesce failures include boxing/unboxing and coalesce conversion behavior outside the primitive conversion fix.
- Forty neighboring binary logical failures involve custom operator behavior; the nullable Boolean changes do not address those.
- Irreducible-extension upstream tests expect the system compiler's `ArgumentException`. Strict FEC instead rejects unsupported nodes, so the compatibility adapter throws `NotSupportedException`. They now finish but remain failures. Two other extension assertions also remain failures.
- Broader imported `IsTests`, `IsNullableTests`, and `TypeIs` runs still fail or crash. A separate boxed-constant conversion defect was observed: converting an `IComparable`-typed boxed integer constant to object can return an invalid object before the type test runs. The focused 28-case result does not imply these entire classes pass.
- `TypeEqual`, debug-info compilation, and other unsupported features in the parent report are unchanged. The full-suite outcomes below retain all remaining failures and crashes.

## Reproduction

Build and run `test/FastExpressionCompiler.TestsRunner` in Release for net10.0. Build and run `test/FastExpressionCompiler.TestsRunner.Net472` for .NET Framework. The regression groups are registered in both native runners.

Run the imported project with strict FEC and the filters below; leave `FEC_TEST_COMPILER` unset or set it to `fec`:

```powershell
dotnet test test/FastExpressionCompiler.DotNetRuntimeTests -c Release --filter 'FullyQualifiedName~System.Linq.Expressions.Tests.IsNullableTests.CheckNullable|FullyQualifiedName~System.Linq.Expressions.Tests.IsTests.CheckStructIs|FullyQualifiedName~System.Linq.Expressions.Tests.IsTests.CheckEnumIs|FullyQualifiedName~System.Linq.Expressions.Tests.IsTests.CheckIntIs'
```

The imported tests retain both upstream compilation-mode rows, but both exercise strict FEC; neither invokes the interpreter or falls back to the system compiler.

## Fresh full-suite run

Of 35,133 imported cases: **23,719 pass**, **1,755 fail**, **9,655 remain unfinished after 71 crashing methods**, **0 are inconsistent across attempts**, and **4 are skipped**. There are 0 timed-out methods and 0 otherwise unaccounted-for cases. See `summary.json`. The verified system control from the parent report was reused on the same machine/runtime.

The process marker identifies the crashing method, not the specific data row; unfinished rows must not be described as independently reproduced crashes. All 21 batches finished after crash isolation, with the deep-tree stress test run separately.

Detailed current results are in `failures.csv`, `methods.csv`, and `crashing-methods.csv`. The parent report combines multiple diagnostic and verification attempts; this follow-up is a fresh crash-recovery run. The number of rows completed before a process crash can vary, so differences in unfinished-case counts alone do not establish a regression or a fix.
