import assert from 'node:assert/strict';
import { cp, mkdir, mkdtemp, readFile, readdir, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { packageApplication } from '../scripts/package.mjs';
import { resolveRuntimeTarget } from '../scripts/runtime-targets.mjs';

const fixtures = fileURLToPath(new URL('../../../.agents/.tmp/implementation-user-story/63/package-tests/', import.meta.url));
const files = ['electron/main.mjs', 'electron/backend-process.mjs', 'electron/shell-policy.mjs', 'electron/window-size.mjs', 'scripts/runtime-targets.mjs', 'assets/app-logo.png'];

async function fixture(t, { missing, failure, wrongRid, wrongOutput, platform = 'win32' } = {}) {
  await mkdir(fixtures, { recursive: true });
  const desktop = await mkdtemp(path.join(fixtures, 'bundle with spaces-'));
  t.after(() => rm(desktop, { recursive: true, force: true }));
  const target = resolveRuntimeTarget(platform, 'x64');
  const destination = path.join(desktop, 'out', `ServiceBusViewer-1.2.0-${platform}-x64`);
  await mkdir(destination, { recursive: true });
  await writeFile(path.join(destination, 'stale'), 'old delivery');
  for (const file of [...files, 'secrets.env', 'tests/private.test.mjs', 'node_modules/unrelated/index.js']) {
    await mkdir(path.dirname(path.join(desktop, file)), { recursive: true });
    await writeFile(path.join(desktop, file), 'fixture');
  }
  await writeFile(path.join(desktop, 'package.json'), JSON.stringify({ name: 'fixture', version: '1.2.0', type: 'module', main: 'electron/main.mjs', scripts: { private: 'secret' }, devDependencies: { electron: '45.3.0' } }));
  await mkdir(path.join(desktop, 'node_modules/electron'), { recursive: true });
  await writeFile(path.join(desktop, 'node_modules/electron/package.json'), JSON.stringify({ version: '45.3.0' }));
  await writeFile(path.join(desktop, 'node_modules/electron/checksums.json'), JSON.stringify({ [`electron-v45.3.0-${platform}-x64.zip`]: 'fixture-checksum' }));
  let calls = 0;
  let options;
  const stage = async () => {
    if (failure === 'stage') throw new Error('controlled staging failure');
    const backend = path.join(desktop, '.runtime/backend');
    await rm(backend, { recursive: true, force: true });
    const libraries = platform === 'win32' ? ['hostfxr.dll', 'hostpolicy.dll'] : platform === 'darwin' ? ['libhostfxr.dylib', 'libhostpolicy.dylib'] : ['libhostfxr.so', 'libhostpolicy.so'];
    for (const file of [target.executable, target.coreRuntime, ...libraries, 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json', 'wwwroot/index.html', 'appsettings.json', 'appsettings.Development.json', 'appsettings.Local.json', 'ServiceBusViewer.pdb']) {
      if (file === missing) continue;
      await mkdir(path.dirname(path.join(backend, file)), { recursive: true });
      const content = file.endsWith('.deps.json') ? JSON.stringify({ runtimeTarget: { name: `.NETCoreApp,Version=v10.0/${wrongRid ?? target.rid}` } })
        : file.endsWith('.runtimeconfig.json') ? JSON.stringify({ runtimeOptions: { includedFrameworks: [{ name: 'Microsoft.NETCore.App' }] } }) : 'compiled fixture';
      await writeFile(path.join(backend, file), content, { mode: 0o755 });
    }
    return { backend, ...target };
  };
  const pack = async opts => {
    calls++; options = opts;
    if (failure === 'pack') throw new Error('controlled packager failure');
    const output = path.join(opts.out, `ServiceBusViewer-${platform}-x64`);
    const root = platform === 'darwin' ? path.join(output, 'ServiceBusViewer.app/Contents') : output;
    const resources = path.join(root, platform === 'darwin' ? 'Resources' : 'resources');
    await mkdir(resources, { recursive: true });
    await cp(opts.dir, path.join(resources, 'app'), { recursive: true });
    await cp(opts.extraResource[0], path.join(resources, 'backend'), { recursive: true });
    if (failure !== 'missing-executable') {
      const executable = path.join(root, platform === 'darwin' ? 'MacOS/ServiceBusViewer' : `ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`);
      await mkdir(path.dirname(executable), { recursive: true });
      await writeFile(executable, 'native fixture', { mode: 0o755 });
    }
    if (failure === 'packaged-backend') await rm(path.join(resources, 'backend', target.coreRuntime));
    return [wrongOutput ? path.join(opts.out, 'other-platform') : output];
  };
  return { desktop, platform, arch: 'x64', stage, pack, icon: async () => undefined, destination, calls: () => calls, options: () => options };
}

test('fresh bundle includes only runtime application files and backend extra resources', async t => {
  const f = await fixture(t);
  const result = await packageApplication(f);
  assert.equal(result.bundle, f.destination);
  assert.equal(result.rid, 'win-x64');
  assert.equal(f.calls(), 1);
  const opts = f.options();
  assert.equal(opts.platform, 'win32');
  assert.equal(opts.arch, 'x64');
  assert.equal(opts.electronVersion, '45.3.0');
  assert.equal(opts.asar, false);
  const resources = path.join(result.bundle, 'resources');
  assert.deepEqual((await readdir(path.join(resources, 'app'))).sort(), ['assets', 'electron', 'package.json', 'scripts']);
  assert.deepEqual(await readdir(path.join(resources, 'app/scripts')), ['runtime-targets.mjs']);
  const metadata = JSON.parse(await readFile(path.join(resources, 'app/package.json'), 'utf8'));
  assert.equal(metadata.devDependencies, undefined);
  assert.equal(metadata.scripts, undefined);
  assert.ok(await stat(path.join(resources, 'backend/wwwroot/index.html')));
  assert.ok(await stat(path.join(resources, 'backend/appsettings.json')));
  for (const file of ['appsettings.Development.json', 'appsettings.Local.json', 'ServiceBusViewer.pdb']) {
    assert.equal(await stat(path.join(resources, 'backend', file)).catch(() => null), null);
  }
  assert.equal(await stat(path.join(result.bundle, 'stale')).catch(() => null), null);
  assert.equal(await stat(path.join(f.desktop, '.runtime/package')).catch(() => null), null);
});

for (const failure of ['stage', 'pack', 'missing-executable', 'packaged-backend']) {
  test(`${failure} rejects without retaining a previous successful artifact`, async t => {
    const f = await fixture(t, { failure });
    await assert.rejects(packageApplication(f));
    assert.equal(await stat(f.destination).catch(() => null), null);
    assert.equal(await stat(path.join(f.desktop, '.runtime/package')).catch(() => null), null);
    assert.equal(await stat(path.join(f.desktop, '.runtime/.package-lock')).catch(() => null), null);
  });
}

for (const missing of ['ServiceBusViewer.exe', 'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll', 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json', 'wwwroot/index.html']) {
  test(`missing staged ${missing} prevents packager invocation`, async t => {
    const f = await fixture(t, { missing });
    await assert.rejects(packageApplication(f), /Required bundle output/);
    assert.equal(f.calls(), 0);
    assert.equal(await stat(f.destination).catch(() => null), null);
  });
}

for (const options of [{ wrongRid: 'linux-arm64' }, { wrongOutput: true }]) {
  test(`mismatched ${options.wrongRid ? 'runtime target' : 'packager output'} cannot become a delivery`, async t => {
    const f = await fixture(t, options);
    await assert.rejects(packageApplication(f), /native/);
    assert.equal(await stat(f.destination).catch(() => null), null);
  });
}

test('unsupported native target fails before any staging or packaging', async t => {
  const f = await fixture(t);
  let staged = false;
  await assert.rejects(packageApplication({ ...f, arch: 'ia32', stage: async () => { staged = true; } }), /Unsupported/);
  assert.equal(staged, false);
  assert.equal(f.calls(), 0);
});

test('macOS fixture checks .app resource layout only, without claiming native launch', async t => {
  const f = await fixture(t, { platform: 'darwin' });
  const result = await packageApplication(f);
  assert.ok(await stat(path.join(result.bundle, 'ServiceBusViewer.app/Contents/Resources/backend/ServiceBusViewer')));
});

test('Linux fixture preserves backend executable modes on hosts that expose POSIX modes', async t => {
  const f = await fixture(t, { platform: 'linux' });
  const result = await packageApplication(f);
  const executable = await stat(path.join(result.bundle, 'resources/backend/ServiceBusViewer'));
  if (process.platform !== 'win32') assert.equal(executable.mode & 0o111, 0o111);
});
