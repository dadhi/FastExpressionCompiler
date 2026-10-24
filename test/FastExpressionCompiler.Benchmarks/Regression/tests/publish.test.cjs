const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const {prepare, post} = require('../publish.cjs');
const head='b'.repeat(40), base='a'.repeat(40);
const header=(h,run=100,attempt=1)=>`<!-- fec-performance:v1 head:${h} run:${run} attempt:${attempt} -->`;
const bot={login:'github-actions[bot]',type:'Bot'};
async function fixture(t) {
  const directory=await fs.mkdtemp(path.join(os.tmpdir(),'fec-publisher-'));
  t.after(()=>fs.rm(directory,{recursive:true,force:true}));
  const calls=[];
  const pr={number:7,state:'open',head:{sha:head},base:{ref:'master',sha:base,repo:{full_name:'dadhi/FastExpressionCompiler'}}};
  const run={id:101,run_attempt:1,event:'pull_request',status:'completed',path:'.github/workflows/performance.yml',repository:{full_name:'dadhi/FastExpressionCompiler'},head_sha:head,html_url:'https://github.com/dadhi/FastExpressionCompiler/actions/runs/101',pull_requests:[]};
  const comments=[];
  const github={rest:{actions:{getWorkflowRun:async()=>({data:run}),listWorkflowRunArtifacts:'artifacts'},repos:{listPullRequestsAssociatedWithCommit:'linked',compareCommitsWithBasehead:async()=>({data:{status:'diverged'}})},pulls:{get:async()=>({data:pr})},issues:{listComments:'comments',createComment:async x=>{calls.push(['create',x]);return{data:{id:99,html_url:'https://github.com/report/99'}}},updateComment:async x=>{calls.push(['update',x]);return{data:{id:x.comment_id,html_url:'https://github.com/report/'+x.comment_id}}}}},paginate:async endpoint=>endpoint==='linked'?[pr]:endpoint==='comments'?comments:[]};
  const context={repo:{owner:'dadhi',repo:'FastExpressionCompiler'},payload:{workflow_run:{id:101,run_attempt:1}}};
  const core={setOutput:(...x)=>calls.push(['output',...x])};
  const input={repository:'dadhi/FastExpressionCompiler',number:7,head_sha:head,run_id:101,attempt:1,run_url:run.html_url,reported_base_sha:base,body:'safe generated report'};
  await fs.writeFile(path.join(directory,'comment.json'),JSON.stringify(input));
  return {directory,github,context,core,calls,pr,run,comments};
}
test('fork run with empty pull_requests resolves through authenticated commit association',async t=>{
  const f=await fixture(t);await prepare(f);assert.equal(f.calls[0][0],'output');assert.equal(JSON.parse(await fs.readFile(path.join(f.directory,'metadata.json'))).number,7);
});
test('stale head ignored before artifact download and before posting',async t=>{
  const f=await fixture(t);f.pr.head.sha='c'.repeat(40);await prepare(f);await post(f);assert.equal(f.calls.length,0);
});
test('wrong workflow and old attempt ignored',async t=>{
  const f=await fixture(t);f.run.path='.github/workflows/other.yml';await prepare(f);assert.equal(f.calls.length,0);
  f.run.path='.github/workflows/performance.yml';f.run.run_attempt=2;await prepare(f);assert.equal(f.calls.length,0);
});
test('new commit creates a new report and collapses the prior report',async t=>{
  const f=await fixture(t);f.comments.push({id:1,user:bot,body:header('c'.repeat(40))+'\nold'});await post(f);
  assert.equal(f.calls[0][0],'create');assert.equal(f.calls[1][0],'update');assert.match(f.calls[1][1].body,/Outdated/);assert.match(f.calls[1][1].body,/<details>/);
});
test('same-head rerun updates its report instead of duplicating',async t=>{
  const f=await fixture(t);f.comments.push({id:2,user:bot,body:header(head)+'\nold'});await post(f);assert.equal(f.calls.length,1);assert.equal(f.calls[0][0],'update');assert.equal(f.calls[0][1].comment_id,2);
});
test('late completion cannot replace a newer run',async t=>{
  const f=await fixture(t);f.comments.push({id:2,user:bot,body:header(head,102)+'\nnewer'});await post(f);assert.equal(f.calls.length,0);
});
test('unverified baseline produces an inconclusive comment',async t=>{
  const f=await fixture(t);f.pr.base.sha='c'.repeat(40);await post(f);assert.match(f.calls[0][1].body,/inconclusive/);assert.doesNotMatch(f.calls[0][1].body,/safe generated report/);
});
