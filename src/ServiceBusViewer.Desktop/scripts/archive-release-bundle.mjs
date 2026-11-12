import { spawn } from 'node:child_process';
import { lstat, readFile, readdir, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export async function validateBundle(bundle, { version, platform, arch }) {
  const expected = `ServiceBusViewer-${version}-${platform}-${arch}`;
  if (!['win32', 'linux', 'darwin'].includes(platform)
    || !({ win32: ['x64'], linux: ['x64'], darwin: ['arm64'] }[platform] ?? []).includes(arch)) {
    throw new Error(`Unsupported release target: ${platform}/${arch}.`);
  }
  const info = await lstat(bundle).catch(() => null);
  if (!info?.isDirectory() || info.isSymbolicLink() || path.basename(bundle) !== expected) {
    throw new Error(`Expected a fresh bundle directory named ${expected}.`);
  }
  const root = platform === 'darwin' ? path.join(bundle, 'ServiceBusViewer.app', 'Contents') : bundle;
  const executable = path.join(root, platform === 'darwin' ? 'MacOS/ServiceBusViewer' : `ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`);
  const backendExecutable = path.join(root, platform === 'darwin' ? 'Resources/backend/ServiceBusViewer' : `resources/backend/ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`);
  const resources = path.join(root, platform === 'darwin' ? 'Resources' : 'resources');
  for (const file of [executable, backendExecutable, path.join(resources, 'app/package.json')]) {
    const entry = await stat(file).catch(() => null);
    if (!entry?.isFile() || !entry.size) throw new Error(`Required runnable bundle file is missing or empty: ${file}`);
  }
  const packageInfo = JSON.parse(await readFile(path.join(resources, 'app/package.json'), 'utf8'));
  if (packageInfo.version !== version) throw new Error(`Bundle version ${packageInfo.version} does not match ${version}.`);
  const entries = await readdir(resources);
  if (!entries.includes('app') || !entries.includes('backend')) throw new Error('Bundle resources are incomplete.');
  if (platform !== 'win32') {
    for (const file of [executable, backendExecutable]) {
      if (((await stat(file)).mode & 0o111) === 0) {
        throw new Error(`Runnable bundle executable has no executable mode bits: ${file}`);
      }
    }
  }
  if (platform === 'darwin') {
    const infoPlist = await stat(path.join(root, 'Info.plist')).catch(() => null);
    if (!infoPlist?.isFile() || !infoPlist.size) throw new Error('macOS application bundle is missing Contents/Info.plist.');
  }
  return { expected, executable };
}

export async function archiveBundle({ bundle, output, version, platform, arch, command = 'tar', runCommand = run }) {
  const { expected } = await validateBundle(bundle, { version, platform, arch });
  const format = platform === 'win32' ? 'zip' : 'tar.gz';
  const archiveName = `ServiceBusViewer-${version}-${platform}-${arch}.${format}`;
  const destination = path.resolve(output, archiveName);
  await runCommand(command, platform === 'win32'
    ? ['-a', '-c', '-f', destination, '-C', path.dirname(bundle), expected]
    : ['-czf', destination, '-C', path.dirname(bundle), expected]);
  const archive = await stat(destination).catch(() => null);
  if (!archive?.isFile() || !archive.size) throw new Error(`Archive was not created: ${destination}`);
  return destination;
}

function run(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { shell: false, stdio: 'inherit' });
    child.once('error', reject);
    child.once('close', code => code === 0 ? resolve() : reject(new Error(`${command} failed (exit ${code}).`)));
  });
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [bundle, output, version, platform, arch] = process.argv.slice(2);
  archiveBundle({ bundle, output, version, platform, arch })
    .then(file => console.log(`Archived ${file}.`))
    .catch(error => { console.error(error.message); process.exitCode = 1; });
}
