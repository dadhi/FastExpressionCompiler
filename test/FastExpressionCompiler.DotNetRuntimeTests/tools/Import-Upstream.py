"""Reimport the pinned runtime test suite from a local dotnet/runtime checkout."""
from pathlib import Path
import argparse,hashlib,json,re,shutil,subprocess
PIN='ec0059c08c077e839c6eb8827c846530a891d7e2'
p=argparse.ArgumentParser();p.add_argument('runtime_checkout',type=Path);a=p.parse_args()
source=a.runtime_checkout.resolve();dest=Path(__file__).resolve().parent.parent
commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=source,text=True).strip()
if commit!=PIN:raise SystemExit('Expected pinned dotnet/runtime revision '+PIN+'; found '+commit)
root=source/'src/libraries/System.Linq.Expressions/tests';files=[]
for f in sorted(root.rglob('*.cs')):
 rel=f.relative_to(root);original=f.read_text(encoding='utf-8-sig');text=original
 if rel.as_posix()=='AssemblyInfo.cs':
  text='using Xunit;\n[assembly: CollectionBehavior(DisableTestParallelization = true)]\n'
 else:
  text=re.sub(r'\.Compile\s*\(','.CompileForFec(',text)
  text=text.replace('"System.Linq.Expressions.Tests")','"FastExpressionCompiler.DotNetRuntimeTests")')
  text=text.replace('[System.Linq.Expressions.Tests]', '[FastExpressionCompiler.DotNetRuntimeTests]')
  text=text.replace('LambdaExpression.CanCompileToIL','System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported').replace('LambdaExpression.CanInterpret','true')
  if rel.as_posix()!='Dynamic/CallInfoTests.cs':text=text.replace('.ArgumentCount','.GetArgumentCountForPort()')
  text=text.replace('var c = f.Target as Closure;','var c = RuntimePrivateAccess.ReadClosure(f.Target);')
  text=text.replace('var closure = (Closure)d.Target;','var closure = RuntimePrivateAccess.ReadClosure(d.Target);')
  if rel.as_posix()=='InterpreterTests.cs':
   text=text.replace('using System.Linq.Expressions.Interpreter;','')
   text=text.replace('typeof(LightLambda)','typeof(Expression).Assembly.GetType("System.Linq.Expressions.Interpreter.LightLambda", throwOnError: true)')
   text=text.replace('var lambda = (LightLambda)thunk.Target;','var lambda = thunk.Target;')
  if rel.as_posix()=='Dynamic/CallSiteCachingTests.cs':
   text=text.replace('CallSiteOps.Get','System.Linq.Expressions.Tests.RuntimeCallSiteOps.Get')
   text=text.replace('typeof(CallSiteOps)','typeof(System.Linq.Expressions.Expression).Assembly.GetType("System.Runtime.CompilerServices.CallSiteOps", true)')
  text=text.replace('[Fact, ActiveIssue("https://github.com/dotnet/runtime/issues/20083")]','[Fact(Skip = "Upstream active issue: https://github.com/dotnet/runtime/issues/20083"), ActiveIssue("https://github.com/dotnet/runtime/issues/20083")]')
  if rel.parts[0]=='TrimmingTests':
   text=text.replace('static int Main(string[] args)','internal static int RunPorted(string[] args)')
   start=text.index('internal class Program');ns='System.Linq.Expressions.Tests.Trimming.'+f.stem
   text=text[:start]+'namespace '+ns+'\n{\n'+text[start:]+'\npublic class EntryPointTests { [Xunit.Fact] public void ReturnsSuccess() => Xunit.Assert.Equal(100, Program.RunPorted(System.Array.Empty<string>())); }\n}\n'
 out=dest/'Ported'/rel;out.parent.mkdir(parents=True,exist_ok=True);out.write_text(text,encoding='utf-8')
 # Normalized text hashes are stable across Git core.autocrlf settings.
 files.append({'path':rel.as_posix(),'upstreamNormalizedSha256':hashlib.sha256(original.encode()).hexdigest(),'portedNormalizedSha256':hashlib.sha256(text.encode()).hexdigest(),'compileCalls':len(re.findall(r'\.Compile\s*\(',original)),'adapted':text!=original})
(dest/'upstream-manifest.json').write_text(json.dumps({'repository':'https://github.com/dotnet/runtime','commit':PIN,'directory':'src/libraries/System.Linq.Expressions/tests','files':files},indent=2)+'\n',encoding='utf-8')
shutil.copyfile(source/'LICENSE.TXT',dest/'UPSTREAM-LICENSE.TXT')
print(f'Imported {len(files)} C# source/support files; {sum(x["compileCalls"] for x in files)} compilation sites.')

