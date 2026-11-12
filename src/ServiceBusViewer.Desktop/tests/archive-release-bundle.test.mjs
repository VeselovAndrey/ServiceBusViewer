import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { chmod, mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { archiveBundle, validateBundle } from '../scripts/archive-release-bundle.mjs';

async function fixture(platform = 'linux', arch = 'x64') {
  const directory = await mkdtemp(path.join(os.tmpdir(), 'desktop-release-'));
  const bundle = path.join(directory, `ServiceBusViewer-1.2.0-${platform}-${arch}`);
  const root = platform === 'darwin' ? path.join(bundle, 'ServiceBusViewer.app/Contents') : bundle;
  const resources = path.join(root, platform === 'darwin' ? 'Resources' : 'resources');
  await mkdir(path.join(resources, 'app'), { recursive: true });
  await mkdir(path.join(resources, 'backend'), { recursive: true });
  if (platform === 'darwin') await mkdir(path.join(root, 'MacOS'), { recursive: true });
  const appExecutable = path.join(root, platform === 'darwin' ? 'MacOS/ServiceBusViewer' : `ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`);
  const backendExecutable = path.join(resources, 'backend', `ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`);
  await writeFile(appExecutable, 'app');
  if (platform !== 'win32') await chmod(appExecutable, 0o755);
  await writeFile(backendExecutable, 'backend');
  if (platform !== 'win32') await chmod(backendExecutable, 0o755);
  await writeFile(path.join(resources, 'app/package.json'), JSON.stringify({ version: '1.2.0' }));
  if (platform === 'darwin') await writeFile(path.join(root, 'Info.plist'), 'fixture');
  return { directory, bundle, root, resources, appExecutable, backendExecutable };
}

test('validateBundle rejects unsupported architecture', async () => {
  await assert.rejects(validateBundle('not-a-bundle', { version: '1.2.0', platform: 'darwin', arch: 'x64' }), /Unsupported release target/);
});

test('validateBundle rejects missing runnable output', async () => {
  const { directory, bundle } = await fixture();
  try {
    await rm(path.join(bundle, 'ServiceBusViewer'));
    await assert.rejects(validateBundle(bundle, { version: '1.2.0', platform: 'linux', arch: 'x64' }), /missing or empty/);
  } finally { await rm(directory, { recursive: true, force: true }); }
});

test('validateBundle rejects missing POSIX execute bits on either executable', async () => {
  if (process.platform === 'win32') return;
  for (const missingMode of ['app', 'backend']) {
    const { directory, bundle, appExecutable, backendExecutable } = await fixture();
    try {
      await chmod(missingMode === 'app' ? appExecutable : backendExecutable, 0o644);
      await assert.rejects(validateBundle(bundle, { version: '1.2.0', platform: 'linux', arch: 'x64' }), /no executable mode bits/);
    } finally { await rm(directory, { recursive: true, force: true }); }
  }
});

test('validateBundle rejects wrong archive root name and mismatched app version', async () => {
  const { directory, bundle } = await fixture();
  try {
    await assert.rejects(validateBundle(bundle, { version: '1.2.1', platform: 'linux', arch: 'x64' }), /Expected a fresh bundle directory/);
    await writeFile(path.join(bundle, 'resources/app/package.json'), JSON.stringify({ version: '1.2.1' }));
    await assert.rejects(validateBundle(bundle, { version: '1.2.0', platform: 'linux', arch: 'x64' }), /does not match/);
  } finally { await rm(directory, { recursive: true, force: true }); }
});

test('archiveBundle emits the platform archive with the runnable bundle as its root', async () => {
  const platform = process.platform === 'win32' ? 'win32' : 'linux';
  const arch = 'x64';
  const { directory, bundle } = await fixture(platform, arch);
  const output = path.join(directory, 'artifacts');
  await mkdir(output);
  try {
    const archive = await archiveBundle({ bundle, output, version: '1.2.0', platform, arch });
    assert.equal(path.basename(archive), `ServiceBusViewer-1.2.0-${platform}-${arch}.${platform === 'win32' ? 'zip' : 'tar.gz'}`);
    assert.ok((await readFile(archive)).length > 0);
    const listing = execFileSync('tar', platform === 'win32' ? ['-tf', archive] : ['-tvzf', archive], { encoding: 'utf8' });
    assert.match(listing, new RegExp(`ServiceBusViewer-1\\.2\\.0-${platform}-${arch}\\/ServiceBusViewer${platform === 'win32' ? '\\.exe' : ''}$`, 'm'));
    if (platform !== 'win32') {
      assert.match(listing, /-rwxr-xr-x .*ServiceBusViewer-1\.2\.0-linux-x64\/ServiceBusViewer$/m);
      assert.match(listing, /-rwxr-xr-x .*ServiceBusViewer-1\.2\.0-linux-x64\/resources\/backend\/ServiceBusViewer$/m);
    }
  } finally { await rm(directory, { recursive: true, force: true }); }
});

test('archiveBundle preserves the macOS app layout under the expected archive root', { skip: process.platform === 'win32' }, async () => {
  const { directory, bundle } = await fixture('darwin', 'arm64');
  const output = path.join(directory, 'artifacts');
  await mkdir(output);
  try {
    const archive = await archiveBundle({ bundle, output, version: '1.2.0', platform: 'darwin', arch: 'arm64' });
    assert.equal(path.basename(archive), 'ServiceBusViewer-1.2.0-darwin-arm64.tar.gz');
    const listing = execFileSync('tar', ['-tzf', archive], { encoding: 'utf8' });
    assert.match(listing, /^ServiceBusViewer-1\.2\.0-darwin-arm64\/ServiceBusViewer\.app\/Contents\/MacOS\/ServiceBusViewer$/m);
    assert.match(listing, /^ServiceBusViewer-1\.2\.0-darwin-arm64\/ServiceBusViewer\.app\/Contents\/Resources\/app\/package\.json$/m);
    assert.match(listing, /^ServiceBusViewer-1\.2\.0-darwin-arm64\/ServiceBusViewer\.app\/Contents\/Info\.plist$/m);
  } finally { await rm(directory, { recursive: true, force: true }); }
});
