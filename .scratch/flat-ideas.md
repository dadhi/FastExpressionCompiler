# Flat expression: compile-path ideas and their status

Status: `todo` / `tried: <result>` / `dropped: <why>`.
Baseline (Release net10.0, 200 exprs, ~35 nodes, noisy machine - compare rows within one run only):
flat collect 1.6-2.6us, Light collect 6.4-7.5us, flat native compile 12-13us, Light FEC 26-37us,
flat->sys->light->FEC 64-108us, System.Compile 169-198us.

## Steps
1. `tried: done` Direct Flat -> Light converter (`FlatToLightExpression.cs`, mirrors `SysExprBuilder`), Flat -> System kept.
   convert only 43.6us -> 15.0us; flat->light->FEC 45us vs 108us via System (same run). Verified: all tests, the generated-tree
   property test, and the optional-children fuzzer (direct == via-system structurally, compiles and agrees with System).
   Cost: ~330 duplicated lines (the two builders must be kept in sync). `GetChildren`/`ReadInlineValue` are shared statics on `ExprTree`.
2. `tried: done` Light -> Flat directly (`FlatFromLightExpression.cs`, mirrors `AddSysExpression`); Sys -> Flat already existed.
   convert only 35.8us (via System) -> 14.6us (direct). `FromLightExprViaSystem` and `ToLightExpressionViaSystem` are kept as oracles.
   Found and fixed a real bug on the way: Light `LoopExpression.CreateSysExpression` dropped the continue label when there was no
   break label (Expression.cs); the optional-children fuzzer exposed it as a flat->light->flat mismatch in 80 of 300 trees.
   The fuzzer now asserts flat->light->flat structural equality and direct == via-system.
3. `tried: measured` README expressions (`new X(closure a, b)`, `n => () => n + 1`) built flat (`Flat_compiles_the_readme_expressions_and_compares_the_time`).
   Both are outside the native subset (closure constant, nested lambda, New), so `CompileFast` = probe (0.1-0.3us) + direct flat->light (~2-4us) + Light FEC (~8us / ~47us).
   The conversion is a 25-40% overhead on top of Light for these; single noisy runs, the order of measurements moves numbers by 2x.
   Conclusion: the native subset must grow to closures/New/nested lambdas before flat beats Light on the README cases.
4. `tried: done` Widened the native subset: reference-typed params/locals/results, `New`, static+instance `Call`, Field/Property getter,
   closure constants (the `ArrayClosure` array is filled from `ClosureConstants`), string and null constants. The collect checks by construction what IL needs
   (`FlatFits` only for the lambda return/params vs the delegate; per-node arg/instance type checks were dropped to match Light: the factories validate, and
   `GetParameters()` allocated per Call/New. A tree built wrong now emits bad IL instead of falling back.)
   README 1 (`new X(closure a, b)`): light FEC 5.2us -> flat native 3.2us (Release, one noisy run); README 2 (nested lambda) still falls back.
   Cost: ~60 lines in FlatExpressionCompiler.cs.
5. `todo` Nested lambdas (closure with nested-lambda slots), Convert, value types (struct params/Nullable), Invoke, arrays, loops/labels/try. Each with a fuzz or deterministic test.

## Construction-time ideas
- `todo` Profile node access: the scan is ~45ns/node, far from memory bound. Suspect the Stack32 indexer / Type compares.
- `todo` Parent index per node (2 bytes/node, one store at attach): ancestor walk gives scope check and closure-capture detection without an explicit stack.
- `todo` Aggregate flags OR-ed from the children at construction (has lambda / try / goto / closure const / non-int): root flags let simple trees skip collect.
- `todo` Seal pass: renumber to post-order once; subtree = contiguous range, O(1) ancestor test, sizes, usage counts in one pass.
- `todo` SOA split: hot fields (NodeType, ChildIdx, NextIdx, ChildCount) apart from Type/Obj; intern Type to a ushort id.
- `todo` Contiguous child lists instead of first-child + sibling chain (only with Seal).
- `todo` The builders take `ExprTree` by value (big Stack32 copy); pass by `in`/ref.
- `dropped (for now): closure-constant usage counts` - trivial in the collect scan, ClosureConstants.Count==0 already covers the no-closure case.
- `dropped (for now): parameter declaring owner alone` - weak without a parent link; parent index replaces it.
- `deferred: label id table` - conflicts with the "drop info lists" direction.

## Step 6: closures / nested lambdas (tried)
- Design: captured vars live in StrongBox (shared mutation), nested lambda = separate DynamicMethod, open delegate stored in the shared constants array, curried at run-time with ArrayClosureWithNonPassedParams (same as FEC).
- Cost: recursive capture pre-pass + per-compile lists, only when lambdas > 1.
- Measured (noisy, Release): README 2 flat native ~30us vs Light FEC ~29us, so no win yet; the DynamicMethod per lambda dominates. README 1 stays ~3x faster natively.
- Todo: Convert, value types, Invoke, arrays, loops/labels/try, native-vs-System fuzzer for new nodes, linear capture scan via construction-time parent links.

