import copy
import importlib.util
import io
import json
from pathlib import Path
import sys
import subprocess
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import compare
from publish import read_artifact


def stats(mean=100, low=99, high=101, allocated=0):
    return dict(mean=mean, lower=low, upper=high, n=5, bytes=allocated)


def report():
    return dict(schema=1, context=dict(repository="dadhi/FastExpressionCompiler", number=7,
        base_sha="a"*40, head_sha="b"*40, harness_sha="a"*40, harness_source="base", runtime="net10.0", sdk="10.0.301"),
        problems=[], rows=[dict(zip(("method", "backend", "shape"), k), rounds=[dict(baseline=stats(), candidate=stats()) for _ in range(2)]) for k in sorted(compare.EXPECTED)])


class PolicyTests(unittest.TestCase):
    def test_baseline_control_is_complete(self):
        self.assertTrue(compare.validate_report(report()))
        self.assertIn("Comparison complete", compare.render(report()))

    def test_significance_required_in_both_rounds(self):
        row = report()["rows"][0]
        row["rounds"][0]["candidate"] = stats(120,119,121)
        self.assertFalse(compare.classify(row)[2])
        row["rounds"][1]["candidate"] = stats(120,119,121)
        self.assertTrue(compare.classify(row)[2])
        row["rounds"][1]["candidate"] = stats(120,90,150)
        self.assertFalse(compare.classify(row)[2])

    def test_zero_allocation_needs_both_rounds(self):
        row = report()["rows"][0]
        row["rounds"][0]["candidate"]["bytes"] = 24
        self.assertFalse(compare.classify(row)[2])
        row["rounds"][1]["candidate"]["bytes"] = 24
        self.assertTrue(compare.classify(row)[2])

    def test_missing_results_are_inconclusive(self):
        r = report(); r["rows"].pop()
        self.assertFalse(compare.validate_report(r))
        self.assertIn("Inconclusive", compare.render(r))

    def test_duplicate_and_nonfinite_results_rejected(self):
        r = report(); r["rows"].append(copy.deepcopy(r["rows"][0]))
        with self.assertRaises(ValueError): compare.validate_report(r)
        r = report(); r["rows"][0]["rounds"][0]["candidate"]["mean"] = float("nan")
        with self.assertRaises(ValueError): compare.validate_report(r)

    def test_problems_cannot_inject_markdown(self):
        r = report(); r["problems"] = ["@everyone [click](https://attacker.invalid)"]
        self.assertNotIn("attacker", compare.render(r))
        self.assertNotIn("@everyone", compare.render(r))

    def test_dry_measurements_not_accepted(self):
        r = report(); r["rows"][0]["rounds"][0]["baseline"]["n"] = 1
        with self.assertRaises(ValueError): compare.validate_report(r)

    def test_configuration_mismatch_and_missing_input(self):
        with tempfile.TemporaryDirectory() as folder:
            d=Path(folder); (d/"context.json").write_text(json.dumps(report()["context"]))
            result=compare.compare(d)
            self.assertFalse(compare.validate_report(result))
            self.assertEqual(len(result["rows"]),0)


class ComparisonIntegrationTests(unittest.TestCase):
    def test_cli_policy_and_configuration_identity(self):
        with tempfile.TemporaryDirectory() as folder:
            d=Path(folder); (d/"context.json").write_text(json.dumps(report()["context"]))
            for rd in range(2):
                for version in ("baseline","candidate"):
                    benchmarks=[]
                    for i,(method,backend,shape) in enumerate(sorted(compare.EXPECTED)):
                        mean=120 if version=="candidate" and i==0 else 100
                        benchmarks.append(dict(Method=method,FullName=f'Regression.PerformanceBenchmarks.{method}(Backend: "{backend}", Shape: "{shape}")',
                            Parameters=f"Backend={backend}&Shape={shape}",DisplayInfo=f"{method}: Regression [Backend={backend}, Shape={shape}]",HardwareIntrinsics="test",
                            Statistics=dict(Mean=mean,N=5,ConfidenceInterval=dict(Lower=mean-1,Upper=mean+1,Level=12)),Memory=dict(BytesAllocatedPerOperation=0)))
                    data=dict(HostEnvironmentInfo=dict(RuntimeVersion=".NET 10.0.9",BenchmarkDotNetCaption="0.15.8",Architecture="X64",Configuration="Release",DotNetCliVersion="10.0.301"),Benchmarks=benchmarks)
                    path=d/"runs"/f"{rd}-{version}";path.mkdir(parents=True);(path/"suite-full.json").write_text(json.dumps(data))
            command=[sys.executable,str(Path(compare.__file__)),"--input",str(d)]
            self.assertEqual(subprocess.run(command,capture_output=True).returncode,0)
            self.assertEqual(subprocess.run(command+["--blocking"],capture_output=True).returncode,2)
            self.assertIn("Regression (both rounds)",(d/"report.md").read_text())
            path=d/"runs/1-candidate/suite-full.json";data=json.loads(path.read_text());data["HostEnvironmentInfo"]["RuntimeVersion"]="different runtime";path.write_text(json.dumps(data))
            self.assertEqual(subprocess.run(command,capture_output=True).returncode,1)
            self.assertIn("Inconclusive",(d/"report.md").read_text())


class ArtifactTests(unittest.TestCase):
    metadata = dict(repository="dadhi/FastExpressionCompiler", number=7, head_sha="b"*40)

    def artifact(self, contents, name="report.json"):
        f=tempfile.NamedTemporaryFile(suffix=".zip", delete=False)
        p=Path(f.name); f.close(); self.addCleanup(p.unlink)
        with zipfile.ZipFile(p,"w") as z:z.writestr(name, json.dumps(contents))
        return p

    def test_valid_artifact(self):
        self.assertTrue(compare.validate_report(read_artifact(self.artifact(report()), self.metadata)))

    def test_stale_or_foreign_report(self):
        for key,value in [("head_sha","c"*40),("repository","attacker/repo"),("number",8),("runtime","net8.0")]:
            r=report();r["context"][key]=value
            with self.assertRaises(ValueError):read_artifact(self.artifact(r),self.metadata)

    def test_never_extracts_paths_or_accepts_extra_files(self):
        with self.assertRaises(ValueError):read_artifact(self.artifact(report(),"../report.json"),self.metadata)
        p=self.artifact(report())
        with zipfile.ZipFile(p,"a") as z:z.writestr("run.py","raise RuntimeError('must never execute')")
        with self.assertRaises(ValueError):read_artifact(p,self.metadata)

    def test_compressed_oversize_rejected(self):
        p=self.artifact(report())
        with zipfile.ZipFile(p,"w",zipfile.ZIP_DEFLATED) as z:z.writestr("report.json"," "*300000)
        with self.assertRaises(ValueError):read_artifact(p,self.metadata)


if __name__ == "__main__":unittest.main()
