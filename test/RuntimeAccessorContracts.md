# Runtime accessor contracts (#560)

`RuntimeAccessorContractTests` runs for Standard and LightExpression before the existing .NET 8/10 suites. The existing Windows/Linux CI matrix therefore exercises it without an additional workflow. Contract failures produce a nonzero exit, including assertions that the TestX runner normally records rather than throws.

To run only these checks:

```sh
dotnet run -c Release -f net10.0 --project test/FastExpressionCompiler.TestsRunner -- --runtime-accessor-contracts
dotnet run -c Release -f net8.0 --project test/FastExpressionCompiler.TestsRunner -- --runtime-accessor-contracts
```

These intentionally strict tests target the CoreCLR implementations used by CI:

- .NET 8/10: local-allocation and IL-generator-reuse helpers must remain available. Reuse must actually return the same generator, reset locals/signatures/tokens/exception-region state for a different method, and preserve the previously created delegate.
- .NET 10: inspect all six production `UnsafeAccessor` declarations. Resolve the owner from `UnsafeAccessorType`, require the member on that exact declaring type, and check field types/byref returns or method parameter/return types and instance/static shape.
- .NET 10: bind the production accessors themselves, then use all six to create locals, write IL, update stack accounting, and install a method token. The resulting delegate must execute correctly. This catches binding/JIT failures as well as metadata changes.

The tests run against the executing runtime; building alone does not validate its private members. They do not promise compatibility with runtimes that CI has not exercised. Framework 4.7.2 has a different implementation and does not contain these .NET 10 accessors, so this fixture is excluded there. An intentional change to FEC's accessors or supported runtime contract should update the fixture too.

## Why keep UnsafeAccessor?

Issue [#560](https://github.com/dadhi/FastExpressionCompiler/issues/560) proposes compiler checks using [IgnoresAccessChecksToGenerator](https://github.com/aelij/IgnoresAccessChecksToGenerator). Its current publicizer exposes internal members but leaves private members private. An extension could expose private members and nested types in generated build-time references; that is possible in principle.

The isolated stock-package 0.8.0 probe on Windows ARM64/.NET 10.0.9 found two practical obstacles: normal framework reference assemblies omit `RuntimeILGenerator`, and feeding the installed `System.Private.CoreLib` implementation to the generator fails with `Writing mixed-mode assemblies is not supported`. FEC accesses both internal members (for example `m_localCount`) and private fields (`m_length`, `m_ILStream`). Supporting those accesses requires more than adding the package: usable implementation-derived metadata references and private-member publicizing are needed. The installed runtime assembly should not be replaced.

Direct typed helpers could potentially reduce startup reflection/DynamicMethod generation and delegate dispatch in FEC's reuse path. Replacing an existing UnsafeAccessor field access alone is not an established speedup. Specialized/batched emission is a separate optimization. None of those performance ideas are implemented or measured by this test-only change.
