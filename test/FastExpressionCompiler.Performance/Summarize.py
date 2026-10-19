"""Extract compact, commit-friendly evidence from BenchmarkDotNet full JSON exports."""
import argparse
import csv
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("results", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
rows = []
for path in sorted(args.results.glob("*-report-full.json")):
    for benchmark in json.loads(path.read_text(encoding="utf-8-sig"))["Benchmarks"]:
        statistics = benchmark["Statistics"]
        if statistics is None:
            raise SystemExit(f"Benchmark failed: {benchmark['FullName']}")
        rows.append(dict(phase=benchmark["Type"], method=benchmark["Method"],
            workload=benchmark["Parameters"].removeprefix("Workload="),
            mean_ns=statistics["Mean"], standard_error_ns=statistics["StandardError"],
            confidence999_halfwidth_ns=statistics["ConfidenceInterval"]["Margin"],
            samples=statistics["N"], allocated_bytes=benchmark["Memory"]["BytesAllocatedPerOperation"]))
if not rows:
    raise SystemExit("No results found")
args.output.parent.mkdir(parents=True, exist_ok=True)
with args.output.open("w", encoding="utf-8", newline="") as stream:
    writer = csv.DictWriter(stream, fieldnames=rows[0])
    writer.writeheader()
    writer.writerows(rows)
print(f"Saved {len(rows)} measurements to {args.output}")
