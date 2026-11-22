# FlatExpression into FEC TryEmit

Follow the steps in order. Finish one step, run the test command, stop. Do not start the next step in the same change.

Worktree: `e:\Dev\FEC-flat-barebones` (branch `flat-barebones`). Do not edit `e:\Dev\FastExpressionCompiler`.

Test command (from the worktree):

```
dotnet run -c Release -f net10.0 --project test/FastExpressionCompiler.TestsRunner
```

Do not commit or push unless the user asks. Do not force-push.

## Goal

`ExprTree.CompileFast` emits IL by walking node indexes. It does not build a Light or System expression tree. Closures, nested lambdas, and labels use the same `CompilerContext` facts the Light compiler already uses, stored as `ushort` ids instead of `Expression` objects.

## What is already true

- Flat exists only when `FLAT_EXPRESSION` is set. The Flat project defines it. Light defines `LIGHT_EXPRESSION` only. The two are never both on. Use `#if FLAT_EXPRESSION`. Do not define Flat from Light.
- `ExprNode` is 24 bytes. `ChildIdx` / `NextIdx` is the walk. Parameter identity is the declaration node's `ChildIdx` (a `ushort`), not object identity.
- `ExprTree.Constant` already classifies values. Inline primitives and `ldstr` / `ldtoken` / `decimal` stay on the node. Closure values are unique in `ExprTree.ClosureConstants` and the node `Obj` is `ExprTree.ClosureConstantMarker`.
- `ExpressionCompiler.TryCollectInfo(ref CompilerContext, ref ExprTree)` walks the flat tree. It copies closure constants into the current lambda, records nested `NestedLambdaInfo(null)`, and rejects unbound parameters. It does not compile nested lambdas, does not fill label/scope maps, and does not record which nodes have complex arguments.
- `CompileFast(ref ExprTree)` walks node indexes in the Flat assembly. `ToExpression` / `FromSysExpr` are the System round-trip. Flat has no Light conversion.
- `EmittingVisitor` is `public static class` in `ExpressionCompiler` (search `class EmittingVisitor`). `TryEmit(Expression, ...)` is public. Do not change that signature.

## Do not do

- Do not resurrect `FlatExpressionCompiler.cs` or a second compiler.
- Do not grow `ExprNode` past 24 bytes. Two extra bytes pad the array stride to 32 and slow every walk. A fact that is known at construction and is not compiler-flag-dependent may use the spare flag bits. Arg and local identity stays a stack `SmallList` of ids (`CompilerContext.FlatArgIds`), not a per-node field. Do not stamp `HasComplexExpression` on the node: it depends on `CompilerFlags`.
- Do not stamp collect results onto node flags. `HasComplexExpression` depends on `CompilerFlags` (invoke inlining), so it is not a property of the tree.
- Do not key Flat maps by `Expression`, `ParameterExpression`, `LabelTarget`, or `InvocationExpression`. That forces allocating those objects.
- Do not replace the Light maps (`_varInBlock`, `InlinedLambdaInvocation`, `ArgsContainingComplexExpression`). Light and System compile keep them. Flat gets parallel `ushort` storage under `#if FLAT_EXPRESSION`.
- Do not change `NestedLambdaInfo` from a class to a struct. Light dedupes it by reference.
- Do not change user-facing `CompileFast` overloads on `LambdaExpression` / `Expression<TDelegate>`.
- Do not change `ExprTree` factory signatures (`Constant`, `Lambda`, `Add`, `Block`, ...).
- A `CompileFast` that only calls `ToLightExpression()` is not a step. The fallback below is temporary and each step must shrink it.

## User-facing API (keep small)

Add one entry, when step 1 lands:

```csharp
public static TDelegate CompileFast<TDelegate>(ref this ExprTree tree,
    bool ifFastFailedReturnNull = false,
    CompilerFlags flags = CompilerFlags.Default) where TDelegate : class
```

Place it next to the other `CompileFast` methods in `ExpressionCompiler`, inside `#if FLAT_EXPRESSION`. `ExprTree` is a large struct: the parameter is `ref this`, not a by-value `this`.

