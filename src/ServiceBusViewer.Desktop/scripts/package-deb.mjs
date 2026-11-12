import { spawn } from 'node:child_process';
import { lstat, mkdir, readFile, readdir, rename, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { bundleDirectoryName, packageApplication, validateLinuxExecutable } from './package.mjs';

const desktopDirectory = fileURLToPath(new URL('..', import.meta.url));
const licenseFile = fileURLToPath(new URL('../../../LICENSE', import.meta.url));
export const ubuntuDependencies = [
  'libc6 (>= 2.39)',
  'libgcc-s1',
  'libstdc++6',
  'ca-certificates',
  'tzdata',
  'libssl3t64',
  'libicu74',
  'libgssapi-krb5-2',
  'zlib1g',
  'libglib2.0-0t64',
  'libnspr4',
  'libnss3',
  'libatk1.0-0t64',
  'libatk-bridge2.0-0t64',
  'libcups2t64',
  'libdbus-1-3',
  'libcairo2',
  'libgtk-3-0t64',
  'libpango-1.0-0',
  'libx11-6',
  'libxcomposite1',
  'libxdamage1',
  'libxext6',
  'libxfixes3',
  'libxrandr2',
  'libgbm1',
  'libdrm2',
  'libexpat1',
  'libxcb1',
  'libxkbcommon0',
  'libudev1',
  'libasound2t64',
  'libatspi2.0-0t64',
];

export function debianVersion(version) {
  bundleDirectoryName(version, 'linux', 'x64');
  if (!/^\d+\.\d+\.\d+$/.test(version)) {
    throw new Error('DEB packaging currently supports release versions only, without prerelease or build identifiers.');
  }
  return version;
}

export function runDebCommand(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      ...options,
      shell: false,
      stdio: ['ignore', 'pipe', 'pipe'],
    });
    let stdout = '';
    let stderr = '';
    child.stdout.setEncoding('utf8').on('data', chunk => {
      stdout += chunk;
    });
    child.stderr.setEncoding('utf8').on('data', chunk => {
      stderr += chunk;
    });
    child.once('error', reject);
    child.once('close', (code, signal) => {
      if (code === 0) resolve(stdout);
      else reject(new Error(`${path.basename(command)} failed (${signal ?? `exit ${code}`}): ${stderr.trim()}`));
    });
  });
}

async function realDirectory(directory) {
  const info = await lstat(directory).catch(error => {
    if (error.code !== 'ENOENT') throw error;
    return null;
  });
  if (info && (!info.isDirectory() || info.isSymbolicLink())) {
    throw new Error(`Generated output must be a real directory: ${directory}`);
  }
  await mkdir(directory, { recursive: true });
}

async function requireFile(file) {
  const info = await lstat(file).catch(() => null);
  if (!info?.isFile() || !info.size) {
    throw new Error(`Required DEB input/output is missing or empty: ${file}`);
  }
}

async function rejectLinks(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const child = path.join(directory, entry.name);
    if (entry.isDirectory()) await rejectLinks(child);
    else if (!entry.isFile()) throw new Error(`DEB input must contain only regular files and directories: ${child}`);
  }
}

const executables = [
  './opt/servicebusviewer/ServiceBusViewer',
  './opt/servicebusviewer/resources/backend/ServiceBusViewer',
  './opt/servicebusviewer/chrome_crashpad_handler',
];
const sandbox = './opt/servicebusviewer/chrome-sandbox';
const launcher = './usr/bin/servicebusviewer';

export function validateDebContents(contents) {
  // Inspect every entry: only the launcher link and sandbox setuid bit are permitted.
  const found = new Set();
  for (const line of contents.trim().split('\n')) {
    const match = /^(\S+)\s+(\S+)\s+\d+\s+\S+\s+\S+\s+(.+)$/.exec(line);
    if (!match) {
      throw new Error('Invalid DEB content manifest.');
    }
    const [, mode, owner, entry] = match;
    if (!['root/root', '0/0'].includes(owner)) {
      throw new Error(`DEB entry is not root-owned: ${entry}`);
    }
    if (mode[0] === 'l') {
      if (entry !== `${launcher} -> /opt/servicebusviewer/ServiceBusViewer`) {
        throw new Error('Unexpected DEB symbolic link.');
      }
      found.add(launcher);
      continue;
    }
    if (entry.includes('/../') || !entry.startsWith('./')) {
      throw new Error('Invalid DEB archive path.');
    }
    const isExecutable = executables.includes(entry)
      || entry === './opt/servicebusviewer/resources/backend/createdump';
    const expected = mode[0] === 'd'
      ? 'drwxr-xr-x'
      : entry === sandbox
        ? '-rwsr-xr-x'
        : isExecutable
          ? '-rwxr-xr-x'
          : '-rw-r--r--';
    if (mode !== expected) {
      throw new Error(`Unexpected DEB permissions: ${entry} (${mode}).`);
    }
    found.add(entry);
  }
  for (const required of [
    ...executables, sandbox, launcher, './opt/servicebusviewer/', './usr/bin/',
    './usr/share/applications/servicebusviewer.desktop', './usr/share/pixmaps/servicebusviewer.png',
    './usr/share/doc/servicebusviewer/copyright',
  ]) {
    if (!found.has(required)) {
      throw new Error(`Required DEB archive entry is missing: ${required}`);
    }
  }
  return found.size;
}

