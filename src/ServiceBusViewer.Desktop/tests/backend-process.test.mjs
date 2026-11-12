import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { spawn } from 'node:child_process';
import { mkdir, mkdtemp, writeFile, rm } from 'node:fs/promises';
import path from 'node:path';
import net from 'node:net';
import test from 'node:test';
import { createBackendProcess, resolveBackendDirectory, backendOrigin } from '../electron/backend-process.mjs';
import { resolveRuntimeTarget } from '../scripts/runtime-targets.mjs';

const temporaryRoot = path.resolve(import.meta.dirname, '../../../.agents/.tmp');

async function fixture(t) {
  await mkdir(temporaryRoot, { recursive: true });
  const resourcesPath = await mkdtemp(path.join(temporaryRoot, 'backend-lifecycle-'));
  const directory = path.join(resourcesPath, 'backend');
  await mkdir(directory);
  await writeFile(path.join(directory, resolveRuntimeTarget().executable), '', { mode: 0o755 });
  t.after(() => rm(resourcesPath, { recursive: true, force: true }));
  const child = new EventEmitter();
  child.exitCode = null;
  child.signalCode = null;
  child.kills = [];
  child.stdout = new EventEmitter();
  child.stdout.setEncoding = () => {};
  child.kill = (signal) => {
    child.kills.push(signal);
    child.signalCode = signal;
    queueMicrotask(() => { child.emit('exit', null, signal); child.emit('close', null, signal); });
    return true;
  };
  const calls = [];
  const dependencies = { spawn: (...args) => { calls.push(args); queueMicrotask(() => child.stdout.emit('data', `Now listening on: ${backendOrigin}\n`)); return child; }, checkPort: async () => {}, probe: async () => true, timeoutMs: 500, stopTimeoutMs: 30 };
  return { options: { isPackaged: true, resourcesPath }, dependencies, child, calls, directory };
}

test('runtime paths are absolute and independent of working directory', () => {
  assert.equal(resolveBackendDirectory({ isPackaged: true, resourcesPath: path.resolve('resources') }), path.resolve('resources/backend'));
  assert.equal(resolveBackendDirectory(), path.resolve(import.meta.dirname, '../.runtime/backend'));
});

test('launches native executable directly and stops only its child once', async (t) => {
  const f = await fixture(t);
  const oldMarker = process.env.SERVICEBUSVIEWER_RUNNING_IN_CONTAINER;
  process.env.SERVICEBUSVIEWER_RUNNING_IN_CONTAINER = 'true';
  t.after(() => { if (oldMarker === undefined) delete process.env.SERVICEBUSVIEWER_RUNNING_IN_CONTAINER; else process.env.SERVICEBUSVIEWER_RUNNING_IN_CONTAINER = oldMarker; });
  const owner = createBackendProcess(f.options, f.dependencies);
  t.after(() => owner.stop());
  assert.equal(await owner.start(), backendOrigin);
  const [executable, args, options] = f.calls[0];
  assert.equal(executable, path.join(f.directory, resolveRuntimeTarget().executable));
  assert.deepEqual(args, ['--urls', backendOrigin]);
  assert.equal(options.cwd, f.directory);
  assert.equal(options.shell, false);
  assert.equal(options.env.SERVICEBUSVIEWER_RUNNING_IN_CONTAINER, undefined);
  assert.deepEqual(options.stdio, ['ignore', 'pipe', 'ignore']);
  await Promise.all([owner.stop(), owner.stop(), owner.cancel()]);
  assert.deepEqual(f.child.kills, ['SIGTERM']);
});

test('inherited hosting log filters cannot suppress the owned bind announcement', async (t) => {
  const f = await fixture(t);
  const inherited = {
    'logging__loglevel__microsoft.hosting.lifetime': 'Warning',
    'Logging__Console__LogLevel__Microsoft.Hosting.Lifetime': 'None',
    'Logging__LogLevel__Default': 'Error',
  };
  for (const [key, value] of Object.entries(inherited)) {
    const previous = process.env[key];
    process.env[key] = value;
    t.after(() => {
      if (previous === undefined) delete process.env[key];
      else process.env[key] = previous;
    });
  }
  f.dependencies.spawn = (executable, args, options) => {
    const levels = Object.fromEntries(Object.entries(options.env).map(([key, value]) => [key.toUpperCase(), value]));
    assert.equal(levels['LOGGING__LOGLEVEL__MICROSOFT.HOSTING.LIFETIME'], 'Information');
    assert.equal(levels['LOGGING__CONSOLE__LOGLEVEL__MICROSOFT.HOSTING.LIFETIME'], 'Information');
    assert.equal(levels.LOGGING__LOGLEVEL__DEFAULT, 'Error');
    queueMicrotask(() => f.child.stdout.emit('data', `Now listening on: ${backendOrigin}\n`));
    return f.child;
  };
  const owner = createBackendProcess(f.options, f.dependencies);
  t.after(() => owner.stop());
  assert.equal(await owner.start(), backendOrigin);
  assert.equal(process.env['logging__loglevel__microsoft.hosting.lifetime'], 'Warning');
});

test('missing executable gives actionable safe failure without spawning', async (t) => {
  const f = await fixture(t);
  await rm(path.join(f.directory, resolveRuntimeTarget().executable));
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /missing or inaccessible.*npm run stage/);
  assert.equal(f.calls.length, 0);
});

