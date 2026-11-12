import assert from 'node:assert/strict';
import { mkdir, mkdtemp, readFile, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';
import { debianVersion, packageDeb, ubuntuDependencies, validateDebContents } from '../scripts/package-deb.mjs';

const fixtures = fileURLToPath(new URL('../../../.agents/.tmp/implementation-user-story/63/deb/tests/', import.meta.url));
const archiveEntries = [
  ['drwxr-xr-x', './'], ['drwxr-xr-x', './opt/'], ['drwxr-xr-x', './opt/servicebusviewer/'],
  ['drwxr-xr-x', './usr/'], ['drwxr-xr-x', './usr/bin/'],
  ['-rwxr-xr-x', './opt/servicebusviewer/ServiceBusViewer'],
  ['-rwxr-xr-x', './opt/servicebusviewer/resources/backend/ServiceBusViewer'],
  ['-rwxr-xr-x', './opt/servicebusviewer/chrome_crashpad_handler'],
  ['-rwsr-xr-x', './opt/servicebusviewer/chrome-sandbox'],
  ['lrwxrwxrwx', './usr/bin/servicebusviewer -> /opt/servicebusviewer/ServiceBusViewer'],
  ['-rw-r--r--', './usr/share/applications/servicebusviewer.desktop'],
  ['-rw-r--r--', './usr/share/pixmaps/servicebusviewer.png'],
  ['-rw-r--r--', './usr/share/doc/servicebusviewer/copyright'],
];
const manifest = archiveEntries.map(([mode, file]) => `${mode} root/root 1 2026-11-12 12:00 ${file}`).join('\n') + '\n';

async function fixture(t, { failure, missing, version = '1.2.0', host = 'ubuntu\n24.04\namd64\n' } = {}) {
  await mkdir(fixtures, { recursive: true });
  const desktop = await mkdtemp(path.join(fixtures, 'deb with spaces-'));
  t.after(() => rm(desktop, { recursive: true, force: true }));
  await writeFile(path.join(desktop, 'package.json'), JSON.stringify({ version }));
  const destination = path.join(desktop, 'out/ServiceBusViewer-1.2.0-linux-x64.deb');
  await mkdir(path.dirname(destination), { recursive: true });
  await writeFile(destination, 'old current package');
  await writeFile(path.join(desktop, 'out/ServiceBusViewer-1.1.0-linux-x64.deb'), 'older version');
  let builds = 0;
  const calls = [];
  const build = async options => {
    builds++;
    assert.equal(options.platform, 'linux');
    assert.equal(options.arch, 'x64');
    if (failure === 'build') throw new Error('controlled bundle failure');
    const bundle = path.join(desktop, 'out/ServiceBusViewer-1.2.0-linux-x64');
    const elf = Buffer.alloc(64);
    elf.writeUInt32BE(0x7f454c46); elf[4] = 2; elf[5] = 1; elf.writeUInt16LE(62, 18);
    for (const file of ['ServiceBusViewer', 'resources/backend/ServiceBusViewer', 'chrome-sandbox', 'chrome_crashpad_handler', 'resources/app/assets/app-logo.png']) {
      if (file === missing) continue;
      await mkdir(path.dirname(path.join(bundle, file)), { recursive: true });
      await writeFile(path.join(bundle, file), file.endsWith('ServiceBusViewer') ? elf : 'fixture');
    }
    return { bundle, rid: 'linux-x64' };
  };
  const run = async (command, args) => {
    calls.push({ command, args });
    if (command === 'sh') return host;
    assert.equal(command, 'bash');
    if (failure === 'assembler') throw new Error('controlled dpkg-deb failure');
    const [, , work, output] = args;
    const script = await readFile(args[0], 'utf8');
    assert.ok(script.includes('dpkg-deb --root-owner-group --build'));
    assert.ok(script.includes('mktemp -d /tmp/servicebusviewer-deb.'));
    assert.ok(script.includes('chmod 4755 "$app/chrome-sandbox"'));
    assert.ok(!script.includes('sudo'));
    await writeFile(path.join(work, 'package.contents'), failure === 'permissions' ? manifest.replace('-rwsr-xr-x', '-rwxrwxrwx') : manifest);
    await writeFile(path.join(work, 'package.info'), ` Package: servicebusviewer\n Version: ${failure === 'metadata' ? '9.9.9' : '1.2.0'}\n Architecture: amd64\n`);
    if (failure !== 'missing-output') await writeFile(output, 'controlled DEB fixture');
    return '';
  };
  return { desktop, destination, build, run, platform: 'linux', builds: () => builds, calls };
}

test('release-only Debian versions reject ambiguous prerelease/build and unsafe paths', () => {
  assert.equal(debianVersion('1.2.0'), '1.2.0');
  for (const version of ['1.2.0-beta.1', '1.2.0+build', '../1.2.0', '1.2.0\n']) assert.throws(() => debianVersion(version));
});

test('content manifest requires root ownership, fixed modes and the exact launcher link', () => {
  assert.equal(validateDebContents(manifest), archiveEntries.length);
  for (const changed of [manifest.replace('root/root', 'user/user'), manifest.replace('drwxr-xr-x', 'drwxrwxrwx'), manifest.replace('-rwsr-xr-x', '-rwxr-xr-x'), manifest.replace('-> /opt/servicebusviewer/ServiceBusViewer', '-> /tmp/untrusted'), manifest.replace(/.*chrome-sandbox.*\n/, '')]) assert.throws(() => validateDebContents(changed));
});

test('DEB orchestrator builds fresh input, validates archive and publishes a versioned package', async t => {
  const f = await fixture(t);
  const result = await packageDeb(f);
  assert.equal(result.deb, f.destination);
  assert.equal(result.architecture, 'amd64');
  assert.equal(f.builds(), 1);
  assert.equal(await readFile(result.deb, 'utf8'), 'controlled DEB fixture');
  assert.equal(await readFile(path.join(f.desktop, 'out/ServiceBusViewer-1.1.0-linux-x64.deb'), 'utf8'), 'older version');
  assert.equal(await stat(path.join(f.desktop, '.runtime/deb')).catch(() => null), null);
  assert.ok(ubuntuDependencies.includes('libicu74'));
  assert.ok(ubuntuDependencies.includes('libgtk-3-0t64'));
});

for (const failure of ['build', 'assembler', 'missing-output', 'permissions', 'metadata']) {
  test(`${failure} prevents publishing an old or invalid current DEB`, async t => {
    const f = await fixture(t, { failure });
    await assert.rejects(packageDeb(f));
    assert.equal(await stat(f.destination).catch(() => null), null);
    assert.equal(await stat(path.join(f.desktop, '.runtime/.deb-lock')).catch(() => null), null);
    assert.equal(await stat(path.join(f.desktop, '.runtime/deb')).catch(() => null), null);
    assert.equal(await readFile(path.join(f.desktop, 'out/ServiceBusViewer-1.1.0-linux-x64.deb'), 'utf8'), 'older version');
  });
}

test('missing sandbox helper prevents assembly', async t => {
  const f = await fixture(t, { missing: 'chrome-sandbox' });
  await assert.rejects(packageDeb(f), /missing or empty/);
  assert.equal(f.calls.filter(call => call.command === 'bash').length, 0);
});

test('unsupported Ubuntu host fails before bundle generation', async t => {
  const f = await fixture(t, { host: 'ubuntu\n22.04\namd64\n' });
  await assert.rejects(packageDeb(f), /Ubuntu 24.04 amd64 only/);
  assert.equal(f.builds(), 0);
});

test('WSL assembly passes paths containing spaces as separate arguments', async t => {
  const f = await fixture(t);
  const calls = [];
  const run = async (command, args) => {
    calls.push({ command, args });
    assert.equal(command, 'wsl.exe');
    assert.deepEqual(args.slice(0, 3), ['--distribution', 'Ubuntu-24.04', '--exec']);
    if (args[3] === 'wslpath') return args[6] + '\n';
    return f.run(args[3], args.slice(4));
  };
  await packageDeb({ ...f, platform: 'win32', run });
  assert.equal(calls.filter(call => call.args[3] === 'wslpath').length, 4);
  const assembly = calls.find(call => call.args[3] === 'bash');
  assert.equal(assembly.args.length, 9);
  assert.ok(assembly.args.slice(4, 8).every(argument => argument.includes('deb with spaces-')));
  assert.equal(assembly.args[8], '1.2.0');
});