`flags` is the existing `CompilerFlags` (Light namespace when `LIGHT_EXPRESSION` is on).

Leave `ToExpression` and `FromSysExpr`. They are the System interop path. `ToLightExpression` and `FromLightExpr` are gone: Flat does not reference Light.

## Emitter shape

Keep `TryEmit(Expression, ...)`.

Add, inside `EmittingVisitor`, `#if FLAT_EXPRESSION`:

```csharp
public static bool TryEmitFlat(ref ExprTree tree, ushort idx, ILGenerator il,
    ref CompilerContext context, ParentFlags parent, int byRefIndex = -1)
```

Rules for this method:

- Walk with the same sibling rule as collect: `FlatNext` (search that function). Do not allocate a child array.
- Reuse an IL helper when it already takes `Type` / `MethodInfo` / `ILGenerator` and not an `Expression`. Examples: `TryEmitArrayElementGet`, `EmitLoadIndirectlyByRef`, `TryEmitBoxOf`.
- Do not extract a helper that is used once. Do not wrap `TryEmitFlat` in an `Expression` subclass.
- On a node kind this step does not handle, return `false`. The compile entry then uses the fallback. Do not throw.
- `ParentFlags` stays the shared contract. Do not add a second flag enum.

## Fallback (bridge, then delete)

`CompileFast(ref ExprTree)`:

1. Collect with `TryCollectInfo(ref context, ref tree)`.
2. If collect is not `Result.OK`, return null or throw the same way `TryCompileBoundToFirstClosureParam` does for Light (`ThrowOnNotSupportedExpression`).
3. Emit with `TryEmitFlat`. If it returns true, finish the delegate the same way Light does (`DynamicMethod`, pooled `ILGenerator`, `ArrayClosure` from `GetArrayOfConstantsAndNestedLambdas`).
4. An unsupported node returns null or throws. It does not build a Light or System tree.

## Optimizations (do them inside the step that needs them)

Ranked by allocations removed. Do not do them as a separate refactor.

| Rank | What | Where it happens | Do not |
|---|---|---|---|
| 1 | No Light/System tree on compile | steps 1–7 | leave the fallback after step 7 |
| 2 | Parameters and locals are `ushort` ids | steps 1 and 4 | build `ParameterExpression` to reuse `_varInBlock` |
| 3 | Closure constant is an index into `ClosureConstants` | already in collect; emit loads that object | call `IsClosureBoundConstant` again |
| 4 | Complex-arg owners are a `SmallSet4<int, IntEq>` of node indexes | step 3, filled by collect | `SmallSet4<Expression, ...>` |
| 5 | Labels are the label-target node `ChildIdx` (`ushort`) | step 5 | `LabelTarget` objects, `AddLabel(LabelTarget)` |
| 6 | Inlined invoke stores the lambda node index, not a rewritten `Expression` | step 6 | `CreateInlinedLambdaInvocationExpression` on the flat path |
| 7 | Nested lambda compiles by calling `TryEmitFlat` on its body | step 6 | `ToLightExpression` on the nested body |

`IntEq` already exists in `ImTools.cs`. Use it for node indexes. Do not add a new equality type unless a `ushort` key shows up in a hot map and `int` does not fit.

Collect still walks the tree, then emit walks it again. Do not merge the two passes. Emit needs the closure array before IL starts.

## Tests

File: `test/FastExpressionCompiler.LightExpression.UnitTests/LightExpressionPropertyTests.cs`.

Already there: `Can_property_test_flat_try_collect_matches_light` and the optional-child check. Leave them. They compare collect, not emit.

Each emit step adds one fact to `Can_property_test_flat_try_emit_matches_light` (create it in step 1):

- Build the flat tree with `ExprTree` factories (the generators already in that file).
- `CompileFast<Func<int,int>>(ref fe)` result equals `CreateGeneratedLightExpression(spec).CompileFast<Func<int,int>>()` for a few inputs (`-2, 0, 1, 5`).
- Widen the generator only to node kinds the current step emits. Do not turn on Try/Switch/Loop in step 1.

