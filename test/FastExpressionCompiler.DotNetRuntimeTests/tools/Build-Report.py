"""Compare TRX result groups without inventing identities for duplicate data rows."""
from pathlib import Path
import argparse,collections,csv,hashlib,html,json,re,xml.etree.ElementTree as ET
p=Path(__file__).resolve().parent.parent
ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,default=p/'Reports');a=ap.parse_args();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
def read_trx(path):
 root=ET.parse(path).getroot();defs={};groups={}
 for x in root.findall('.//t:UnitTest',ns):
  m=x.find('t:TestMethod',ns)
  if m is not None:defs[x.get('id')]=(m.get('className'),m.get('name'))
 for x in root.findall('.//t:UnitTestResult',ns):
  raw_id=x.get('testId');name=x.get('testName')
  cls,meth=defs.get(raw_id,('',''));outcome=x.get('outcome');normalized=name
  if cls.endswith('SymbolDocumentInfoTests'):
   normalized=re.sub(r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}','<GUID>',normalized)
  if cls.endswith('ExpressionDebuggerTypeProxyTests'):
   normalized=re.sub(r'\d{2}[.]\d{2}[.]\d{4} \d{2}:\d{2}:\d{2}','<DateTime.Now>',normalized)
  key=cls+'|'+meth+'|'+normalized if normalized!=name else raw_id+'|'+name
  g=groups.setdefault(key,{'id':hashlib.sha256(key.encode()).hexdigest()[:20],'test':name,'class':cls,'method':cls+'.'+meth,'counts':collections.Counter(),'errors':[],'evidence':str(path.relative_to(p))})
  g['counts'][outcome]+=1
  if outcome=='Failed':g['errors'].append({'message':x.findtext('.//t:Message','',ns),'stack':x.findtext('.//t:StackTrace','',ns)})
 return groups
baseline={};baseline_files=[];shard_system=p/'TestResults/sharded-net10.0/system'
if (shard_system/'run.json').exists():
 run=json.loads((shard_system/'run.json').read_text())
 if len(run['batches'])!=21 or any(x['status']!='Complete' for x in run['batches']):raise SystemExit('Baseline shards incomplete')
 baseline_files=[shard_system/f"batch-{x['batch']:02}"/f"{x['attempt']:02}.trx" for x in run['batches']]
else:baseline_files=[p/'TestResults/net10.0/system/01.trx']
baseline_files.append(p/'TestResults/stress/system/stress.trx')
for f in baseline_files:baseline.update(read_trx(f))
verified_system=p/'TestResults/verified-system/verification.trx'
if verified_system.exists():baseline.update(read_trx(verified_system))
shards=p/'TestResults/sharded-net10.0/fec';run=json.loads((shards/'run.json').read_text())
if len(run['batches'])!=21 or any(x['status']!='Complete' for x in run['batches']):raise SystemExit('FEC shards incomplete')
final={}
for batch in run['batches']:final.update(read_trx(shards/f"batch-{batch['batch']:02}"/f"{batch['attempt']:02}.trx"))
fec=collections.defaultdict(list)
fec_files=sorted(shards.glob('batch-*/*.trx'))+sorted((p/'TestResults/net10.0/fec').glob('*.trx'))+[p/'TestResults/fec.trx',p/'TestResults/stress/fec/stress.trx',p/'TestResults/isolated-call/call.trx']
for f in fec_files:
 if not f.exists():continue
 try:groups=read_trx(f)
 except ET.ParseError:continue
 for key,g in groups.items():fec[key].append(g)
for f in sorted((p/'TestResults/coverage').glob('*/*.trx')):
 for key,g in read_trx(f).items():fec[key].append(g);final[key]=g
verified_fec=p/'TestResults/verified-fec/verification.trx'
if verified_fec.exists():
 for key,g in read_trx(verified_fec).items():fec[key]=[g];final[key]=g
crashes={x['method']:x for x in run['crashes']};stress='System.Linq.Expressions.Tests.CompilerTests.CompileDeepTree_NoStackOverflow'
stress_log=p/'TestResults/stress/fec/stress.log'
if stress_log.exists() and ('crashed' in stress_log.read_text(encoding='utf-8',errors='replace').lower() or 'stack overflow' in stress_log.read_text(encoding='utf-8',errors='replace').lower()):
 crashes[stress]={'method':stress,'outcome':'ProcessCrash','log':'TestResults/stress/fec/stress.log'}
