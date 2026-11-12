import { cp, lstat, mkdir, readFile, readdir, rename, rm, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { stageRuntime, runCommand } from './stage-runtime.mjs';
import { resolveRuntimeTarget } from './runtime-targets.mjs';

const desktopDirectory = fileURLToPath(new URL('..', import.meta.url));
const applicationFiles = [
  'electron/main.mjs',
  'electron/backend-process.mjs',
  'electron/shell-policy.mjs',
  'electron/window-size.mjs',
  'scripts/runtime-targets.mjs',
  'assets/app-logo.png',
];
const applicationName = 'ServiceBusViewer';

export function bundleDirectoryName(version, platform, arch) {
  // Validate the version before using it in generated directory names or cleanup paths.
  const semver = typeof version === 'string' && /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$/.exec(version);
  if (!semver || semver[0] !== version
    || semver[4]?.split('.').some(identifier => /^\d+$/.test(identifier) && !/^(0|[1-9]\d*)$/.test(identifier))) {
    throw new Error('Desktop package version must be a safe, strict semantic version.');
  }
  resolveRuntimeTarget(platform, arch);
  return `${applicationName}-${version}-${platform}-${arch}`;
}

export function parseBundleArguments(args, hostPlatform = process.platform, hostArch = process.arch) {
  const options = { platform: hostPlatform, arch: hostArch };
  const seen = new Set();
  for (const argument of args) {
    const match = /^--(platform|arch)=([^=]+)$/.exec(argument);
    if (!match || seen.has(match[1])) {
      throw new Error(`Invalid or duplicate bundle option: ${argument}. Use --platform=linux --arch=x64 (or arm64).`);
    }
    seen.add(match[1]);
    options[match[1]] = match[2];
  }
  resolveRuntimeTarget(options.platform, options.arch);
  options.crossBuild = options.platform !== hostPlatform || options.arch !== hostArch;
  if (options.crossBuild && !(hostPlatform === 'win32' && options.platform === 'linux')) {
    throw new Error('Only native builds and Windows-to-Linux cross-builds are supported.');
  }
  return options;
}

export async function stageBundleRuntime({ desktop, work, platform, arch, crossBuild, stage, run }) {
  if (!crossBuild) return stage({ desktop, platform, arch, run });

  // Route build paths through an isolated staging directory while sharing the
  // repository's existing frontend and backend source projects.
  const isolatedDesktop = path.join(work, 'cross', 'desktop');
  await mkdir(isolatedDesktop, { recursive: true });
  const sources = ['ServiceBusViewer.Web', 'ServiceBusViewer'].map(name => ({
    isolated: path.resolve(isolatedDesktop, '..', name),
    actual: path.resolve(desktop, '..', name),
  }));
  const sourcePath = value => {
    const source = sources.find(item => value === item.isolated || value.startsWith(`${item.isolated}${path.sep}`));
    return source ? source.actual + value.slice(source.isolated.length) : value;
  };
  const runWithSources = (command, args, options) => run(command, args.map(sourcePath), {
    ...options,
    cwd: sourcePath(options.cwd),
  });
  const result = await stage({ desktop: isolatedDesktop, platform, arch, run: runWithSources });
  if (path.resolve(result.backend) !== path.join(isolatedDesktop, '.runtime', 'backend')) {
    throw new Error('Cross-build staging returned an unexpected output directory.');
  }
  return result;
}

export async function validateLinuxExecutable(file, arch) {
  const bytes = await readFile(file);
  // Confirm ELF64, little-endian format and the requested x86-64 or AArch64 machine.
  const machine = arch === 'x64' ? 62 : 183;
  if (bytes.length < 20
    || bytes.subarray(0, 4).toString('hex') !== '7f454c46'
    || bytes[4] !== 2
    || bytes[5] !== 1
    || bytes.readUInt16LE(18) !== machine) {
    throw new Error(`Expected a Linux ${arch} ELF executable: ${file}`);
  }
}

async function requireFile(file) {
  const info = await stat(file).catch(() => null);
  if (!info?.isFile() || !info.size) {
    throw new Error(`Required bundle output is missing or empty: ${file}`);
  }
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

async function validateBackend(backend, target) {
  const library = name => target.rid.startsWith('win-')
    ? `${name}.dll`
    : `lib${name}.${target.rid.startsWith('osx-') ? 'dylib' : 'so'}`;
  for (const name of [target.executable, 'wwwroot/index.html']) {
    await requireFile(path.join(backend, name));
  }
  const runtimeSidecars = [
    target.coreRuntime,
    library('hostfxr'),
    library('hostpolicy'),
    'ServiceBusViewer.dll',
    'ServiceBusViewer.deps.json',
    'ServiceBusViewer.runtimeconfig.json',
  ];
  const sidecarsExist = await Promise.all(runtimeSidecars.map(async name => stat(path.join(backend, name))
    .then(() => true)
    .catch(error => {
      if (error.code === 'ENOENT') return false;
      throw error;
    })));
  // Windows single-file publishes embed the runtime sidecars; reject incomplete folder publishes.
  if (target.rid.startsWith('win-') && !sidecarsExist.some(Boolean)) return;
  for (const name of runtimeSidecars) {
    await requireFile(path.join(backend, name));
  }
  const config = JSON.parse(await readFile(path.join(backend, 'ServiceBusViewer.runtimeconfig.json'), 'utf8'));
  if (!config.runtimeOptions?.includedFrameworks?.some(item => item.name === 'Microsoft.NETCore.App')
    || config.runtimeOptions.framework || config.runtimeOptions.frameworks) {
    throw new Error('Backend must contain the self-contained .NET runtime.');
  }
  const deps = JSON.parse(await readFile(path.join(backend, 'ServiceBusViewer.deps.json'), 'utf8'));
  if (!deps.runtimeTarget?.name?.endsWith(`/${target.rid}`)) {
    throw new Error('Backend runtime target does not match the requested native target.');
  }
}

// Electron's existing nativeImage API resizes the desktop logo; no additional image tool is required.
async function createIcon({ desktop, work, platform, run }) {
  if (platform === 'linux') return undefined; // Linux uses BrowserWindow's PNG icon.
  const helper = path.join(work, 'icon.cjs');
  const png = path.join(work, 'icon.png');
  await writeFile(helper, `const { app, nativeImage } = require('electron');
const fs = require('node:fs');
try {
  fs.writeFileSync(
    process.argv[3],
    nativeImage.createFromPath(process.argv[2]).resize({ width: 256, height: 256 }).toPNG(),
  );
  app.exit(0);
} catch (error) {
  console.error(error.message);
  app.exit(1);
}
`);
  const electronPackage = path.join(desktop, 'node_modules/electron/package.json');
  const electron = JSON.parse(await readFile(electronPackage, 'utf8'));
  if (!electron.version) {
    throw new Error('Local Electron installation is missing.');
  }
  const electronEntry = path.join(desktop, 'node_modules/electron/index.js');
  const electronPath = (await import(pathToFileURL(electronEntry).href)).default;
  const env = { ...process.env };
  delete env.ELECTRON_RUN_AS_NODE;
  await run(
    electronPath,
    [helper, path.join(desktop, 'assets/app-logo.png'), png],
    { cwd: desktop, env },
  );
  await requireFile(png);
  const image = await readFile(png);
  let icon;
  if (platform === 'win32') {
    // ICO permits a PNG-compressed 256px image (zero width/height represent 256).
    const header = Buffer.alloc(22);
    header.writeUInt16LE(1, 2);
    header.writeUInt16LE(1, 4);
    header.writeUInt16LE(1, 10);
    header.writeUInt16LE(32, 12);
    header.writeUInt32LE(image.length, 14);
    header.writeUInt32LE(22, 18);
    icon = path.join(work, 'app-logo.ico');
    await writeFile(icon, Buffer.concat([header, image]));
  } else {
    const header = Buffer.alloc(16);
    header.write('icns');
    header.writeUInt32BE(image.length + 16, 4);
    header.write('ic08', 8);
    header.writeUInt32BE(image.length + 8, 12);
    icon = path.join(work, 'app-logo.icns');
    await writeFile(icon, Buffer.concat([header, image]));
  }
  return icon;
}

export async function packageApplication({
  desktop = desktopDirectory,
  platform = process.platform,
  arch = process.arch,
  crossBuild = false,
  stage = stageRuntime,
  pack,
  icon = createIcon,
  run = runCommand,
} = {}) {
  const target = resolveRuntimeTarget(platform, arch);
  const metadata = JSON.parse(await readFile(path.join(desktop, 'package.json'), 'utf8'));
  const directoryName = bundleDirectoryName(metadata.version, platform, arch);
  const runtime = path.join(desktop, '.runtime');
  const out = path.join(desktop, 'out');
  await realDirectory(runtime);
  await realDirectory(out);
  const lock = path.join(runtime, '.package-lock');
  await mkdir(lock).catch(error => {
    if (error.code === 'EEXIST') {
      throw new Error('Desktop packaging is already running, or .runtime/.package-lock remains from an interrupted build.');
    }
    throw error;
  });
  const work = path.join(runtime, 'package');
  const destination = path.join(out, directoryName);
  try {
    // Remove the native target's previous delivery before starting, including on build failure.
    await rm(destination, { recursive: true, force: true });
    await rm(work, { recursive: true, force: true });
    await mkdir(work, { recursive: true });
    // Build a fresh backend/SPA pair for the requested platform.
    const staged = await stageBundleRuntime({
      desktop,
      work,
      platform,
      arch,
      crossBuild,
      stage,
      run,
    });
    const expectedBackend = crossBuild
      ? path.join(work, 'cross/desktop/.runtime/backend')
      : path.join(runtime, 'backend');
    if (staged.rid !== target.rid || path.resolve(staged.backend) !== expectedBackend) {
      throw new Error('Staging returned an unexpected target runtime.');
    }
    await validateBackend(staged.backend, target);
    if (crossBuild) {
      await validateLinuxExecutable(path.join(staged.backend, target.executable), arch);
    }
    // Publish can include ignored developer appsettings. Keep the base configuration,
    // but never distribute environment-specific local configuration or debug symbols.
    for (const name of await readdir(staged.backend)) {
      if (/^appsettings\..+\.json$/i.test(name) || /\.pdb$/i.test(name)) {
        await rm(path.join(staged.backend, name));
      }
    }

    // Check the installed version and target archive before assembling the shell.
    const localElectronPath = path.join(desktop, 'node_modules/electron/package.json');
    const localElectron = JSON.parse(await readFile(localElectronPath, 'utf8'));
    if (localElectron.version !== metadata.devDependencies.electron) {
      throw new Error('Installed Electron does not match the pinned desktop version.');
    }
    const checksumsPath = path.join(desktop, 'node_modules/electron/checksums.json');
    const checksums = JSON.parse(await readFile(checksumsPath, 'utf8'));
    if (!checksums[`electron-v${localElectron.version}-${platform}-${arch}.zip`]) {
      throw new Error('Pinned Electron has no checksum for the native target.');
    }
    const app = path.join(work, 'app');
    for (const file of applicationFiles) {
      await requireFile(path.join(desktop, file));
      await mkdir(path.dirname(path.join(app, file)), { recursive: true });
      await cp(path.join(desktop, file), path.join(app, file));
    }
    await writeFile(path.join(app, 'package.json'), JSON.stringify({
      name: metadata.name,
      version: metadata.version,
      type: metadata.type,
      main: metadata.main,
    }));
    const nativeIcon = await icon({ desktop, work, platform, run });
    const packager = pack ?? (await import('@electron/packager')).packager;
    const temporaryOut = path.join(work, 'out');
    await mkdir(path.join(work, 'downloads'), { recursive: true });
    // Keep packager downloads and temporary files inside the desktop-owned runtime tree.
    const outputs = await packager({
      dir: app,
      out: temporaryOut,
      tmpdir: path.join(work, 'tmp'),
      name: applicationName,
      platform,
      arch,
      electronVersion: metadata.devDependencies.electron,
      appVersion: metadata.version,
      download: {
        cacheRoot: path.join(runtime, 'electron-cache'),
        tempDirectory: path.join(work, 'downloads'),
        checksums,
      },
      asar: false,
      prune: false,
      overwrite: false,
      extraResource: [staged.backend],
      win32metadata: { CompanyName: 'Andrey Veselov', ProductName: 'Service Bus Viewer' },
      ...(nativeIcon ? { icon: nativeIcon } : {}),
    });

    // Validate the packaged copy before moving it into the final delivery directory.
    const expected = path.join(temporaryOut, `${applicationName}-${platform}-${arch}`);
    if (outputs.length !== 1 || path.resolve(outputs[0]) !== expected) {
      throw new Error('Packager did not produce the expected native bundle.');
    }
    const root = platform === 'darwin' ? path.join(expected, `${applicationName}.app/Contents`) : expected;
    await requireFile(path.join(root, platform === 'darwin'
      ? `MacOS/${applicationName}`
      : `${applicationName}${platform === 'win32' ? '.exe' : ''}`));
    const resources = path.join(root, platform === 'darwin' ? 'Resources' : 'resources');
    await validateBackend(path.join(resources, 'backend'), target);
    if (crossBuild) {
      await validateLinuxExecutable(path.join(root, applicationName), arch);
      await validateLinuxExecutable(path.join(resources, 'backend', target.executable), arch);
    }
    for (const file of [...applicationFiles, 'package.json']) {
      await requireFile(path.join(resources, 'app', file));
    }
    await rename(expected, destination);
    return { bundle: destination, rid: target.rid };
  } finally {
    await rm(work, { recursive: true, force: true });
    await rm(lock, { recursive: true, force: true });
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  Promise.resolve()
    .then(() => packageApplication(parseBundleArguments(process.argv.slice(2))))
    .then(({ bundle, rid }) => console.log(`Packaged ${rid} application at ${bundle}`))
    .catch(error => {
      console.error(`Desktop packaging failed: ${error.message}`);
      process.exitCode = 1;
    });
}
