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
3. `todo` Widen the native emitter subset node by node, fuzzer guards each step.

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
