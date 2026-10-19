"""Run the imported suite in recoverable groups; keep process failures visible."""
from pathlib import Path
import argparse
import concurrent.futures
import json
import os
import subprocess
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent
STRESS = 'System.Linq.Expressions.Tests.CompilerTests.CompileDeepTree_NoStackOverflow'
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--mode', choices=['fec', 'system'], default='fec')
parser.add_argument('--jobs', type=int, default=4)
parser.add_argument('--batch', type=int, nargs='+', help='Retry only these batch numbers')
parser.add_argument('--assembly', type=Path, default=ROOT/'bin/Release/net10.0/FastExpressionCompiler.DotNetRuntimeTests.dll')
parser.add_argument('--case-timeout', type=int, default=120, help='Seconds without advancing to another method')
args = parser.parse_args()
if not args.assembly.is_file():
    parser.error('Build the project first; assembly not found: ' + str(args.assembly))
output = ROOT/'TestResults/sharded-net10.0'/args.mode
output.mkdir(parents=True, exist_ok=True)
inventory = json.loads((ROOT/'test-inventory.json').read_text())
classes = inventory['classes']
batches = [(index//10+1, classes[index:index+10]) for index in range(0, len(classes), 10)]
crash_file = output/'crashes.json'
crashes = json.loads(crash_file.read_text()) if crash_file.exists() else []
lock = threading.Lock()

def record_failure(method, batch, attempt, outcome, folder):
    failure = {'method': method, 'batch': batch, 'attempt': attempt,
               'outcome': outcome, 'log': str((folder/f'{attempt:02}.log').relative_to(output))}
    with lock:
        crashes.append(failure)
        crash_file.write_text(json.dumps(crashes, indent=2), encoding='utf-8')
    print(outcome + ': ' + method, flush=True)

def stop_process_tree(process):
    if os.name == 'nt':
        subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    else:
        process.kill()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()

def execute(command, env, log_file, marker):
    last_method = None
    advanced_at = time.monotonic()
    with log_file.open('w', encoding='utf-8') as log:
        process = subprocess.Popen(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT)
        while process.poll() is None:
            time.sleep(0.5)
            try:
                method = marker.read_text().strip() if marker.exists() else None
            except (OSError, UnicodeError):
                continue
            if method != last_method:
                last_method = method
                advanced_at = time.monotonic()
            if time.monotonic() - advanced_at > args.case_timeout:
                stop_process_tree(process)
                return 'Timeout', last_method
    return 'Exited', last_method

def run_batch(number, selected_classes):
    folder = output/f'batch-{number:02}'
    folder.mkdir(exist_ok=True)
    excluded = {x['method'] for x in crashes} | {STRESS}
    marker = folder/'current.txt'
    env = os.environ.copy()
    env.update(FEC_TEST_COMPILER=args.mode, DOTNET_CLI_UI_LANGUAGE='en', VSLANG='1033',
               FEC_TEST_CURRENT=str(marker))
    existing_attempts = [int(f.stem) for f in folder.glob('*.trx') if f.stem.isdigit()]
    first_attempt = max(existing_attempts, default=0) + 1
    for attempt in range(first_attempt, first_attempt + 250):
        observations = folder/f'{attempt:02}-observations.jsonl'
        env['FEC_TEST_OBSERVATIONS'] = str(observations)
        marker.unlink(missing_ok=True)
        include = '(' + '|'.join('FullyQualifiedName~'+c+'.' for c in selected_classes) + ')'
        test_filter = include + '&' + '&'.join('FullyQualifiedName!='+m for m in sorted(excluded))
        command = ['dotnet', 'test', str(args.assembly.resolve()), '--filter', test_filter,
                   '--results-directory', str(folder), '--logger', f'trx;LogFileName={attempt:02}.trx',
                   '--logger', 'console;verbosity=quiet']
        outcome, method = execute(command, env, folder/f'{attempt:02}.log', marker)
        if outcome == 'Timeout':
            if not method:
                return {'batch': number, 'status': 'InfrastructureTimeout'}
            record_failure(method, number, attempt, 'Timeout', folder)
            excluded.add(method)
            continue
        trx = folder/f'{attempt:02}.trx'
        if not trx.exists():
            return {'batch': number, 'status': 'MissingResults', 'log': str(folder/f'{attempt:02}.log')}
        summary = ET.parse(trx).getroot().find('t:ResultSummary', NS)
        log = (folder/f'{attempt:02}.log').read_text(encoding='utf-8', errors='replace').lower()
        if summary.get('outcome') != 'Aborted' and 'test run aborted' not in log:
            result = {'batch': number, 'status': 'Complete', 'attempt': attempt,
                      'classes': selected_classes, 'excluded': sorted(excluded),
                      'counters': summary.find('t:Counters', NS).attrib}
            (folder/'run.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
            return result
        if not method or method in excluded:
            return {'batch': number, 'status': 'RecoveryFailed', 'method': method}
        record_failure(method, number, attempt, 'ProcessCrash', folder)
        excluded.add(method)
    return {'batch': number, 'status': 'RecoveryLimit'}

results = {}
work = []
for number, selected_classes in batches:
    saved = output/f'batch-{number:02}/run.json'
    if saved.exists() and (not args.batch or number not in args.batch):
        result = json.loads(saved.read_text())
        if result['status'] == 'Complete':
            results[number] = result
            continue
    if not args.batch or number in args.batch:
        work.append((number, selected_classes))
with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
    futures = [pool.submit(run_batch, number, selected_classes) for number, selected_classes in work]
    for future in concurrent.futures.as_completed(futures):
        result = future.result()
        results[result['batch']] = result
        print(f"Batch {result['batch']}/{len(batches)}: {result['status']}", flush=True)
        (output/'run.json').write_text(json.dumps({'mode': args.mode,
            'batches': list(results.values()), 'crashes': crashes}, indent=2), encoding='utf-8')

# The unusually deep expression tree always runs separately.
stress_dir = ROOT/'TestResults/stress'/args.mode
stress_dir.mkdir(parents=True, exist_ok=True)
env = os.environ.copy()
env.update(FEC_TEST_COMPILER=args.mode, DOTNET_CLI_UI_LANGUAGE='en',
           FEC_TEST_CURRENT=str(stress_dir/'current.txt'),
           FEC_TEST_OBSERVATIONS=str(stress_dir/'observations.jsonl'))
(stress_dir/'observations.jsonl').unlink(missing_ok=True)
command = ['dotnet', 'test', str(args.assembly.resolve()), '--filter', 'FullyQualifiedName~'+STRESS,
           '--results-directory', str(stress_dir), '--logger', 'trx;LogFileName=stress.trx',
           '--logger', 'console;verbosity=quiet']
outcome, method = execute(command, env, stress_dir/'stress.log', stress_dir/'current.txt')
(stress_dir/'run.json').write_text(json.dumps({'mode': args.mode, 'outcome': outcome}, indent=2))
if len(results) != len(batches) or any(r['status'] != 'Complete' for r in results.values()):
    raise SystemExit(2)
