import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, readdir, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { bundleDirectoryName, packageApplication } from '../scripts/package.mjs';

const fixtures = fileURLToPath(new URL('../../../.agents/.tmp/implementation-user-story/63/versioned-bundles/tests/', import.meta.url));
async function fixture(t, version) {
  await mkdir(fixtures, { recursive: true });
  const desktop = await mkdtemp(path.join(fixtures, 'version-'));
  t.after(() => rm(desktop, { recursive: true, force: true }));
  await writeFile(path.join(desktop, 'package.json'), JSON.stringify({ version }));
  return desktop;
}

for (const platform of ['win32', 'linux', 'darwin']) {
  for (const arch of ['x64', 'arm64']) {
    test(`${platform}/${arch} delivery directory includes the application version`, () => {
      assert.equal(bundleDirectoryName('1.2.0', platform, arch), `ServiceBusViewer-1.2.0-${platform}-${arch}`);
    });
  }
}

test('semantic prerelease and build identifiers are valid safe directory components', () => {
  assert.equal(bundleDirectoryName('1.2.0-rc.1+build.23', 'linux', 'x64'), 'ServiceBusViewer-1.2.0-rc.1+build.23-linux-x64');
});

for (const version of ['../1.2.0', '1.2.0/../other', '1.2.0\\other', '1.2.0 ', '1.2.0\n', '01.2.0', '1.2.0-01', '1.2', '', null]) {
  test(`invalid version ${JSON.stringify(version)} fails before generated directories or staging`, async t => {
    const desktop = await fixture(t, version);
    let staged = false;
    await assert.rejects(packageApplication({ desktop, platform: 'win32', arch: 'x64', stage: async () => { staged = true; } }), /strict semantic version/);
    assert.equal(staged, false);
    assert.deepEqual(await readdir(desktop), ['package.json']);
  });
}

test('failed rebuild invalidates only the current version and preserves other deliveries', async t => {
  const desktop = await fixture(t, '1.2.0');
  for (const name of ['ServiceBusViewer-1.2.0-win32-x64', 'ServiceBusViewer-1.1.0-win32-x64', 'ServiceBusViewer-win32-x64']) {
    await mkdir(path.join(desktop, 'out', name), { recursive: true });
    await writeFile(path.join(desktop, 'out', name, 'sentinel'), name);
  }
  await assert.rejects(packageApplication({ desktop, platform: 'win32', arch: 'x64', stage: async () => { throw new Error('controlled build failure'); } }), /controlled build failure/);
  assert.equal(await stat(path.join(desktop, 'out/ServiceBusViewer-1.2.0-win32-x64')).catch(() => null), null);
  for (const name of ['ServiceBusViewer-1.1.0-win32-x64', 'ServiceBusViewer-win32-x64']) assert.equal(await readFile(path.join(desktop, 'out', name, 'sentinel'), 'utf8'), name);
});