// All shell text is static. Paths and metadata are passed as positional arguments;
// chmod operates only on the Linux-native temporary copy, never on a mounted checkout.
const assemblyScript = `#!/bin/bash
set -euo pipefail
bundle=$1
metadata=$2
output=$3
version=$4
scratch=$(mktemp -d /tmp/servicebusviewer-deb.XXXXXXXX)
trap 'rm -rf -- "$scratch"' EXIT
tree="$scratch/root"
app="$tree/opt/servicebusviewer"
mkdir -p "$app" "$tree/DEBIAN"
mkdir -p "$tree/usr/bin" "$tree/usr/share/applications"
mkdir -p "$tree/usr/share/pixmaps" "$tree/usr/share/doc/servicebusviewer"
cp -R --no-preserve=mode,ownership -- "$bundle/." "$app/"
cp -- "$metadata/control" "$tree/DEBIAN/control"
cp -- "$metadata/servicebusviewer.desktop" "$tree/usr/share/applications/"
cp -- "$metadata/copyright" "$tree/usr/share/doc/servicebusviewer/copyright"
cp -- "$app/resources/app/assets/app-logo.png" "$tree/usr/share/pixmaps/servicebusviewer.png"
ln -s /opt/servicebusviewer/ServiceBusViewer "$tree/usr/bin/servicebusviewer"
# Keep application files non-writable to regular users; only the sandbox helper is setuid.
find "$tree" -type d -exec chmod 0755 {} +
find "$tree" -type f -exec chmod 0644 {} +
chmod 0755 "$app/ServiceBusViewer" "$app/resources/backend/ServiceBusViewer" "$app/chrome_crashpad_handler"
if [ -f "$app/resources/backend/createdump" ]; then
  chmod 0755 "$app/resources/backend/createdump"
fi
chmod 4755 "$app/chrome-sandbox"
printf 'Installed-Size: %s\\n' "$(du -sk "$tree/opt" "$tree/usr" | awk '{sum += $1} END {print sum}')" >> "$tree/DEBIAN/control"
dpkg-deb --root-owner-group --build "$tree" "$scratch/application.deb"
test "$(dpkg-deb -f "$scratch/application.deb" Package)" = servicebusviewer
test "$(dpkg-deb -f "$scratch/application.deb" Version)" = "$version"
test "$(dpkg-deb -f "$scratch/application.deb" Architecture)" = amd64
dpkg-deb --info "$scratch/application.deb" > "$metadata/package.info"
dpkg-deb --contents "$scratch/application.deb" > "$metadata/package.contents"
cp -- "$scratch/application.deb" "$output"
`;

