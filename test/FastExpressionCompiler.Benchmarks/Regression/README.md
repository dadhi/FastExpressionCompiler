# Pull-request performance comparisons

Addresses the repeatable before/after comparison requested in [#64](https://github.com/dadhi/FastExpressionCompiler/issues/64). This is the reporting phase: performance differences never block merging by default. Build failures and missing/invalid measurements are inconclusive, not passes. The separate minimal-emitted-IL experiment proposed in that discussion is outside this workflow.

## What runs

`.github/workflows/performance.yml` runs on PRs targeting master, including new commits and draft PRs. It snapshots the exact event base and head commits, rather than the synthetic merge commit. Each runtime job builds both libraries in Release on one disposable Ubuntu 24.04 runner, using SDK **10.0.301**, BenchmarkDotNet **0.15.8**, tiering and ReadyToRun disabled. Nothing changes the existing exploratory benchmark entry point.

The default PR pass targets .NET 10. The manual **PR performance** workflow accepts a PR number and can target .NET 8, .NET 10 or both. The .NET 8 installation is pinned to SDK 8.0.422 (runtime 8.0.28); the actual build SDK remains 10.0.301. Keep these explicit pins current when updating supported targets.

Four workloads cover scalar arithmetic, a balanced arithmetic tree, method/property access with a condition, and a nested-lambda capture. One `Workloads.cs` is compiled twice, against Standard and LightExpression, so the definitions stay identical. Light construction uses native Light nodes. Strict `CompileFast` rejects fallback; setup checks results for negative, zero and positive inputs.

Each of the eight workload/backend combinations measures:

- `Construct`: tree construction, including applicable reflection lookups.
- `Compile`: compile a prebuilt tree and create the delegate.
- `CompileAndFirstInvoke`: compile and invoke each new delegate, including deferred JIT work. Closure factories invoke both the outer and resulting inner delegate.
- `Invoke`: warmed delegate execution in a consumed 256-operation batch. Closure factories include activation and inner invocation.

MemoryDiagnoser measures managed B/op for every phase. Invocation calls typed compiled delegates directly, with the workload branch outside the batch. Loop/checksum overhead is shared; these are relative regression probes, not minimum delegate-call latency claims. Compilation and first invocation stay separate because dynamic-method hosting can move JIT work across the `CreateDelegate` boundary.

There are 32 cases, five requested measurement iterations after two warmups per case, and two process rounds in opposite order: **base, PR; PR, base**. Builds, correctness checks and outputs are isolated. SDK/source/library hashes, runtime/environment logs and complete BDN JSON are uploaded. The intended hosted-runner budget is roughly 5-15 minutes; the job has a 30-minute cap. Observe actual CI costs before expanding the suite.

## Stable workloads

After installation, the harness and comparator come from the exact base commit, and that one harness is copied for both revisions. A PR cannot simply replace its benchmark definitions for only the candidate run. New harness definitions take effect after merging, for subsequent comparisons.

For this initial installation PR only, the base lacks a harness. A clearly labelled **bootstrap** uses one candidate harness for both revisions. This still tests the same definitions on both versions, but does not claim base-branch provenance. The artifacts record the harness SHA and hashes.

Benchmark/build code is executable PR code and can deliberately lie or tamper with measurements. These reports are review aids, not tamper-proof attestations or authorization to merge. Do not promote an unreviewed benchmark artifact into a trusted executable.

## Interpretation and policy

`compare.py` matches phase/backend/workload plus BDN full identity, job display/configuration, runtime, architecture, SDK and hardware-intrinsic configuration. All 32 identities must be present in all four runs. Duplicate cases, incompatible jobs, unavailable statistics, nonfinite values or fewer than three usable samples make the comparison inconclusive. A dry smoke run intentionally cannot produce a performance pass.

The report shows absolute times, median per-round relative time changes, allocations and assessments:

| Evidence | Default action |
| --- | --- |
| Less than 3% time difference | Report as within variation |
| At least 3% slower without confirmation | Warning / noise |
| More than 5% slower and candidate confidence interval entirely above base in **both** rounds | Potential confirmed regression; report only |
| Zero allocation becomes at least 0.5 B/op in **both** rounds | New-allocation regression; report only |
| Other allocation increases above 0.5 B/op in both rounds | Report allocation increase |
| Missing/invalid/mismatched results | Inconclusive; infrastructure check fails |

BDN's exported confidence intervals are used without pooling different runs. Five samples per round and shared hardware still have limits; no claim of machine isolation is made. Configure `--warning-percent` and `--regression-percent` locally. `--blocking` opts into exit code 2 for confirmed time or new-zero-path allocation regressions; it is deliberately **not enabled in the workflow**. Exit 1 means inconclusive/infrastructure failure; exit 0 is a completed reporting-only comparison, which can still contain reported regressions.

Before enabling a required regression check, collect same-source controls over time, rerun suspicious cases and calibrate on stable, preferably dedicated isolated hardware. Scheduling, history storage and automatic confirmation runs are future reliability work. Do not run arbitrary fork code on a persistent self-hosted runner.

## PR comments and trust boundary

The benchmark job uses `pull_request`, read-only repository permissions, no repository secrets, no persistent checkout credentials and no shared caches. GitHub's normal fork-run approval policy still applies.

A separate `.github/workflows/performance-comment.yml` uses `workflow_run`. It checks out **only the default branch** and never builds the candidate or executes downloaded files. It resolves the PR via GitHub's authenticated commit association (fork runs can have an empty `pull_requests` array), requires the current PR head and expected workflow/attempt, and reads just one size-bounded `report.json` inside the artifact ZIP without extracting it. The trusted renderer validates identities/numbers and ignores artifact-supplied prose. A reported older base must be verifiable as master history; otherwise the comment is inconclusive.

For a new head commit it posts a new report, marks the previous bot report **Outdated** and collapses its measurements with a link to the new report. Rerunning the same head updates that head's report instead of duplicating it. Stale heads and late older runs cannot overwrite a newer report. Missing artifacts produce an inconclusive report. Comments link to the run and complete artifacts (14-day retention); they do not delete historical results.

The publisher must first be merged into the default branch before GitHub can trigger it. Therefore this installation PR can have action summaries/artifacts before merge, but automatic publishing starts after installation. Manual runs publish job summaries/artifacts only; they do not use an artifact-supplied PR identity to obtain comment privileges.

See GitHub's [secure use reference](https://docs.github.com/en/actions/reference/security/secure-use) for the privilege separation behind this design.

## Local validation and replay

With separate clean checkouts for the exact baseline and candidate:

```sh
python3 test/FastExpressionCompiler.Benchmarks/Regression/run.py \
  --baseline /path/to/base --candidate /path/to/candidate \
  --runtime net10.0 --output /path/to/new-results
python3 test/FastExpressionCompiler.Benchmarks/Regression/compare.py \
  --input /path/to/new-results
```

The output directory must be new. `--verify-only` builds both versions and checks all eight behavior oracles without measuring. `--smoke` exercises BDN's Dry job and exports without treating them as meaningful measurements. `--filter` is useful for diagnostics, but an incomplete subset is inconclusive under the full-suite policy. SDK 10.0.301 must be installed; .NET 8 replay also needs its runtime. `logs/environment.log` records the actual environment.

Run policy/publisher tests with:

```sh
python3 -m unittest discover -s test/FastExpressionCompiler.Benchmarks/Regression/tests -v
node --test test/FastExpressionCompiler.Benchmarks/Regression/tests/publish.test.cjs
```

The tests cover confidence in both rounds, allocation confirmation, reporting versus blocking exit codes, mismatched configurations, missing data, malformed/untrusted ZIPs, stale/foreign reports, fork PR resolution, comment supersession and late-run ordering. They mock GitHub writes; local tests do not post comments.
