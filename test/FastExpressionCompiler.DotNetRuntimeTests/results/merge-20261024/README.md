# Merge upstream master and comparison review follow-up (24 October 2026)

Merged dadhi/master `5473b427` (including #562 and dadhi's `c00fe0ac` SmallMap repair) into #558 in `700053a1`. ImTools.cs is exactly upstream's version. The comparison conflict keeps #558's method-aware shared arithmetic emitter and reference-comparison handling; the standalone #562 emitter would duplicate that path. The #562 fixture and runner registrations are included. The interpreter method guard was already present. No changes to #559, new exclusions, compiler context layout or global caches.

`884ed766` adapts the #562 review feedback to `IsAtomicArithmeticOperand`: reuse `IsParamOrConstantOrDefault` to recognize ordinary conversions, while explicitly rejecting intrinsic emitters anywhere along a conversion chain. The scan stops immediately at an intrinsic. The existing intrinsic test is retained and extended to arithmetic; an added test covers converted parameters/constants/defaults under try/finally for both primitive arithmetic and selected comparison methods. Supporting custom emitters requires preserving their actual code-generation behavior even when their declared operand looks simple.

## Correctness

Validated production/test source: `884ed766`. Both production projects build across configured targets. Full .NET 10 ARM64 and .NET 8 ARM64 native suites pass (1,714 / 1,713 legacy tests, plus registered regressions). Framework passes 1,041 TestX and 1,597 legacy cases. All 21 imported batches pass on their first attempts: **35,064 executed passes; zero executed failures/crashes; the same 69 exclusions**. The deferred deep-tree cases remain deferred. Targeted #561 fixture: 11 Standard / 12 Light methods pass on .NET 8/10 with ARM64 and x64 hosts. Windows 11 ARM64; x64 runs under emulation. This does not claim the unrelated clean-master .NET 8 ARM64 Issue346 failure was fixed here; #558 already contains its own broader arithmetic repairs.

## Narrow measurements

Compare the merged source (`700053a1`) with the helper change (`884ed766`), using separately frozen source snapshots. Windows ARM64 .NET 8.0.28 / 10.0.9; Standard and Light; strict FEC and System compiled controls. Three process rounds, reversed run order, seven samples each: medians of 21 samples per phase/compiler/configuration. Each sample measures 2,000 compilations after 1,000 warmups, and 5,000,000 delegate calls after 100,000 warmups. Trees are prebuilt. Compilation includes delegate creation; invocation is measured separately. Allocation uses per-thread allocated bytes. All raw samples, both System control runs, emitted IL and source/binary hashes are preserved. The scalar control is a constant-folded addition, so it does not isolate helper-call overhead. These small synthetic observations are not a general performance or parity guarantee.

For the two converted EH shapes, IL removes two temporary locals and four store/load instructions. Converted addition compilation allocates **8 B less** in every tested configuration; selected-method and scalar compilation allocations are unchanged. All measured delegate invocation allocations are **0 B/op**. Conversion-shape compilation medians range from **8.9% faster to 2.8% slower**; the slower medians are Light net8 addition (+2.8%) and Light net10 selected method (+2.7%). Runtime medians range from about **0.2% faster to 2.5% slower** (largest absolute increase about 0.036 ns). These timing differences are retained, not treated as proof of universal speedup. System compile medians in the corresponding after controls are higher for these measured shapes, while runtime comparisons remain available in the JSON.

| Flavor/runtime | Shape | Compile us before → after | Compile B/op before → after | Runtime ns before → after | System compile us (after control) |
| --- | --- | ---: | ---: | ---: | ---: |
| Std net8.0 | scalar | 1.099 → 1.087 | 872.08 → 872.08 | 2.023 → 2.055 | 10.338 |
| Std net8.0 | converted-add-eh | 12.014 → 11.983 | 1480.16 → 1472.16 | 1.218 → 1.234 | 15.338 |
| Std net8.0 | converted-method-eh | 14.483 → 13.194 | 1504.16 → 1504.16 | 1.232 → 1.263 | 17.459 |
| Std net10.0 | scalar | 1.004 → 1.016 | 872.08 → 872.08 | 1.687 → 1.689 | 10.880 |
| Std net10.0 | converted-add-eh | 14.153 → 13.727 | 1480.17 → 1472.17 | 1.687 → 1.694 | 16.445 |
| Std net10.0 | converted-method-eh | 16.543 → 16.082 | 1504.17 → 1504.17 | 1.687 → 1.696 | 19.290 |
| Light net8.0 | scalar | 1.072 → 1.099 | 872.08 → 872.08 | 2.011 → 2.007 | 9.480 |
| Light net8.0 | converted-add-eh | 12.264 → 12.603 | 1480.16 → 1472.16 | 1.239 → 1.237 | 14.549 |
| Light net8.0 | converted-method-eh | 14.467 → 14.025 | 1504.16 → 1504.16 | 1.228 → 1.235 | 16.631 |
| Light net10.0 | scalar | 1.098 → 1.111 | 872.08 → 872.08 | 1.334 → 1.343 | 12.271 |
| Light net10.0 | converted-add-eh | 14.441 → 14.059 | 1480.17 → 1472.17 | 1.665 → 1.682 | 16.050 |
| Light net10.0 | converted-method-eh | 16.914 → 17.377 | 1504.17 → 1504.17 | 1.689 → 1.725 | 18.809 |

`validation.json` identifies source/assembly hashes and exact counters. `measurement-summary.json` contains all phase medians, including System runtime and allocation controls; `measurements.json` preserves the 1,008 raw sample rows. `evidence.zip` includes scripts, frozen source files, targeted results, measurement logs and full build/test logs. `evidence-manifest.json` records archive and entry hashes. Initial measurement-harness build/formatting errors are retained; they were corrected before measurements. No production performance claim is based on those failed harness attempts.