export async function packageDeb({
  desktop = desktopDirectory,
  platform = process.platform,
  build = packageApplication,
  run = runDebCommand,
} = {}) {
  if (!['win32', 'linux'].includes(platform)) {
    throw new Error('DEB assembly requires Ubuntu 24.04 amd64, or Windows with the existing Ubuntu-24.04 WSL distribution.');
  }
  const metadata = JSON.parse(await readFile(path.join(desktop, 'package.json'), 'utf8'));
  const version = debianVersion(metadata.version);
  const baseName = bundleDirectoryName(version, 'linux', 'x64');
  const linux = args => {
    if (platform === 'win32') {
      return run(
        'wsl.exe',
        ['--distribution', 'Ubuntu-24.04', '--exec', ...args],
        { cwd: desktop },
      );
    }
    return run(args[0], args.slice(1), { cwd: desktop });
  };
  // Verify the Linux assembly host before creating or replacing package output.
  const host = await linux([
    'sh', '-c',
    '. /etc/os-release; printf "%s\\n%s\\n" "$ID" "$VERSION_ID"; dpkg --print-architecture; command -v dpkg-deb >/dev/null; command -v bash >/dev/null; command -v tar >/dev/null',
  ]);
  if (host.trim() !== 'ubuntu\n24.04\namd64') {
    throw new Error('DEB assembly supports Ubuntu 24.04 amd64 only.');
  }
  const runtime = path.join(desktop, '.runtime');
  const out = path.join(desktop, 'out');
  await realDirectory(runtime);
  await realDirectory(out);
  const lock = path.join(runtime, '.deb-lock');
  await mkdir(lock).catch(error => {
    if (error.code === 'EEXIST') {
      throw new Error('DEB packaging is already running, or .runtime/.deb-lock remains from an interrupted build.');
    }
    throw error;
  });
  const work = path.join(runtime, 'deb');
  const destination = path.join(out, `${baseName}.deb`);
  try {
    await rm(destination, { force: true });
    await rm(work, { recursive: true, force: true });
    await mkdir(work, { recursive: true });
    // Produce and validate the Linux bundle before assembling its Debian archive.
    const bundled = await build({
      desktop,
      platform: 'linux',
      arch: 'x64',
      crossBuild: platform === 'win32',
    });
    if (bundled.rid !== 'linux-x64' || path.resolve(bundled.bundle) !== path.join(out, baseName)) {
      throw new Error('DEB build returned an unexpected Linux bundle.');
    }
    await rejectLinks(bundled.bundle);
    for (const file of [
      'ServiceBusViewer', 'resources/backend/ServiceBusViewer',
      'chrome-sandbox', 'chrome_crashpad_handler', 'resources/app/assets/app-logo.png',
    ]) {
      await requireFile(path.join(bundled.bundle, file));
    }
    await validateLinuxExecutable(path.join(bundled.bundle, 'ServiceBusViewer'), 'x64');
    await validateLinuxExecutable(path.join(bundled.bundle, 'resources/backend/ServiceBusViewer'), 'x64');

    // Generate package metadata separately from the static Linux assembly script.
    await writeFile(
      path.join(work, 'control'),
      `Package: servicebusviewer\nVersion: ${version}\n`
      + 'Section: utils\nPriority: optional\nArchitecture: amd64\n'
      + 'Maintainer: Andrey Veselov\n'
      + `Depends: ${ubuntuDependencies.join(', ')}\n`
      + 'Description: Service Bus Viewer desktop for Ubuntu 24.04 x64\n'
      + ' Integrated Electron application with a self-contained backend.\n',
    );
    await writeFile(
      path.join(work, 'servicebusviewer.desktop'),
      '[Desktop Entry]\nType=Application\nName=Service Bus Viewer\n'
      + 'Comment=Browse Azure Service Bus messages\n'
      + 'Exec=/usr/bin/servicebusviewer\nTryExec=/usr/bin/servicebusviewer\n'
      + 'Icon=servicebusviewer\nTerminal=false\nCategories=Development;Utility;\n'
      + 'StartupWMClass=servicebusviewer-desktop\n',
    );
    await writeFile(path.join(work, 'copyright'), await readFile(licenseFile));
    const script = path.join(work, 'assemble.sh');
    await writeFile(script, assemblyScript);
    const temporaryDeb = path.join(work, 'application.deb');
    // WSL receives Linux paths as arguments; checkout paths never become shell text.
    const linuxPath = async file => platform === 'win32'
      ? (await linux(['wslpath', '-a', '-u', file])).trim()
      : file;
    const args = [];
    for (const file of [script, bundled.bundle, work, temporaryDeb]) {
      args.push(await linuxPath(file));
    }
    await linux(['bash', ...args, version]);
    await requireFile(temporaryDeb);

    // Publish only after checking the archive's ownership, permissions and metadata.
    const entries = validateDebContents(await readFile(path.join(work, 'package.contents'), 'utf8'));
    const info = await readFile(path.join(work, 'package.info'), 'utf8');
    if (!info.includes('Package: servicebusviewer')
      || !info.includes(`Version: ${version}\n`)
      || !info.includes('Architecture: amd64')) {
      throw new Error('DEB package metadata validation failed.');
    }
    await rename(temporaryDeb, destination);
    return { deb: destination, version, architecture: 'amd64', entries };
  } finally {
    await rm(work, { recursive: true, force: true });
    await rm(lock, { recursive: true, force: true });
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  Promise.resolve().then(() => {
    if (process.argv.length !== 2) {
      throw new Error('package:deb does not accept target switches; its target is Ubuntu 24.04 x64.');
    }
    return packageDeb();
  }).then(({ deb, entries }) =>
    console.log(`Packaged Ubuntu 24.04 amd64 installer at ${deb} (${entries} verified root-owned entries)`)
  ).catch(error => {
    console.error(`DEB packaging failed: ${error.message}`);
    process.exitCode = 1;
  });
}
