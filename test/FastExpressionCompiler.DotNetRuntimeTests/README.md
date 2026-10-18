# dotnet/runtime expression tests

Current compiler fixes, verified backend-specific exclusions, and measured results are documented in the [fixes report](results/fixes/README.md). Four irreducible-node cases assert FEC's strict rejection contract directly; narrowly classified compiled/interpreter differences have dedicated behavior proofs. The original import snapshot remains available below.

This compatibility project ports every C# file beneath `src/libraries/System.Linq.Expressions/tests` at dotnet/runtime revision `ec0059c08c077e839c6eb8827c846530a891d7e2`. `UPSTREAM-LICENSE.TXT` preserves the upstream MIT license; `upstream-manifest.json` records every original file and adaptation.

Every upstream expression `.Compile(...)` call becomes `.CompileForFec(...)`. The default adapter calls `CompileFast(ifFastFailedReturnNull: true)` and throws if FEC returns null. System compilation cannot silently satisfy a FEC test. Both upstream interpreter flags are retained and routed to FEC, preserving branches and data; they are repeated FEC executions, not FEC interpreter support. Set `FEC_TEST_COMPILER=system` to execute the original compiler/interpreter modes as a control.

Build and run in PowerShell:

```powershell
dotnet build test/FastExpressionCompiler.DotNetRuntimeTests -c Release
python test/FastExpressionCompiler.DotNetRuntimeTests/Run-Sharded.py --mode system
python test/FastExpressionCompiler.DotNetRuntimeTests/Run-Sharded.py --mode fec
python test/FastExpressionCompiler.DotNetRuntimeTests/tools/Build-Report.py
```

`Run-Sharded.py` runs all classes in groups, isolates process-crashing methods, and continues the remaining tests. It records crashes instead of marking them as passes. It runs the 10,000-deep tree stress method separately with a time limit. Results go under `TestResults/`; the report includes every baseline case, including cases that could not finish after a crash. Direct `dotnet test` also works, but a compiler-generated fatal CLR error can abort that process.

The five upstream trimming applications are namespaced and wrapped as xUnit tests that retain their success return-code checks. These runs test their execution without publishing/trimming; trimming and NativeAOT deployment compatibility are not claimed. The original RemoteExecutor test is explicitly skipped because that infrastructure is absent. The three unconditional upstream ActiveIssue tests remain skipped. Windows/CoreCLR platform conditions are represented in `RuntimeTestCompatibility.cs`; that shim is intended for the documented local environment, not a portable replacement for runtime's test infrastructure.

Internal ArgumentProvider/Closure/CallSiteOps/LightLambda access is performed through reflection, with target invocation exceptions unwrapped. Exact IL, closure layout, and interpreter instruction assertions remain present; the report identifies these implementation-specific checks separately from behavioral failures. API-only tests are also identified separately using compilation observations.

The report covers the standard `System.Linq.Expressions` input API of FastExpressionCompiler. LightExpression construction/conversion, NativeAOT smoke infrastructure, linker metadata preservation, C#/VB compiler lowering, and debugger expression compilation in Roslyn are different test targets and are not claimed as verified here. This is a complete import of the named runtime suite, not a claim that every expression-related test in every repository in the dotnet organization was ported.

Reimport from a local checkout of the pinned upstream revision:

```powershell
python test/FastExpressionCompiler.DotNetRuntimeTests/tools/Import-Upstream.py C:/path/to/dotnet-runtime
```

Imported assertions are retained. Failing compatibility tests intentionally remain failures; compiler implementation files are unchanged.

## Measured local results (18 October 2026)

The .NET 10 ARM64 Release comparison accounts for 35,133 test cases: 23,322 passed, 2,571 failed, 9,228 did not finish after a method crashed, 2 did not finish after a timeout, 6 had inconsistent outcomes across attempts, and 4 were skipped. There were 91 crashing methods and one timed-out method. These totals include API-only tests; the report records compilation-requesting methods separately.

The corrected system control had 35,116 passes, 13 failures, and 4 skips. Some theory data rows have identical IDs and display names, so the report groups them with explicit outcome counts instead of asserting a one-to-one mapping. Random GUID/date values in names are normalized for run matching only. Raw TRX evidence retains the original results.

Open `Reports/report.html` for the searchable report, `Reports/all-tests.csv` for all result groups, and `Reports/failures.csv` for error details. The build and final compiler smoke test passed. Compiler implementation files were not changed.

The committed [results snapshot](results/README.md) lists all methods/classes and the observed failure areas for review without generated local artifacts.
