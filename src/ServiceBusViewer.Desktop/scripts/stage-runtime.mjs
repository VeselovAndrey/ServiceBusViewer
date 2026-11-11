import { spawn } from 'node:child_process';
import { cp, lstat, mkdir, rename, rm, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolveRuntimeTarget } from './runtime-targets.mjs';

const desktopDirectory = fileURLToPath(new URL('..', import.meta.url));

export function runCommand(command, args, options) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { ...options, stdio: 'inherit', shell: false });
    child.once('error', reject);
    child.once('exit', (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`${path.basename(command)} failed (${signal ?? `exit ${code}`}).`));
    });
  });
}

async function requireFile(file) {
  const output = await stat(file).catch(() => null);
  if (!output?.isFile() || output.size === 0) {
    throw new Error(`Required staged output is missing or empty: ${file}`);
  }
}

export async function stageRuntime({ desktop = desktopDirectory, platform = process.platform, arch = process.arch, run = runCommand } = {}) {
  const runtime = path.join(desktop, '.runtime');
  // Never follow a substituted generated-output directory into source or user data.
  const existingRuntime = await lstat(runtime).catch(error => {
    if (error.code !== 'ENOENT') throw error;
    return null;
  });
  if (existingRuntime && (!existingRuntime.isDirectory() || existingRuntime.isSymbolicLink())) {
    throw new Error('Desktop .runtime must be a real directory. Remove the substituted path before staging.');
  }
  await mkdir(runtime, { recursive: true });
  const lock = path.join(runtime, '.stage-lock');
  await mkdir(lock).catch(error => {
    if (error.code === 'EEXIST') throw new Error('Desktop staging is already running, or .runtime/.stage-lock remains from an interrupted build.');
    throw error;
  });
  const backend = path.join(runtime, 'backend');
  const work = path.join(runtime, 'stage');
  try {
    await rm(backend, { recursive: true, force: true });
    await rm(work, { recursive: true, force: true });
    const target = resolveRuntimeTarget(platform, arch);
    const web = path.resolve(desktop, '../ServiceBusViewer.Web');
    const project = path.resolve(desktop, '../ServiceBusViewer/ServiceBusViewer.csproj');
    const spa = path.join(work, 'spa');
    const publish = path.join(work, 'backend');
    await mkdir(work, { recursive: true });
    const env = { ...process.env, VITE_API_BASE_PATH: '/api' };
    // Run the existing build's type checks separately so their incremental files stay desktop-owned.
    for (const config of ['app', 'node']) {
      await run(process.execPath, [path.join(web, 'node_modules/typescript/bin/tsc'), '-p', `tsconfig.${config}.json`, '--incremental', '--tsBuildInfoFile', path.join(work, `${config}.tsbuildinfo`)], { cwd: web, env });
    }
    await run(process.execPath, [path.join(web, 'node_modules/vite/bin/vite.js'), 'build', '--configLoader', 'runner', '--outDir', spa], { cwd: web, env });
    await requireFile(path.join(spa, 'index.html'));
    await run('dotnet', ['publish', project, '-c', 'Release', '-r', target.rid, '--self-contained', 'true', '-p:UseAppHost=true', '--artifacts-path', path.join(work, 'artifacts'), '-o', publish], { cwd: desktop, env });
    for (const file of [target.executable, target.coreRuntime, 'ServiceBusViewer.dll', 'ServiceBusViewer.deps.json', 'ServiceBusViewer.runtimeconfig.json']) {
      await requireFile(path.join(publish, file));
    }
    await cp(spa, path.join(publish, 'wwwroot'), { recursive: true });
    await requireFile(path.join(publish, 'wwwroot/index.html'));
    await rename(publish, backend);
    return { backend, ...target };
  } catch (error) {
    await rm(backend, { recursive: true, force: true });
    throw error;
  } finally {
    await rm(work, { recursive: true, force: true });
    await rm(lock, { recursive: true, force: true });
  }
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  stageRuntime().then(({ backend, rid }) => console.log(`Staged ${rid} runtime at ${backend}`)).catch(error => {
    console.error(`Desktop staging failed: ${error.message}`);
    process.exitCode = 1;
  });
}
