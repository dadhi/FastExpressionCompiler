"""Render bounded, untrusted measurement JSON; never extract or execute artifacts."""
import argparse
import json
from pathlib import Path
import zipfile
from compare import render, validate_report


def read_artifact(path, metadata):
    if path.stat().st_size > 1024 * 1024: raise ValueError("Artifact too large")
    with zipfile.ZipFile(path) as z:
        entries = z.infolist()
        if len(entries) != 1 or entries[0].filename != "report.json" or entries[0].file_size > 256 * 1024:
            raise ValueError("Unexpected archive contents")
        if (entries[0].external_attr >> 16) & 0o170000 == 0o120000: raise ValueError("Symlink artifact")
        report = json.loads(z.read(entries[0]))
    validate_report(report)
    c = report["context"]
    if c["repository"] != metadata["repository"] or c["number"] != metadata["number"] or c["head_sha"] != metadata["head_sha"] or c["runtime"] != "net10.0":
        raise ValueError("Artifact is not for this PR head/runtime")
    return report


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--directory", type=Path, required=True)
    a = p.parse_args()
    m = json.loads((a.directory / "metadata.json").read_text())
    url = m["run_url"]
    try:
        report = read_artifact(a.directory / "performance-report.zip", m)
        body = render(report, url)
        baseline = report["context"]["base_sha"]
    except (OSError, ValueError, KeyError, TypeError, zipfile.BadZipFile, RuntimeError):
        body = "### Performance comparison: inconclusive\n\nThe benchmark run did not provide valid, complete report data for this PR head. No performance pass is claimed.\n\n" + f"[Run and artifacts]({url})\n"
        baseline = None
    payload = dict(m, body=body, reported_base_sha=baseline)
    (a.directory / "comment.json").write_text(json.dumps(payload), encoding="utf-8")


if __name__ == "__main__":
    main()
