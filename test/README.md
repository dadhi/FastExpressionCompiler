# Native test registration

Both native runners use compile-time generated registration for Standard and LightExpression fixtures, including `ITestX`, legacy `Run()` fixtures, and the modern runtime-accessor contract gate. Add a fixture and mark its declaration:

```csharp
[RunTest]
public struct MyRegressionTests : ITestX
{
    public void Run(TestRun tests) { /* test cases */ }
}
```

There is no runner list to edit. Legacy fixtures expose a public parameterless `int Run()` method; `ITestX` fixtures use their existing interface. Public, concrete, top-level, non-generic fixtures need a public parameterless constructor (or may be structs). A fixture exposing both entry points must explicitly select `[RunTest(TestRegistrationGroup.Legacy)]` when using the legacy entry point. Runtime-accessor contracts use `TestRegistrationGroup.RuntimeContracts` and remain available through `--runtime-accessor-contracts`.

Place any platform or flavor guard around the attribute, next to its fixture. The generator reads the attributes from the four compiled fixture assemblies, so it observes each assembly's actual conditional compilation. Shared test files may opt into both APIs automatically, while Light-only files opt into Light alone. Unmarked helpers and experimental fixtures are not registered. Existing registration restrictions are retained in the migrated fixture declarations.

The test-only incremental generator validates marked types while compiling fixture assemblies, before reference assemblies can hide an invalid internal or private fixture. In each runner compilation it reads fixture metadata and emits ordinary typed constructor and `Run` calls. It uses Roslyn symbols, with no runtime assembly scanning or `Activator` construction. Legacy callbacks retain per-fixture failure handling and unit/issue parallelism. Registration order is deterministic by fully qualified type name. The shared legacy case counter uses `Interlocked.Add` because both task groups update it.

`test/GeneratedTests.cs` declares required partial methods. Removing the analyzer and recompiling fails with CS8795, rather than silently producing an empty run. Invalid marked fixtures fail with FECTEST001. The generator is a build-only analyzer dependency of the test projects; production compiler projects and packages do not reference it.

Run the generator's execution, conditional-compilation, incremental-update and diagnostic tests:

```powershell
dotnet test test/FastExpressionCompiler.TestRegistrationGenerator.Tests -c Release
```

To inspect emitted code, build a runner with `-p:EmitCompilerGeneratedFiles=true` and look below its `obj/<configuration>/<framework>/generated` directory. `GeneratedTests.GetFixtures()` provides a generated manifest for registration audits. The migration was audited against the original runner lists for Standard and LightExpression on net472, net8.0 and net10.0: all existing fixture memberships and categories are retained.

The imported dotnet/runtime xUnit suite continues to use xUnit discovery.