CsCheck is already referenced by `FastExpressionCompiler.LightExpression.UnitTests.csproj` for `net8.0` and `net10.0`. Do not add a package.

Net472 has no Flat. Do not compile these tests for net472.

## Steps

### Step 0 — baseline (done in the working tree)

Flat `TryCollectInfo`, `FLAT_EXPRESSION`, constant classification, collect property test. Do not rewrite this.

Done when the test command passes before any emit edit.

### Step 1 — compile entry and leaves

Emit only: `Constant`, `Parameter` (lambda parameters only), `Default`, unary `Convert` / `Negate` / `Not` with no method, binary `Add` / `Subtract` / `Multiply` / `Equal` / `GreaterThan` / `AndAlso` / `OrElse` with no method and no lift-to-null.

Touch:

- `ExpressionCompiler.CompileFast(ref this ExprTree, ...)`.
- `EmittingVisitor.TryEmitFlat` for those node kinds.
- The new property test, wired from `LightExpressionTests.Run` under `#if NET8_0_OR_GREATER`. Bump the `return` count in `Run` by 1.

Parameter emit: scan the root lambda's parameter children (the same loop as `FlatIsDeclared`) and `Ldarg` by that slot plus one (slot 0 is the closure). Reuse the Light `Ldarg` sequence; do not allocate `ParamExprs`.

Constant emit: inline value from `ExprNode.InlineValue`; `Obj` value via `Ldstr` / `ldtoken` / `Ldc` as Light `TryEmitConstant` already does; marker value via the closure slot `ConstantUsageThenVarIndex` already filled by collect.

Any other node kind: `TryEmitFlat` returns false and the fallback runs.

Done when: generated int trees (no blocks, no nested) match Light compile, and the test command passes.

### Step 2 — calls, members, constructors, arrays

Add node kinds: `Call`, `MemberAccess`, `New`, `NewArrayInit`, `NewArrayBounds`, `ArrayIndex`.

Still no blocks, try, or nested lambda. Instance and arguments are leaves from step 1, or these kinds.

Reuse Light's IL for `call` / `callvirt`, field/property get, `newobj`, `newarr`. Pass `MethodInfo` / `FieldInfo` / `ConstructorInfo` from `ExprNode.Obj` and child indexes from the walk.

Multi-dimensional `NewArrayInit` stays `Result.NotSupported_NewArrayInit_MultidimensionalArray` (collect already returns that).

Done when: a hand-built flat tree of `Call` + `Constant` + parameter matches Light `CompileFast` on the same shape, and the test command passes.

### Step 3 — complex arguments

Light spills an argument when `HasComplexArguments(expr)` is set (`ArgsContainingComplexExpression`). Flat cannot use that set.

During flat collect, when a parent sees `HasComplexExpression` after a child (the places that today call `MarkComplexArguments`), add that parent node index to `CompilerContext.FlatComplexArgNodes` (`SmallSet4<int, IntEq>`, `#if FLAT_EXPRESSION` only).

`TryEmitFlat` for `Call` / `New` / `Index` / array init checks that set and spills the same way the Light method does (local slot, reload). Copy the spill order from the Light method for that node kind. Do not invent a new order.

Done when: an argument that is itself a `Try` is not required yet; a nested call that collect marks complex matches Light. If no such tree is easy, a `Call` whose argument is a `Block` may wait for step 4 — then this step only stores the indexes and the emit check is a no-op until step 4. Say which one you did in the commit message.

### Step 4 — blocks and locals

Add `Block` and `Assign`.

Locals: push each block variable's `ChildIdx` for the duration of the block, pop by resetting the list count (collect already does this with `locals`). Emit stores them with `Stloc` / `Ldloc` in that order. Do not create `ParameterExpression`.

A parameter use whose id is on the locals list is a local. An id on the lambda parameter list is an argument. Anything else is a capture: step 4 returns false from emit for that node (fallback). Collect already sets `HasClosure` for captures inside nested lambdas and returns `ParameterIsNotVariableNorInPassedParameters` at the root.

