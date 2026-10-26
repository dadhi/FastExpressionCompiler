"""Local-only, pinned-runtime experiment. Does not modify installed runtimes, commit, push or publish."""
import hashlib
import json
import os
import pathlib
import re
import subprocess

root = pathlib.Path(__file__).resolve().parent
repo = root.parents[1]
artifacts = root / "artifacts"
logs = artifacts / "logs"
logs.mkdir(parents=True, exist_ok=True)
results = []
child_environment = os.environ.copy()


def run(label, arguments, expected=0, contains=None):
    completed = subprocess.run(arguments, cwd=root, env=child_environment, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    output = completed.stdout.decode("utf-8", errors="replace")
    (logs / (label + ".log")).write_text(output, encoding="utf-8")
    ok = (completed.returncode == 0 if expected == 0 else completed.returncode != 0)
    if contains is not None:
        ok = ok and contains in output
    results.append({"label": label, "arguments": arguments, "exit_code": completed.returncode,
                    "expected": "success" if expected == 0 else "failure", "passed": ok})
    print(label + ": " + ("PASS" if ok else "FAIL"), flush=True)
    if not ok:
        print(output[-6000:])
        raise RuntimeError(label)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


version = subprocess.check_output(["dotnet", "--version"], text=True).strip()
current = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
installed = subprocess.check_output(["dotnet", "--list-runtimes"], text=True)
paths = {v: pathlib.Path(p) / v / "System.Private.CoreLib.dll"
         for v, p in re.findall(r"Microsoft.NETCore.App (\S+) \[([^\]]+)\]", installed)}
selected = {"net8.0": "8.0.28", "net10.0": "10.0.9"}

# Keep the real FEC source untouched. Transform only a throwaway compilation snapshot.
source = (repo / "src/FastExpressionCompiler/FastExpressionCompiler.cs").read_text(encoding="utf-8")
old = "#if NET10_0_OR_GREATER\n                // In .NET 10+, use UnsafeAccessorType"
assert source.count(old) == 1
source = source.replace(old, "#if NET8_0_OR_GREATER\n                // Local prototype: direct typed access through a metadata facade", 1)
old = "GetMLocalSignature(il).AddArgument(t, false);\n                    return PostInc(ref GetMLocalCount(il));"
assert source.count(old) == 1
source = source.replace(old, "((System.Reflection.Emit.RuntimeILGenerator)il).m_localSignature.AddArgument(t, false);\n                    return PostInc(ref ((System.Reflection.Emit.RuntimeILGenerator)il).m_localCount);", 1)
(artifacts / "Compiler.Direct.cs").write_bytes(source.encode("utf-8"))

run("generator-build", ["dotnet", "build", "Generator/Generator.csproj", "-c", "Release"])
runtime_hashes = {}
try:
    for tfm, runtime in selected.items():
        corelib = paths[runtime]
        child_environment["EXPECTED_CORELIB"] = str(corelib)
        original_hash = digest(corelib)
        runtime_hashes[runtime] = {"implementation": str(corelib), "sha256_before": original_hash}
        private = artifacts / tfm / "private" / "System.Private.CoreLib.dll"
        internal = artifacts / tfm / "internal" / "System.Private.CoreLib.dll"
        generator = ["dotnet", str(root / "Generator/bin/Release/net10.0/Generator.dll")]
        run(tfm + "-generate-private", generator + [str(corelib), str(private), "--private"])
        run(tfm + "-verify-private", generator + ["--verify", str(corelib), str(private), "--private"])
        run(tfm + "-generate-internal", generator + [str(corelib), str(internal)])
        consumer = ["dotnet", "run", "--project", "Consumer/Consumer.csproj", "-c", "Release", "-f", tfm,
                    "-p:RuntimeFrameworkVersion=" + runtime]
        run(tfm + "-consumer", consumer, contains="result=42")
        run(tfm + "-task", consumer + ["-p:UseGeneratorTask=true", "-p:RuntimeCoreLib=" + str(corelib)], contains="result=42")
        run(tfm + "-private-disabled", consumer + ["-p:FacadeDirectory=" + str(internal.parent)], expected=1, contains="error CS")
        run(tfm + "-permission-disabled", consumer + ["-p:GrantPrivateAccess=false"], expected=1, contains="TypeAccessException")
        run(tfm + "-missing-member", consumer + ["-p:MissingMemberProbe=true"], expected=1, contains="CS1061")
        run(tfm + "-wrong-signature", consumer + ["-p:WrongSignatureProbe=true"], expected=1, contains="CS0029")
        for light in (False, True):
            run(tfm + ("-light" if light else "-standard"),
                ["dotnet", "run", "--project", "FecSmoke/FecSmoke.csproj", "-c", "Release", "-f", tfm,
                 "-p:RuntimeFrameworkVersion=" + runtime, "-p:Light=" + str(light).lower()], contains="runtime contracts passed")
        # Finish with a successful consumer output, not a deliberately broken negative-test build.
        run(tfm + "-consumer-final", consumer, contains="result=42")
        runtime_hashes[runtime]["sha256_after"] = digest(corelib)
        assert runtime_hashes[runtime]["sha256_after"] == original_hash, "Installed runtime changed"
        assert not (root / "Consumer/bin/Release" / tfm / "System.Private.CoreLib.dll").exists(), "Facade leaked into output"
finally:
    (root / "results.json").write_bytes((json.dumps({"sdk": version, "fec_base": current,
        "upstream_generator": "882b30bf82fbb72f3bee8878ad9fd31f5c93c875", "runtimes": runtime_hashes,
        "results": results}, indent=2) + "\n").encode("utf-8"))
print(f"All {len(results)} checks passed; installed runtime hashes unchanged; no runtime DLL copied to consumer outputs")
