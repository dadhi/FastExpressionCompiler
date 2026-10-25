// Runs only from the default-branch checkout in the privileged workflow.
const fs = require('node:fs/promises');
const path = require('node:path');
const marker = /^<!-- fec-performance:v1 head:([a-f0-9]{40}) run:(\d+) attempt:(\d+) -->/;

async function prepare({github, context, core, directory}) {
  const {owner, repo} = context.repo;
  const event = context.payload.workflow_run;
  const {data: run} = await github.rest.actions.getWorkflowRun({owner, repo, run_id: event.id});
  if (run.event !== 'pull_request' || run.path !== '.github/workflows/performance.yml' || run.status !== 'completed' ||
      run.repository.full_name !== `${owner}/${repo}` || run.run_attempt !== event.run_attempt) return;
  // Fork runs often have an empty pull_requests array; resolve through GitHub, never artifact metadata.
  const linked = await github.paginate(github.rest.repos.listPullRequestsAssociatedWithCommit, {owner, repo, commit_sha: run.head_sha, per_page: 100});
  const matches = linked.filter(pr => pr.state === 'open' && pr.base.ref === 'master' &&
      pr.base.repo.full_name === `${owner}/${repo}` && pr.head.sha === run.head_sha);
  if (matches.length !== 1) return;
  const pr = matches[0];
  await fs.mkdir(directory, {recursive: true});
  const metadata = {repository: `${owner}/${repo}`, number: pr.number, head_sha: run.head_sha,
      run_id: run.id, attempt: run.run_attempt, run_url: run.html_url};
  await fs.writeFile(path.join(directory, 'metadata.json'), JSON.stringify(metadata));
  const artifacts = await github.paginate(github.rest.actions.listWorkflowRunArtifacts, {owner, repo, run_id: run.id, per_page: 100});
  const selected = artifacts.filter(a => !a.expired && a.name === `performance-report-net10.0-${run.run_attempt}`);
  if (selected.length === 1 && selected[0].size_in_bytes <= 1024 * 1024) {
    const result = await github.rest.actions.downloadArtifact({owner, repo, artifact_id: selected[0].id, archive_format: 'zip'});
    const buffer = Buffer.from(result.data);
    if (buffer.length <= 1024 * 1024) await fs.writeFile(path.join(directory, 'performance-report.zip'), buffer);
  }
  core.setOutput('publish', 'true');
}

async function post({github, context, directory}) {
  const {owner, repo} = context.repo;
  const m = JSON.parse(await fs.readFile(path.join(directory, 'comment.json'), 'utf8'));
  const {data: pr} = await github.rest.pulls.get({owner, repo, pull_number: m.number});
  if (pr.state !== 'open' || pr.base.ref !== 'master' || pr.base.repo.full_name !== `${owner}/${repo}` || pr.head.sha !== m.head_sha) return;
  if (m.reported_base_sha && m.reported_base_sha !== pr.base.sha) {
    // A base branch can advance during the run. Allow its authenticated history, not a fork baseline.
    let valid = false;
    try {
      const {data: comparison} = await github.rest.repos.compareCommitsWithBasehead({owner, repo, basehead: `${m.reported_base_sha}...${pr.base.sha}`});
      valid = comparison.status === 'ahead' || comparison.status === 'identical';
    } catch { /* Publish an inconclusive result instead of trusting an unrelated baseline. */ }
    if (!valid) m.body = `### Performance comparison: inconclusive\n\nThe reported baseline could not be verified against master.\n\n[Workflow run](${m.run_url})\n`;
  }
  const comments = await github.paginate(github.rest.issues.listComments, {owner, repo, issue_number: pr.number, per_page: 100});
  const ours = comments.filter(c => c.user?.login === 'github-actions[bot]' && c.user?.type === 'Bot' && marker.test(c.body || ''));
  if (ours.some(c => { const x = c.body.match(marker); return Number(x[2]) > m.run_id || Number(x[2]) === m.run_id && Number(x[3]) > m.attempt; })) return;
  const body = `<!-- fec-performance:v1 head:${m.head_sha} run:${m.run_id} attempt:${m.attempt} -->\n${m.body}`;
  const sameHead = ours.find(c => c.body.match(marker)[1] === m.head_sha && !c.body.includes('<!-- superseded -->'));
  const latest = sameHead
    ? (await github.rest.issues.updateComment({owner, repo, comment_id: sameHead.id, body})).data
    : (await github.rest.issues.createComment({owner, repo, issue_number: pr.number, body})).data;
  for (const old of ours) {
    if (old.id === latest.id || old.body.includes('<!-- superseded -->')) continue;
    const header = old.body.match(marker)[0];
    await github.rest.issues.updateComment({owner, repo, comment_id: old.id,
        body: `${header}\n<!-- superseded -->\n> **Outdated:** superseded by [the newer performance report](${latest.html_url}).\n\n<details><summary>Previous measurements</summary>\n\n${old.body.slice(header.length).trim()}\n\n</details>`});
  }
}
module.exports = {prepare, post};
