"""Compare full BenchmarkDotNet JSON; default policy is reporting only."""
import argparse
import itertools
import json
import math
from pathlib import Path
import re
import statistics

METHODS = ("Construct", "Compile", "CompileAndFirstInvoke", "Invoke")
BACKENDS = ("Standard", "Light")
SHAPES = ("Scalar", "Arithmetic", "CallConditional", "Closure")
EXPECTED = set(itertools.product(METHODS, BACKENDS, SHAPES))


def finite(value, minimum=None):
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or abs(value) > 1e15:
        raise ValueError("Invalid numeric measurement")
    if minimum is not None and value < minimum:
        raise ValueError("Negative measurement")
    return value


def validate_stats(s):
    if set(s) != {"mean", "lower", "upper", "n", "bytes"}:
        raise ValueError("Invalid measurement fields")
    for key in ("mean", "lower", "upper", "bytes"):
        finite(s[key], 0 if key in ("mean", "bytes") else None)
    if s["mean"] <= 0 or not s["lower"] <= s["mean"] <= s["upper"]:
        raise ValueError("Invalid confidence interval")
    if type(s["n"]) is not int or not 3 <= s["n"] <= 1000:
        raise ValueError("Insufficient measurement samples")


def validate_report(report):
    if not isinstance(report, dict) or not isinstance(report.get("context"), dict) or not isinstance(report.get("rows"), list) or len(report["rows"]) > len(EXPECTED):
        raise ValueError("Invalid report shape")
    if report.get("schema") != 1 or type(report.get("problems")) is not list or len(report["problems"]) > 128:
        raise ValueError("Invalid report schema")
    c = report["context"]
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", c["repository"]):
        raise ValueError("Invalid repository")
    for key in ("base_sha", "head_sha", "harness_sha"):
        if not re.fullmatch(r"[0-9a-f]{40}", c[key]): raise ValueError("Invalid revision")
    if c["runtime"] not in ("net8.0", "net10.0") or c["sdk"] != "10.0.301": raise ValueError("Invalid runtime")
    if c["harness_source"] not in ("base", "bootstrap"): raise ValueError("Invalid harness provenance")
    if c["harness_sha"] != c["base_sha" if c["harness_source"] == "base" else "head_sha"]: raise ValueError("Invalid harness revision")
    if type(c["number"]) is not int or c["number"] < 0: raise ValueError("Invalid PR number")
    seen = set()
    for row in report["rows"]:
        key = tuple(row[k] for k in ("method", "backend", "shape"))
        if key not in EXPECTED or key in seen: raise ValueError("Invalid/duplicate benchmark identity")
        seen.add(key)
        if len(row["rounds"]) != 2: raise ValueError("Missing process round")
        for rd in row["rounds"]:
            if set(rd) != {"baseline", "candidate"}: raise ValueError("Missing revision")
            for stats in rd.values(): validate_stats(stats)
    return seen == EXPECTED and not report["problems"]


def classify(row, warning=3.0, regression=5.0):
    rounds = row["rounds"]
    delta = statistics.median((r["candidate"]["mean"] / r["baseline"]["mean"] - 1) * 100 for r in rounds)
    confirmed = all(r["candidate"]["mean"] > r["baseline"]["mean"] * (1 + regression / 100)
                    and r["candidate"]["lower"] > r["baseline"]["upper"] for r in rounds)
    new_alloc = all(r["baseline"]["bytes"] <= .01 and r["candidate"]["bytes"] >= .5 for r in rounds)
    more_alloc = all(r["candidate"]["bytes"] > r["baseline"]["bytes"] + .5 for r in rounds)
    improved = delta < -warning and all(r["candidate"]["upper"] < r["baseline"]["lower"] for r in rounds)
    status = "Regression (both rounds)" if confirmed else "New allocation (both rounds)" if new_alloc else "Allocation increase" if more_alloc else "Warning / noise" if delta >= warning else "Improvement" if improved else "Within variation"
    return delta, status, confirmed or new_alloc


