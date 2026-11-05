# Registering regression fixtures

For a new parameterless `ITestX` fixture in `FastExpressionCompiler.UnitTests`,
add `[RunTestX]` beside its declaration:

```csharp
[RunTestX]
public struct MyRegressionTests : ITestX
{
    public void Run(TestRun tests) { /* call the regression methods */ }
}
```

No changes to either runner's `Program.cs` are needed. The shared test source is
compiled for Standard and LightExpression, and both the modern and Framework
runners discover marked fixtures in their respective unit-test assemblies.
Use the fixture's existing `#if` guards for API- or framework-specific code.
Each flavor runs marked fixtures once, in ordinal full-type-name order.

Discovery is opt-in. Unmarked tests, legacy `Run()` fixtures, issue-test lists,
and the early runtime-accessor contracts retain their existing entry points.
Do not also register a marked fixture manually. A marked fixture must implement
`ITestX` and have a public parameterless constructor (implicit for structs).

Keeping registration in the fixture file lets separate regression PRs add their
own files without editing shared registration lists. Existing PRs that still add
manual registrations need a one-time migration after this change is merged.
