# Rebase onto current upstream master, 4 November 2026

Base: fetched `dadhi/master` at `6dbc0cbcc9e09e0011256a5cfc3e627f7cc05fd8`.
Previous published #558 head: `bf908bd05c9e5ada3e8ada3e2f432b031b9e1524`.
Validated behavior revision: `1b83ed65076965f1cb4af6a5c4ac12f78cb7a9ae`. Subsequent code cleanup changes one line's indentation only; production/native-test diffs ignoring whitespace are empty. This report is amended into the existing final documentation commit.

The branch now contains **110 nonempty linear commits, no merges**, down from 114. `commit-map.csv` records four dropped commits and two trimmed commits. Its final documentation entry uses the ID before this amendment.

## Removed superseded work

- Drop `96a01a42`: Type-constant identity handling and RuntimeTypeConstantRegressionTests now come from merged #580.
- Drop `e21a58a9`: by-ref conversion handling and ByRefConversionRegressionTests now come from merged #581.
- Drop `f093cff4`: integer switch handling and IntegerSwitchDispatchRegressionTests now come from merged #583, including its 32-bit classification helper and smaller-width coverage.
- Drop `ea6cab25`: custom-node reduction now comes from merged #579, with its broader cache and validation tests.
- Trim `9cb18cf3`: merged #582 supplies the explicit Not implementation and MethodBasedNotRegressionTests. Retain only distinct lifted-complement and boxed-identity edge cases as a test commit.
- Trim `730319de`: remove the superseded feature-specific per-occurrence reduction cache and duplicate basic custom-node tests. Use master's shared reference-identity cache for both APIs. Retain fresh Dynamic/Quote metadata integration tests separately as DynamicQuotedReductionRegressionTests, plus safe identity-map lookups. The obsolete repeated-Reduce assertion is removed in favor of the upstream once-per-node contract, which remains covered by the unchanged upstream fixtures.

All seven upstream fixtures listed in `validation.json` are byte-for-byte unchanged relative to master. Both runners retain every upstream registration with no duplicates. Broader storage, operator, scope, exception, quote/runtime-variable and dynamic fixes remain. Conflict adjustments are folded into their owning commits; no cleanup merge or fixup commits remain. #559 is unchanged.

## Fresh validation

Windows ARM64, Release, SDK 10.0.301. Native runners cover Standard and LightExpression. Imported tests use strict FEC compilation with System fallback disabled.

| Check | Result |
| --- | --- |
| Full solution build | Passed; zero warnings/errors |
| Native .NET 10 | 1,202 TestX + 1,756 legacy passed |
| Native .NET 8 | 1,202 TestX + 1,755 legacy passed |
| Framework 4.7.2 | 1,162 TestX + 1,639 legacy passed |
| Imported runtime suite, .NET 10 | **35,088 passed; 59 existing skips; zero failures/errors/timeouts/aborts** |

Framework uses Windows PowerShell/.NET Framework with en-US culture for existing message assertions. Imported sources and exclusion classification are unchanged. The deep-tree stress exclusion remains deferred; no new skips or assertion relaxations were added. Zero crashes describes the executed suite.

`validation-logs.zip` contains successful build logs, native runs, imported test log and complete TRX. `source-sha256.json` records final source bytes. This rebase makes no new timing/allocation claim; earlier measurements are historical and do not describe this head.