## Step 7: construction helps for the nested lambdas (tried, measured)
- 1 LambdaCount at construction: DONE. Collect no longer counts, the constants array is sized upfront.
- 2 linear capture scan by index range: REJECTED, unsound. The nodes of a subtree are not contiguous (shared nodes are built earlier, clones are appended later). Instead: the same top-down pass but with no allocations (SmallList on the stack, one FlatCtx object).
- 3 AssignedParams recorded by Assign: DONE. The never-assigned captured variable goes by value (boxed in NonPassedParams, no StrongBox, plain local in the declaring lambda). Separate list, not a node flag, because the comparer/hash read the flags.
- 4 counts per tree: DONE with 1 (closure constants count was already known).
- 5 SOA of the nodes: NOT TRIED. The collect scan of README 2 is 0.23 us of ~7 us, the floor of 2 bare DynamicMethods is 3 us; the layout is not the bottleneck.
- Biggest win was not structural: the per-delegate-type cache (Invoke, param types, open Func type, Curry method). Collect 0.71 -> 0.23 us, README 2 native 9.5 -> ~6.6 us (Light FEC ~10-11, min of 9 rounds).
- Todo: the remaining ~3 us over the DynamicMethod floor (IL token resolution, ctx), Convert, value types, Invoke, arrays, loops/try, native-vs-System fuzzer.

## Step 8: via-System dropped from prod, README + ComplexExpression native (done)
- ToLightExpressionViaSystem / FromLightExprViaSystem removed from src; kept as test-only helpers for the round-trip fuzz.
- Native now: MemberInit (field/property bindings), NewArrayInit, ArrayIndex (rank 1), reference Convert (castclass, no user-defined/numeric/boxing). Hoisted `() => new X(a, b)`, manual lambda, `n => () => n + 1` and Complex all compile without fallback.
- Release (noisy), us / bytes per op: README 1 flat 4.4 / 1544 vs Light 7.7 / 1080 (prebuilt); Complex flat native 9.7 / 2016 vs Light FEC 13.8 / 1656; hoisted flat 5.1 / 1832 vs Light 7.6 / 1232. Two bare DynamicMethods floor: 2304 B.
- Allocation: flat allocates more than Light on one-lambda trees (FlatCtx, constants copy, delegate closure), but nothing here is above the DynamicMethod floor. TRIED nothing more; caching StrongBox Value/ctor lookups NOT DONE (only on assigned-captured path, not in README cases).
- Todo: alloc of FlatCtx (struct on stack), skip constants copy when the tree constants array can be reused, value-type members, Invoke, loops/try, native-vs-System fuzzer.

## Step 9: allocations (done, measured)
- FlatCtx is one [ThreadStatic] instance (it embeds the 8-slot capture buffer, was allocated per compile); Constants is cleared after the compile. DONE.
- StrongBox type/ctor/Value field cached per variable type (FlatBox). DONE.
- Release B/op before -> after: README 1 flat 1544 -> 1368, README 2 3024 -> 2848, Complex 2016 -> 1840, hoisted 1832 -> 1656. Light FEC: 1080 / 3000 / 1656 / 1232. Floor of one DynamicMethod is ~1150 B.
- Left: object[] constants copy (could reuse the tree array if ImTools SmallList exposed its heap array), the ArrayClosure + delegate (needed), ILGenerator/LocalBuilder (BCL). Flat is now ~290 B over Light on one-lambda trees, below Light on README 2.
- Todo: ImTools SmallList hand-over of its backing array to skip the copy; check what else allocates in the 290 B with an allocation profiler.

## Step 10: reuse the FEC pooling (done, measured)
- Learned from FEC TryCompileBoundToFirstClosureParam: it rents/frees the per-thread ILGenerator (DynamicMethodHacks.RentPooledOrNewILGenerator/FreePooledILGenerator), pools the closure+param types arrays, interprets Func<bool> constants, and sizes one object[] for constants+nested lambdas. Flat now uses the ILGenerator pool. DONE: -240 B per compile.
- Release B/op now: README 1 flat 1128 (Light 1080), README 2 2608 (3000), Complex 1448 (1656), hoisted 1416 (1232). Time: flat 3.1 / 6.6 / 6.6 / 3.3 us vs Light 4.1 / 10.9 / 9.2 / 4.8.
- Todo: Func<bool> interpreter shortcut (Interpreter is FEC's), the pooled local-var path (GetNextLocalVarLocation), create+compile rows (tree building cost is not measured yet), the nested ILGenerator is not pooled while the parent holds it (same as FEC).
