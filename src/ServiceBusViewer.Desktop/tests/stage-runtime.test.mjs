import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { runCommand, stageRuntime } from '../scripts/stage-runtime.mjs';

const fixtures = fileURLToPath(new URL('../../../.agents/.tmp/desktop-stage-tests/', import.meta.url));

async function fixture(t, { failAt, missing, empty } = {}) {
  await mkdir(fixtures, { recursive: true });
  const desktop = await mkdtemp(path.join(fixtures, 'stage-'));
  t.after(() => rm(desktop, { recursive: true, force: true }));
  await mkdir(path.join(desktop, '.runtime/backend'), { recursive: true });
  await writeFile(path.join(desktop, '.runtime/backend/stale'), 'old');
  const calls = [];
  const run = async (command, args, options) => {
    calls.push({ command, args, options });
    if (calls.length === failAt) throw new Error('controlled build failure');
    const output = command === 'dotnet' ? args[args.indexOf('-o') + 1] : args.includes('--outDir') ? args[args.indexOf('--outDir') + 1] : null;
    if (!output) return;
    await mkdir(output, { recursive: true });
    for (const file of command === 'dotnet' ? ['ServiceBusViewer.exe', 'coreclr.dll', 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json'] : ['index.html']) {
      if (file !== missing) await writeFile(path.join(output, file), file === empty ? '' : file === 'index.html' ? '<html>/api</html>' : 'fixture');
    }
  };
  return { desktop, calls, run };
}

test('publishes complete native backend and SPA only after both builds', async t => {
  const f = await fixture(t);
  const result = await stageRuntime({ ...f, platform: 'win32', arch: 'x64' });
  assert.equal(result.rid, 'win-x64');
  assert.equal(await readFile(path.join(result.backend, 'wwwroot/index.html'), 'utf8'), '<html>/api</html>');
  assert.equal(await stat(path.join(result.backend, 'stale')).catch(() => null), null);
  assert.ok(f.calls.every(call => call.options.env.VITE_API_BASE_PATH === '/api'));
  const publish = f.calls.find(call => call.command === 'dotnet');
  assert.ok(publish.args.includes('win-x64'));
  assert.ok(publish.args.includes('-p:UseAppHost=true'));
  assert.deepEqual(publish.args.slice(publish.args.indexOf('--self-contained'), publish.args.indexOf('--self-contained') + 2), ['--self-contained', 'true']);
});

for (const failAt of [1, 2, 3, 4]) {
  test(`build failure ${failAt} invalidates old output and cleans staging`, async t => {
    const f = await fixture(t, { failAt });
    await assert.rejects(stageRuntime({ ...f, platform: 'win32', arch: 'x64' }), /controlled build failure/);
    for (const entry of ['backend', 'stage', '.stage-lock']) assert.equal(await stat(path.join(f.desktop, '.runtime', entry)).catch(() => null), null);
  });
}

for (const missing of ['index.html', 'ServiceBusViewer.exe', 'coreclr.dll', 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json']) {
  test(`missing ${missing} prevents accepting output`, async t => {
    const f = await fixture(t, { missing });
    await assert.rejects(stageRuntime({ ...f, platform: 'win32', arch: 'x64' }), /Required staged output/);
    assert.equal(await stat(path.join(f.desktop, '.runtime/backend')).catch(() => null), null);
  });
}

test('unsupported target fails before any build and invalidates old output', async t => {
  const f = await fixture(t);
  await assert.rejects(stageRuntime({ ...f, platform: 'linux', arch: 'ia32' }), /Unsupported/);
  assert.equal(f.calls.length, 0);
  assert.equal(await stat(path.join(f.desktop, '.runtime/backend')).catch(() => null), null);
});

test('subprocess nonzero exit is propagated', async () => {
  await assert.rejects(runCommand(process.execPath, ['-e', 'process.exit(9)'], {}), /exit 9/);
});

test('empty native executable is rejected', async t => {
  const f = await fixture(t, { empty: 'ServiceBusViewer.exe' });
  await assert.rejects(stageRuntime({ ...f, platform: 'win32', arch: 'x64' }), /Required staged output/);
  assert.equal(await stat(path.join(f.desktop, '.runtime/backend')).catch(() => null), null);
});

test('subprocess spawn failure is propagated', async () => {
  await assert.rejects(runCommand('servicebusviewer-nonexistent-build-tool', [], {}), /ENOENT/);
});