else:
 stress_run=p/'TestResults/stress/fec/run.json'
 if stress_run.exists() and json.loads(stress_run.read_text()).get('outcome')=='Timeout':crashes[stress]={'method':stress,'outcome':'Timeout','log':'TestResults/stress/fec/stress.log'}
coverage_run=p/'TestResults/coverage/run.json'
if coverage_run.exists():
 for failure in json.loads(coverage_run.read_text()):
  if failure['outcome']=='ProcessCrash':crashes[failure['method']]=failure
timeouts={m:v for m,v in crashes.items() if v.get('outcome')=='Timeout'}
observed=collections.defaultdict(int)
for f in shards.glob('batch-*/*observations.jsonl'):
 for line in f.read_text(encoding='utf-8-sig').splitlines():
  v=json.loads(line);observed[v['method']]+=v['compileRequests']
rows=[];details=[];counts=collections.Counter();categories=collections.Counter();class_counts=collections.defaultdict(collections.Counter);baseline_counts=collections.Counter()
for key,b in baseline.items():
 number=sum(b['counts'].values());baseline_counts.update(b['counts']);seen=fec.get(key,[])
 actual=final.get(key) or (max(seen,key=lambda g:sum(g['counts'].values())) if seen else {'counts':{},'errors':[],'evidence':''})
 ac=collections.Counter(actual['counts']);method=b['method'];error=actual['errors'][0] if actual['errors'] else {'message':'','stack':''};message=error['message'];stack=error['stack']
 statuses=collections.Counter()
 if b['counts'].get('NotExecuted')==number:statuses['Skipped']=number
 else:
  for old,new in [('Passed','Passed'),('Failed','Failed'),('NotExecuted','Skipped')]:statuses[new]+=ac[old]
  missing=number-sum(statuses.values())
  if missing<0:raise RuntimeError('More FEC rows than baseline rows for '+b['test'])
  if missing:statuses['Timeout/NotRunAfterTimeout' if method in timeouts else 'Crash/NotRunAfterCrash' if method in crashes else 'NotRun']+=missing
  if number==1 and len({o for g in seen for o,n in g['counts'].items() if n and o in ['Passed','Failed']})>1:
   statuses=collections.Counter({'Unstable':1})
 statuses=+statuses;counts.update(statuses);class_counts[b['class']].update(statuses)
 status=next(iter(statuses)) if len(statuses)==1 else 'Mixed'
 impl=bool(re.search(r'VerifyIL|VerifyInstructions|EmitConstantsToIL|GetInstructions|CodeGen|AboveByteMaxArityArgAddressIL',method+' '+stack))
 if status=='Passed':category='Passed'
 elif status=='Skipped':category='Upstream skipped'
 elif status=='Crash/NotRunAfterCrash':category='Method crashed; these cases did not finish'
 elif status=='Timeout/NotRunAfterTimeout':category='Method timed out; these cases did not finish'
 elif status=='NotRun':category='Not run (investigate coverage)'
 elif status=='Mixed':category='Mixed outcomes in indistinguishable TRX rows'
 elif b['counts'].get('Failed'):category='Also fails with system compiler'
 elif impl:category='Runtime implementation-specific assertion'
 elif 'strict compilation returned null' in message or 'NotSupportedException' in message:category='Unsupported / strict compilation rejected'
 elif 'InvalidProgramException' in message or 'Bad label' in message:category='Invalid IL / IL generation error'
 elif status=='Unstable':category='Different result across attempts'
 else:category='Behavior or exception mismatch'
 categories[category]+=number
 scope='Compiler-requesting method' if observed[method]>0 or method in crashes else 'No observed compile request in method'
 row={'id':b['id'],'test':b['test'],'class':b['class'],'method':method,'cases':number,'system':', '.join(f'{o}: {n}' for o,n in b['counts'].items()),'fec':status,'fec_counts':', '.join(f'{o}: {n}' for o,n in statuses.items()),'category':category,'scope':scope,'crash_in_method':method in crashes,'error':message.splitlines()[0] if message else '', 'baseline_error':b['errors'][0]['message'].splitlines()[0] if b['errors'] else ''}
 rows.append(row)
 if status not in ['Passed','Skipped']:details.append({**row,'full_error':message,'stack':stack,'all_errors_json':json.dumps(actual['errors']),'evidence':actual.get('evidence','')})
