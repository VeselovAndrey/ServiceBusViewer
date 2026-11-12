import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { parseBundleArguments, stageBundleRuntime, validateLinuxExecutable } from '../scripts/package.mjs';
import { stageRuntime } from '../scripts/stage-runtime.mjs';

const fixtures = fileURLToPath(new URL('../../../.agents/.tmp/implementation-user-story/63/cross-linux/tests/', import.meta.url));
async function fixture(t) {
  await mkdir(fixtures, { recursive: true });
  const desktop = await mkdtemp(path.join(fixtures, 'cross with spaces-'));
  t.after(() => rm(desktop, { recursive: true, force: true }));
  return desktop;
}

test('default stays native and Windows may explicitly target Linux', () => {
  assert.deepEqual(parseBundleArguments([], 'win32', 'x64'), { platform: 'win32', arch: 'x64', crossBuild: false });
  assert.deepEqual(parseBundleArguments(['--platform=linux', '--arch=x64'], 'win32', 'x64'), { platform: 'linux', arch: 'x64', crossBuild: true });
  assert.deepEqual(parseBundleArguments(['--arch=arm64', '--platform=linux'], 'win32', 'x64'), { platform: 'linux', arch: 'arm64', crossBuild: true });
  assert.deepEqual(parseBundleArguments([], 'darwin', 'arm64'), { platform: 'darwin', arch: 'arm64', crossBuild: false });
});

for (const args of [['--unknown=linux'], ['--platform', 'linux'], ['--platform=linux', '--platform=linux'], ['--arch=ia32'], ['--platform=darwin'], ['--arch=arm64'], ['--platform=linux=extra']]) {
  test(`rejects unsupported or malformed options: ${args.join(' ')}`, () => assert.throws(() => parseBundleArguments(args, 'win32', 'x64')));
}

test('isolated cross staging remaps sources and preserves the Windows development backend', async t => {
  const desktop = await fixture(t);
  const work = path.join(desktop, '.runtime/package');
  await mkdir(path.join(desktop, '.runtime/backend'), { recursive: true });
  const sentinel = path.join(desktop, '.runtime/backend/ServiceBusViewer.exe');
  await writeFile(sentinel, 'MZ Windows backend');
  const calls = [];
  const run = async (command, args, options) => {
    calls.push({ command, args, options });
    const output = command === 'dotnet' ? args[args.indexOf('-o') + 1] : args.includes('--outDir') ? args[args.indexOf('--outDir') + 1] : null;
    if (!output) return;
    assert.ok(output.startsWith(work + path.sep));
    await mkdir(output, { recursive: true });
    for (const file of command === 'dotnet' ? ['ServiceBusViewer', 'libcoreclr.so', 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json'] : ['index.html']) await writeFile(path.join(output, file), 'compiled fixture');
  };
  const result = await stageBundleRuntime({ desktop, work, platform: 'linux', arch: 'x64', crossBuild: true, stage: stageRuntime, run });
  assert.equal(result.rid, 'linux-x64');
  assert.equal(await readFile(sentinel, 'utf8'), 'MZ Windows backend');
  assert.equal(result.backend, path.join(work, 'cross/desktop/.runtime/backend'));
  const web = path.resolve(desktop, '../ServiceBusViewer.Web');
  assert.ok(calls.slice(0, 3).every(call => call.options.cwd === web));
  assert.ok(calls[0].args[0].startsWith(web + path.sep));
  const publish = calls.find(call => call.command === 'dotnet');
  assert.equal(publish.args[1], path.resolve(desktop, '../ServiceBusViewer/ServiceBusViewer.csproj'));
  assert.ok(publish.args.includes('linux-x64'));
  assert.equal(publish.options.cwd, path.join(work, 'cross/desktop'));
});

test('Linux executable validation rejects PE and a different ELF architecture', async t => {
  const desktop = await fixture(t);
  const file = path.join(desktop, 'executable');
  await writeFile(file, 'MZ Windows executable');
  await assert.rejects(validateLinuxExecutable(file, 'x64'), /ELF/);
  const elf = Buffer.alloc(64);
  elf.writeUInt32BE(0x7f454c46); elf[4] = 2; elf[5] = 1; elf.writeUInt16LE(62, 18);
  await writeFile(file, elf);
  await validateLinuxExecutable(file, 'x64');
  await assert.rejects(validateLinuxExecutable(file, 'arm64'), /ELF/);
});
