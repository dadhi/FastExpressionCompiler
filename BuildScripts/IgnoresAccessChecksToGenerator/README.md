# Local metadata publicizer experiment for #560

**Draft follow-up experiment to #567, for #560.** Branch `experiment/metadata-private-access-560`, based on FEC master `734314ede7c5e58845d42345e1cfb4d59591b998`. It is not wired into FEC's production build or solution.

This is a local source/backend fork experiment based on [IgnoresAccessChecksToGenerator](https://github.com/aelij/IgnoresAccessChecksToGenerator), upstream revision `882b30bf82fbb72f3bee8878ad9fd31f5c93c875`. The original task/targets snapshots are in `upstream/`, under the retained MIT `LICENSE`. The experimental backend replaces Cecil with `System.Reflection.Metadata` / `System.Reflection.PortableExecutable`, plus an MSBuild task. The compiled generator has no Cecil or NuGet package dependency. This is **not yet a drop-in replacement for the whole upstream NuGet package**.

## What works

The backend reads the installed CoreLib with `PEReader`, copies compiler-relevant metadata into `MetadataBuilder`, and writes a **new IL-only reference assembly** with `ManagedPEBuilder` and `ReferenceAssemblyAttribute`. It does not rewrite the runtime executable image. Assembly identity, type/field/method tokens, signature blobs, generic definitions, nested relationships and relevant member metadata are retained. Method bodies, native sections, RVA field data and embedded resources are not copied.

Default exposure matches the upstream internal-member policy. `--private` additionally exposes private fields, methods and nested types (and the remaining accessibilities) in the generated compiler reference. The actual runtime members remain private; the consumer's `IgnoresAccessChecksTo` attribute authorizes access at runtime.

Both .NET 8.0.28 and .NET 10.0.9 on Windows ARM64 pass:

- C# compilation and execution using `RuntimeILGenerator`, genuine private fields `m_ILStream`/`m_length`, private method `IncreaseCapacity`, and private nested enum `GC.StartNoGCRegionStatus`. Runtime reflection confirms these are genuinely private in the installed assembly. The directly emitted method returns 42.
- MSBuild integration: the new `PublicizeInternals` task generates the reference and access attribute, then supplies compiler references. The same consumer compiles and executes.
- Compiling the real Standard and LightExpression FEC source in an isolated host against the generated references. Only an ignored source snapshot changes the local-allocation helper to use direct typed field access on .NET 8/10; production source files are untouched. Each configuration passes 100 repeated local/try-finally compilations, independent nested captures, and the runtime-contract tests from PR #567 (1 per API on .NET 8, 3 per API on .NET 10).
- Disabling private exposure fails compilation; removing the access attribute fails with `TypeAccessException`; a nonexistent field fails compilation (`CS1061`); using the wrong field type fails compilation (`CS0029`). These are expected failures checked by the replay script.
- All 25 replay steps pass. The installed CoreLib's SHA-256 is unchanged before/after. The generated CoreLib is not copied into consumer output directories. See `results.json` for commands, exit codes and input hashes; logs are in ignored `artifacts/logs/`.

An exploratory `Array.ArrayAssignType` probe compiled on .NET 10 but failed on .NET 8 because that nested type is absent there. The retained cross-version probe uses `GC.StartNoGCRegionStatus`, verified private on both. This illustrates why checks must run against each intended runtime, rather than assuming their internals match.

## Important build constraint

Adding a full CoreLib facade alongside SDK reference assemblies causes duplicate core-type definitions: SDK `System.Runtime` defines types that the implementation puts in CoreLib. This prototype therefore uses one consistent set of **managed implementation references from the explicitly selected runtime**, replacing CoreLib with the generated reference. Native DLLs are excluded using `PEReader.HasMetadata`.

This is deliberately a runtime-version/OS-specific build experiment, not a portable targeting pack. Compiler checks validate the selected implementation version, not every runtime a published FEC package could encounter. Windows implementation references also carry Windows-specific platform annotations; the sample consumers explicitly declare their Windows platform when built on Windows. No reference assembly is substituted at execution time.

## Replay

Requires the .NET 10 SDK, Python, and the installed .NET 8.0.28 / 10.0.9 runtimes. From this directory:

```sh
python replay.py
```

The script discovers the two exact installed runtime paths, builds the generator, verifies metadata and positive/negative behavior, runs the FEC smoke configurations, and records `results.json`. Consumer runtime versions are pinned with roll-forward disabled, and the executing CoreLib path must match the selected input. No git commit/push, network publication, runtime replacement or machine configuration change is performed. Build outputs/logs are ignored. The FEC smoke fixture is copied from PR #567 commit `ff41665593d64d18196aed3f202ef1a1944a4e4f` and compiled locally, so replay does not need that commit available as an object.

CLI use:

```sh
dotnet run --project Generator/Generator.csproj -c Release -- path/to/System.Private.CoreLib.dll artifacts/custom/System.Private.CoreLib.dll --private
dotnet run --project Generator/Generator.csproj -c Release -- --verify path/to/System.Private.CoreLib.dll artifacts/custom/System.Private.CoreLib.dll --private
```

## What is not established

- No compilation/startup/allocation/runtime speedup has been measured. Typed access is feasible; that alone does not prove it beats UnsafeAccessor or generated helpers.
- The task currently requires .NET 10 MSBuild and supports CoreLib only. Arbitrary packages, upstream exclusion/legacy options, older MSBuild hosts, packaging and incremental-generation caching are not implemented.
- No cross-platform, x64, Framework 4.7.2, preview-runtime, or complete compatibility-suite claim is made. These are focused compile/runtime and metadata checks, not a full FEC regression run.
- The verifier checks assembly identity, the reference/IL-only marker, definition tokens/ranges/signatures, visibility and absence of executable method bodies. This is not a general metadata-rewriter certification.

## Merge recommendation

**Keep this as a draft; do not merge the current prototype as a production solution for #560.** It establishes feasibility but adds a runtime-specific compiler-reference strategy without an established performance benefit. It also lacks cross-platform/x64 coverage and the validation needed for a general metadata writer.

Next useful experiments are a narrower exposure policy/reference strategy and measured direct typed reuse/local-allocation helpers versus FEC's existing helpers, including build-time cost. Production integration should wait for those results and a decision on runtime-version support. The smaller runtime-contract tests in #567 can be reviewed independently; this draft does not replace or modify that PR.