Done when: the existing generated trees that use `LetMany` compile through `TryEmitFlat` (no fallback) and match Light.

### Step 5 — branches, loops, labels, try

Add `Conditional`, `Loop`, `Label`, `Goto`, `Switch`, `Try`, `TypeIs`, `TypeEqual`.

Bring label/scope data back, as ids:

- Label target id is the `ChildIdx` of the label-target node (the same id `ToLightExpression` uses to dedupe labels).
- Store goto and definition positions as `short` scope indexes, parallel to `GotoTargets` / `DefTargets`, but the target column is `ushort`, not `LabelTarget`.
- Reuse the visibility rules in `ValidateLabels`. If sharing the method requires `LabelTarget`, copy the rule into a `ValidateFlatLabels` next to it rather than allocating targets. Keep the Light method unchanged.
- Catch variable id is pushed like a block local (`TryCollectFlatCatch` already does this).
- `NotSupported_ExceptionCatchFilter` stays. Filters are not emitted.

This is the largest step. Do not also do nested lambdas here.

Done when: `Can_fuzz_optional_children_presence_in_flat_expression` compiles without the catch-filter cases through `CompileFast(ref fe)` and matches `lambda.Compile()` for the same inputs that test already runs. Catch-filter masks still skip compile.

### Step 6 — nested lambdas and invoke inlining

Nested `Lambda` node:

- Collect already pushes `NestedLambdaInfo`. Extend it so the flat nested lambda records captured parameter ids (`ushort`) instead of `NonPassedParameters` of `ParameterExpression`. Put the ids on a flat-only field. Do not allocate `ParameterExpression`.
- Compile the nested body with `TryEmitFlat` and the existing `TryCompileNestedLambda` closure-array path. The delegate still lands in `NestedLambdaInfo.Lambda`.
- `HasTheSameLambdaExpression` stays for Light dedup. Flat dedup, if needed, compares node indexes, not that method. One nested lambda per node is enough; do not add structural dedup in this step.

`Invoke` of a lambda node, when `NoInvocationLambdaInlining` is off:

- Collect already pushes the lambda's parameter ids onto locals and walks the body. Keep that.
- Emit assigns each argument into those locals, then emits the body. Do not build an `Expression` block.
- When the flag is on, emit `Invoke` as a call through the nested delegate (step 6 nested compile).

Done when: `CreateGeneratedFlatNested` compiles and matches Light, including a captured outer parameter. `Can_property_test_flat_try_collect_matches_light` still passes.

### Step 7 — remove the fallback

- Delete the `ToLightExpression` branch inside `CompileFast(ref ExprTree)`.
- `TryEmitFlat` for an unknown kind returns false, and compile returns null or throws, matching Light's `ThrowOnNotSupportedExpression` behavior.
- Quote, Dynamic, and RuntimeVariables stay unsupported (`Result.NotSupported_*` from collect). Do not emit them.

Done when: the test command passes, and a text search shows `CompileFast(ref this ExprTree` does not call `ToLightExpression`.

## After step 7 (do not start early)

- `Func<bool>` interpreter on flat (`Interpreter.TryInterpretBool`). Same result, no `DynamicMethod`.
- Construction clones in `MayBeCloneChildForOwner` when `NextIdx != 0`. That grows `Nodes` at build time. Look only after emit allocates nothing on the covered grammar.
- Sharing more of `TryEmit(Expression)` by changing it to take indexes. Only if a measured profile shows the duplicated switch is the cost. The duplicate is acceptable until then.

## Checklist for the agent at the end of a step

1. The step's node kinds emit without `ToLightExpression`. Later kinds still fall back.
2. New or extended test is called from `LightExpressionTests.Run`.
3. Test command passes.
4. No new public type besides the one `CompileFast` extension.
5. `ExprNode` layout is unchanged.
6. Light `TryEmit(Expression, ...)` signature is unchanged.