test('spawn error hides raw diagnostics', async (t) => {
  const f = await fixture(t);
  f.dependencies.spawn = () => { throw new Error('secret credential'); };
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), (error) => /could not be started/.test(error.message) && !error.message.includes('secret'));
});

test('asynchronous spawn error is handled', async (t) => {
  const f = await fixture(t);
  f.dependencies.probe = async () => { f.child.emit('error', new Error('secret')); return false; };
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /could not be started/);
});

test('bind race cannot accept competing HTTP listener while owned child fails', async (t) => {
  const f = await fixture(t);
  f.dependencies.spawn = () => {
    setTimeout(() => { f.child.exitCode = 1; f.child.emit('exit', 1); f.child.emit('close', 1); }, 150);
    return f.child;
  };
  f.dependencies.probe = async () => { assert.fail('must not probe unrelated service before owned bind'); };
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /exited unexpectedly/);
  assert.deepEqual(f.child.kills, []);
});

test('readiness timeout aborts an in-flight HTTP request and reaps owned process', async (t) => {
  const f = await fixture(t);
  f.dependencies.timeoutMs = 150;
  f.dependencies.probe = (_, signal) => new Promise((resolve) => signal.addEventListener('abort', () => resolve(false), { once: true }));
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /within 30 seconds/);
  assert.deepEqual(f.child.kills, ['SIGTERM']);
});

test('cancellation during readiness is idempotent and reaps child', async (t) => {
  const f = await fixture(t);
  let probing;
  const reachedProbe = new Promise((resolve) => { probing = resolve; });
  f.dependencies.probe = (_, signal) => { probing(); return new Promise((resolve) => signal.addEventListener('abort', () => resolve(false), { once: true })); };
  const owner = createBackendProcess(f.options, f.dependencies);
  const started = owner.start();
  const rejection = assert.rejects(started, /cancelled/);
  await reachedProbe;
  await Promise.all([owner.cancel(), owner.stop()]);
  await rejection;
  assert.deepEqual(f.child.kills, ['SIGTERM']);
});

test('occupied real port rejects without spawning or stopping unrelated listener', async (t) => {
  const f = await fixture(t);
  const server = net.createServer();
  await new Promise((resolve, reject) => { server.once('error', reject); server.listen(5277, '127.0.0.1', resolve); });
  t.after(() => new Promise((resolve) => server.close(resolve)));
  delete f.dependencies.checkPort;
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /port 5277 is unavailable/);
  assert.equal(f.calls.length, 0);
  assert.equal(server.listening, true);
});

test('unexpected exit after readiness notifies caller without raw logs', async (t) => {
  const f = await fixture(t);
  const owner = createBackendProcess(f.options, f.dependencies);
  const failures = [];
  owner.onFailure((error) => failures.push(error.message));
  await owner.start();
  f.child.exitCode = 1;
  f.child.emit('exit', 1);
  f.child.emit('close', 1);
  assert.match(failures[0], /exited unexpectedly/);
  await owner.stop();
});

test('both SPA and session-state must succeed', async (t) => {
  const f = await fixture(t);
  const routes = [];
  f.dependencies.timeoutMs = 150;
  f.dependencies.probe = async (route) => { routes.push(route); return route === '/'; };
  await assert.rejects(createBackendProcess(f.options, f.dependencies).start(), /within 30 seconds/);
  assert.ok(routes.includes('/api/session-state'));
  assert.deepEqual(f.child.kills, ['SIGTERM']);
});

test('stop before startup prevents a later launch', async (t) => {
  const f = await fixture(t);
  const owner = createBackendProcess(f.options, f.dependencies);
  await owner.stop();
  await assert.rejects(owner.start(), /cancelled/);
  assert.equal(f.calls.length, 0);
});

test('shutdown escalates only the owned child and waits for close', async (t) => {
  const f = await fixture(t);
  f.child.kill = (signal) => {
    f.child.kills.push(signal);
    if (signal === 'SIGKILL') queueMicrotask(() => { f.child.signalCode = signal; f.child.emit('exit', null, signal); f.child.emit('close', null, signal); });
    return true;
  };
  const owner = createBackendProcess(f.options, f.dependencies);
  const failures = [];
  owner.onFailure((error) => failures.push(error));
  await owner.start();
  await owner.stop();
  assert.deepEqual(f.child.kills, ['SIGTERM', 'SIGKILL']);
  assert.deepEqual(failures, []);
});

test('real HTTP fixture is stopped and reaped after readiness', async (t) => {
  const f = await fixture(t);
  let actualChild;
  const owner = createBackendProcess(f.options, {
    spawn: () => {
      actualChild = spawn(process.execPath, ['--input-type=module', '-e', `import http from 'node:http'; http.createServer((request,response)=>{response.end(request.url === '/' ? '<html>SPA</html>' : '{}');}).listen(5277,'127.0.0.1',()=>console.log('Now listening on: ${backendOrigin}'));`], { stdio: ['ignore', 'pipe', 'ignore'], windowsHide: true });
      return actualChild;
    },
  });
  t.after(() => owner.stop());
  assert.equal(await owner.start(), backendOrigin);
  await owner.stop();
  assert.ok(actualChild.exitCode !== null || actualChild.signalCode !== null);
  const server = net.createServer();
  await new Promise((resolve,reject) => { server.once('error',reject); server.listen(5277,'127.0.0.1',resolve); });
  await new Promise((resolve) => server.close(resolve));
});