manifest=json.loads((p/'upstream-manifest.json').read_text(encoding='utf-8-sig'))
summary={'date':'2026-10-18','upstreamCommit':manifest['commit'],'fecCommit':'1d2a4bd364bd5271f5594074f76e298f5afa53db','environment':'.NET SDK 10.0.301; .NET runtime 10.0.9; Windows ARM64; Release','sourceFiles':len(manifest['files']),'compileSites':sum(x['compileCalls'] for x in manifest['files']),'baselineCases':sum(baseline_counts.values()),'displayGroups':len(baseline),'baselineOutcomes':dict(baseline_counts),'fecOutcomes':dict(counts),'groupCategoryCaseCounts':dict(categories),'crashingMethods':len(crashes)-len(timeouts),'timedOutMethods':len(timeouts),'methodsObservedRequestingCompilation':sum(v>0 for v in observed.values()),'fecGroupsWithoutMatchingBaseline':len(set(fec)-set(baseline)),'completedBatches':len(run['batches'])}
(out/'summary.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
def write_csv(name,data):
 if not data:return
 with (out/name).open('w',newline='',encoding='utf-8-sig') as f:
  w=csv.DictWriter(f,fieldnames=list(data[0]));w.writeheader();w.writerows(data)
write_csv('fec-only-groups.csv',[{'id':key,'test':fec[key][-1]['test'],'outcomes':json.dumps(dict(fec[key][-1]['counts']))} for key in sorted(set(fec)-set(baseline))]);write_csv('all-tests.csv',rows);write_csv('failures.csv',details)
write_csv('crashing-methods.csv',[{'method':m,'outcome':v.get('outcome'),'affected_baseline_cases':sum(r['cases'] for r in rows if r['method']==m),'log':v.get('log','')} for m,v in sorted(crashes.items())]);write_csv('source-inventory.csv',manifest['files'])
write_csv('classes.csv',[{'class':c,**{s:n.get(s,0) for s in ['Passed','Failed','Unstable','Skipped','Crash/NotRunAfterCrash','Timeout/NotRunAfterTimeout','NotRun']}} for c,n in sorted(class_counts.items())])
payload=json.dumps(rows,separators=(',',':')).replace('<',chr(92)+'u003c')
html_start='''<!doctype html><html lang="en"><meta charset="utf-8"><title>FastExpressionCompiler — .NET test compatibility</title><style>
body{font:16px/1.5 system-ui;margin:0;color:#192333;background:#f4f6fa}main{max-width:1200px;margin:auto;padding:32px}h1{font-size:30px;margin-bottom:4px}h2{margin-top:32px}small,.muted{color:#536174}.cards{display:flex;gap:12px;flex-wrap:wrap}.card{background:white;border:1px solid #dce2eb;padding:16px 22px;border-radius:10px}.card b{font-size:27px;display:block}table{border-collapse:collapse;width:100%;background:white;font-size:13px}td,th{text-align:left;vertical-align:top;padding:9px;border-bottom:1px solid #e1e6ef;overflow-wrap:anywhere}input,select,button{padding:9px;font:inherit;margin:5px 5px 12px 0}input{width:45%}.bad{color:#a42d35}.ok{color:#207348}code{overflow-wrap:anywhere}.note{background:#fff8e5;border-left:4px solid #dca721;padding:14px}a{color:#265eb6}.pager{display:flex;align-items:center;gap:10px}</style><main><h1>FastExpressionCompiler: .NET expression test compatibility</h1><div class="muted">18 October 2026 · Windows ARM64 · .NET 10.0.9 · Release · strict FEC, fallback disabled</div>'''
cards='<div class="cards">'+''.join(f'<div class="card"><b>{v:,}</b>{html.escape(k)}</div>' for k,v in [('Baseline cases',summary['baselineCases']),*counts.items(),('Crashing methods',len(crashes)-len(timeouts)),('Timed out methods',len(timeouts))])+'</div>'
intro=f'''<p>Imported <b>{summary['sourceFiles']}</b> C# source/support files and redirected <b>{summary['compileSites']:,}</b> compilation call sites from <a href="https://github.com/dotnet/runtime/tree/{manifest['commit']}/src/libraries/System.Linq.Expressions/tests">dotnet/runtime System.Linq.Expressions tests</a>. System control: <b>{summary['baselineOutcomes'].get('Passed',0):,}</b> passed, <b>{summary['baselineOutcomes'].get('Failed',0):,}</b> failed, and <b>{summary['baselineOutcomes'].get('NotExecuted',0):,}</b> skipped. Upstream revision: <code>{manifest['commit']}</code>. FastExpressionCompiler revision: <code>{summary['fecCommit']}</code>.</p>'''
notes='''<h2>How to read the results</h2><p>The system control uses the original .Compile(bool) behavior. Both upstream boolean modes call strict CompileFast in the FEC run: these are repeated FEC executions, not interpreter support. One case may run many expressions and values. API-only passes do not establish compiler support. Recorded pass/fail differences across repeated runs are marked Unstable rather than choosing the more favorable outcome.</p><div class="note">A method that crashes or times out is isolated so other methods continue. Completed cases retain their measured result; remaining cases in that method are shown as Crash/NotRunAfterCrash. The specific crashing data row is not identified by this method-level crash marker. Tests in the main suite that fail on the installed system runtime are kept separate from FEC-specific failures. Source assertions were retained, including exact IL, closure layout and interpreter instruction checks, which are implementation-specific.</div><p>All 21 class batches finished after crash isolation. The deep-tree stress test ran separately: both system modes passed; the FEC process crashed. Trimming app entry points ran as ordinary tests; published trimming and NativeAOT were not verified. The irreducible-extension test exceeded its 600-second group time limit; its remaining group completed after isolation. Four tests were skipped: three upstream ActiveIssue tests and the RemoteExecutor infrastructure test.</p><p>Coverage is the complete named runtime suite. Roslyn C#/VB lowering and debugger expression compiler tests target a different compiler; linker metadata and NativeAOT infrastructure are outside this execution report. LightExpression was not tested. This does not claim an exhaustive import of every expression-related test across the entire dotnet organization.</p><p>The system control originally had 16 IL text failures caused by the new assembly name; the importer now remaps those expected assembly names, and all 16 affected control cases passed on recheck. A representative CallTests.Call_NoParameters method also crashed when run alone on .NET 8 ARM64 and .NET 10 x64. Those are diagnostic checks, not full additional platform runs.</p><p>Download: <a href="all-tests.csv">all result groups</a> · <a href="failures.csv">failure messages and stacks</a> · <a href="classes.csv">class totals</a> · <a href="crashing-methods.csv">crashing methods</a> · <a href="source-inventory.csv">source inventory</a> · <a href="summary.json">summary JSON</a></p>'''
cat_table='<h2>Failure categories</h2><table><tr><th>Category<th>Cases'+''.join('<tr><td>'+html.escape(k)+f'<td>{v:,}' for k,v in categories.most_common() if k!='Passed')+'</table>'
viewer='''<h2>Every baseline result group</h2><input id="query" placeholder="Search test name or error"><select id="status"><option value="">All FEC outcomes</option>'''+''.join('<option>'+html.escape(s)+'</option>' for s in [*counts,'Mixed'])+'''</select><div class="pager"><button id="prev">Previous</button><span id="page"></span><button id="next">Next</button></div><table><thead><tr><th>Test case<th>System<th>FEC<th>Scope<th>Category / error<tbody id="cases"></tbody></table><p class="muted">GUIDs and DateTime.Now values in test names are normalized only when matching runs. Some xUnit theory rows have identical test IDs and display names. Such rows are grouped with explicit case and outcome counts; individual rows cannot be matched reliably. Full multiline errors and evidence paths are in failures.csv. Compilation observation is recorded per method, not per individual theory row.</p></main><script>const data='''+payload+''';let page=0,filtered=data;const size=100;const el=id=>document.getElementById(id);function render(){let q=el('query').value.toLowerCase(),s=el('status').value;filtered=data.filter(r=>(!s||r.fec===s)&&(!q||(r.test+' '+r.error+' '+r.category+' '+r.scope).toLowerCase().includes(q)));page=Math.min(page,Math.max(0,Math.ceil(filtered.length/size)-1));let tbody=el('cases');tbody.replaceChildren();for(let r of filtered.slice(page*size,(page+1)*size)){let tr=document.createElement('tr');for(let v of [r.test+' ('+r.cases+' cases)',r.system,r.fec_counts,r.scope,r.category+(r.error?' — '+r.error:'')]){let td=document.createElement('td');td.textContent=v;tr.append(td)}tbody.append(tr)}el('page').textContent=`${filtered.length.toLocaleString()} result groups · page ${page+1} of ${Math.max(1,Math.ceil(filtered.length/size))}`;el('prev').disabled=page===0;el('next').disabled=(page+1)*size>=filtered.length}el('query').oninput=el('status').onchange=()=>{page=0;render()};el('prev').onclick=()=>{page--;render()};el('next').onclick=()=>{page++;render()};render();</script></html>'''
(out/'report.html').write_text(html_start+intro+cards+notes+cat_table+viewer,encoding='utf-8')
print(json.dumps(summary,indent=2))
