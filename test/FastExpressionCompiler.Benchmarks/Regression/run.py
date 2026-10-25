"""Build exact revisions and run one frozen BDN harness in counterbalanced rounds."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

SDK = "10.0.301"
SUITE = Path("test/FastExpressionCompiler.Benchmarks/Regression")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--baseline", type=Path, required=True)
    p.add_argument("--candidate", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    p.add_argument("--runtime", choices=["net8.0", "net10.0"], default="net10.0")
    p.add_argument("--verify-only", action="store_true")
    p.add_argument("--smoke", action="store_true", help="Dry run, not valid performance evidence")
    p.add_argument("--filter", default="*")
    a = p.parse_args()
    out = a.output.resolve()
    out.mkdir(parents=True, exist_ok=False)
    (out / "logs").mkdir()
    roots = {"baseline": a.baseline.resolve(), "candidate": a.candidate.resolve()}
    shas = {v: subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=r, text=True).strip() for v, r in roots.items()}
    harness_root = roots["baseline"] if (roots["baseline"] / SUITE / "Program.cs").exists() else roots["candidate"]
    context = dict(repository=os.environ.get("GITHUB_REPOSITORY", "local/local"),
                   number=int(os.environ.get("PR_NUMBER", "0")), base_sha=shas["baseline"], head_sha=shas["candidate"],
                   harness_sha=shas["baseline"] if harness_root == roots["baseline"] else shas["candidate"],
                   harness_source="base" if harness_root == roots["baseline"] else "bootstrap",
                   runtime=a.runtime, sdk=SDK)
    (out / "context.json").write_text(json.dumps(context, indent=2), encoding="utf-8")
    env = dict(os.environ, DOTNET_TieredCompilation="0", DOTNET_ReadyToRun="0",
               DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
    def run(name, command, cwd):
        print(name, flush=True)
        with (out / "logs" / (name + ".log")).open("w", encoding="utf-8") as log:
            subprocess.run([str(x) for x in command], cwd=cwd, env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
    binaries = {}
    for version, root in roots.items():
        dest = out / "harness" / version
        shutil.copytree(harness_root / SUITE, dest, ignore=shutil.ignore_patterns("bin", "obj", "compiler", "__pycache__", "BenchmarkDotNet.Artifacts"))
        (dest / "global.json").write_text(json.dumps({"sdk": {"version": SDK, "rollForward": "disable"}}), encoding="utf-8")
        compiler = dest / "compiler"
        for project in ["FastExpressionCompiler", "FastExpressionCompiler.LightExpression"]:
            run("build-" + version + "-" + project, ["dotnet", "build", root / "src" / project, "-c", "Release", "-f", a.runtime,
                "-p:TargetFrameworks=" + a.runtime, "--disable-build-servers", "-m:1", "-o", compiler], dest)
        run("build-harness-" + version, ["dotnet", "build", "Regression.csproj", "-c", "Release", "-f", a.runtime, "--disable-build-servers", "-m:1"], dest)
        binaries[version] = dest / "bin" / "Release" / a.runtime / "Regression.dll"
        run("verify-" + version, ["dotnet", binaries[version], "--verify"], dest)
    manifest = {str(f.relative_to(out)): hashlib.sha256(f.read_bytes()).hexdigest()
                for f in (out / "harness").rglob("*") if f.is_file() and (f.suffix in [".cs", ".csproj", ".props"] or f.name in ["FastExpressionCompiler.dll", "FastExpressionCompiler.LightExpression.dll"])}
    (out / "sources.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    run("environment", ["dotnet", "--info"], out / "harness" / "baseline")
    if a.verify_only:
        return
    for round_index, order in enumerate([["baseline", "candidate"], ["candidate", "baseline"]]):
        for version in order:
            command = ["dotnet", binaries[version], "--filter", a.filter, "--artifacts", out / "runs" / f"{round_index}-{version}"]
            if a.smoke:
                command.append("--smoke")
            run(f"benchmark-{round_index}-{version}", command, out / "harness" / version)


if __name__ == "__main__":
    main()