def render(report, artifact_url=None, warning=3.0, regression=5.0):
    complete = validate_report(report)
    c = report["context"]
    rows = sorted(report["rows"], key=lambda r: (r["backend"], r["method"], r["shape"]))
    confirmed = sum(classify(r, warning, regression)[2] for r in rows)
    title = "Inconclusive" if not complete else "Potential regressions detected" if confirmed else "Comparison complete"
    lines = [f"### {c['runtime']}: {title} (reporting only)", "",
             f"Base `{c['base_sha'][:12]}` Ã¢â€ â€™ PR `{c['head_sha'][:12]}`; SDK `{c['sdk']}`; harness `{c['harness_sha'][:12]}` ({c['harness_source']}).", ""]
    if not complete: lines += ["Missing or invalid benchmark results. This is not evidence of a performance pass.", ""]
    if c["harness_source"] == "bootstrap": lines += ["Bootstrap: the base does not contain this harness yet. One candidate harness was used for both revisions; later PRs use the base-branch harness.", ""]
    lines += ["| Backend / phase / workload | Base ns | PR ns | Time change | B/op: base Ã¢â€ â€™ PR | Assessment |", "| --- | ---: | ---: | ---: | ---: | --- |"]
    for r in rows:
        means = {v: statistics.median(rd[v]["mean"] for rd in r["rounds"]) for v in ("baseline", "candidate")}
        allocations = {v: statistics.median(rd[v]["bytes"] for rd in r["rounds"]) for v in means}
        delta, status, _ = classify(r, warning, regression)
        lines.append(f"| {r['backend']} / {r['method']} / {r['shape']} | {means['baseline']:.2f} | {means['candidate']:.2f} | {delta:+.1f}% | {allocations['baseline']:.2f} Ã¢â€ â€™ {allocations['candidate']:.2f} | {status} |")
    lines += ["", f"Two opposite-order process rounds on one GitHub-hosted runner; BDN confidence intervals must be separated in both rounds to classify a >{regression:g}% time regression. A Ã¢â€°Â¥3% change without that evidence is a warning, not proof. Five measurement iterations/round; hosted-runner noise remains.", "",
              "Compile excludes delegate invocation; CompileAndFirstInvoke includes the first call/JIT of a new delegate. Invoke is warmed; closure invocation includes creating and calling the returned delegate. MemoryDiagnoser reports managed B/op. Loop/checksum overhead is shared by both revisions."]
    if artifact_url: lines += ["", f"[Workflow run and complete raw artifacts]({artifact_url})"]
    return "\n".join(lines) + "\n"


def read_bdn(folder):
    result = {}
    for path in folder.rglob("*-full.json"):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        environment = data["HostEnvironmentInfo"]
        runtime = environment["RuntimeVersion"]
        for b in data["Benchmarks"]:
            text = b["FullName"] + " " + b["Parameters"]
            backend = re.search(r'Backend\s*[:=]\s*"?(Standard|Light)', text)
            shape = re.search(r'Shape\s*[:=]\s*"?(Scalar|Arithmetic|CallConditional|Closure)', text)
            key = (b["Method"], backend[1] if backend else None, shape[1] if shape else None)
            if key not in EXPECTED or key in result: raise ValueError("Unexpected or duplicate benchmark")
            s = b["Statistics"]; ci = s["ConfidenceInterval"]
            stats = dict(mean=s["Mean"], lower=ci["Lower"], upper=ci["Upper"], n=s["N"], bytes=b["Memory"]["BytesAllocatedPerOperation"])
            validate_stats(stats)
            identity = (b["FullName"], b["DisplayInfo"], runtime, environment["BenchmarkDotNetCaption"],
                        environment["Architecture"], environment["Configuration"], environment["DotNetCliVersion"],
                        str(b["HardwareIntrinsics"]), ci["Level"])
            result[key] = (stats, identity)
    if not result: raise ValueError("No full BDN results")
    return result


def compare(folder):
    report = dict(schema=1, context=json.loads((folder / "context.json").read_text()), rows=[], problems=[])
    runs = {}
    for rd, version in itertools.product(range(2), ("baseline", "candidate")):
        try: runs[rd, version] = read_bdn(folder / "runs" / f"{rd}-{version}")
        except (ValueError, TypeError, KeyError, OSError):
            report["problems"].append("Invalid or missing results")
            runs[rd, version] = {}
    for key in sorted(EXPECTED):
        try:
            entries = [runs[rd, v][key] for rd, v in itertools.product(range(2), ("baseline", "candidate"))]
            if len({e[1] for e in entries}) != 1: raise ValueError("Different execution configurations")
            report["rows"].append(dict(zip(("method", "backend", "shape"), key), rounds=[{v: runs[rd, v][key][0] for v in ("baseline", "candidate")} for rd in range(2)]))
        except (ValueError, KeyError): report["problems"].append("Missing or mismatched benchmark identity")
    return report


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--input", type=Path, required=True)
    p.add_argument("--blocking", action="store_true", help="Opt in only after calibrating on stable hardware")
    p.add_argument("--warning-percent", type=float, default=3)
    p.add_argument("--regression-percent", type=float, default=5)
    a = p.parse_args()
    if not 0 < a.warning_percent < a.regression_percent < 100: p.error("Require 0 < warning < regression < 100")
    report = compare(a.input)
    (a.input / "report.json").write_text(json.dumps(report, allow_nan=False, indent=2), encoding="utf-8")
    markdown = render(report, warning=a.warning_percent, regression=a.regression_percent)
    (a.input / "report.md").write_text(markdown, encoding="utf-8")
    print(markdown)
    if not validate_report(report): return 1
    return 2 if a.blocking and any(classify(r, a.warning_percent, a.regression_percent)[2] for r in report["rows"]) else 0


if __name__ == "__main__":
    raise SystemExit(main())
