import { spawn } from 'node:child_process';
import { access } from 'node:fs/promises';
import { constants } from 'node:fs';
import http from 'node:http';
import net from 'node:net';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { resolveRuntimeTarget } from '../scripts/runtime-targets.mjs';

export const backendOrigin = 'http://127.0.0.1:5277';

export function resolveBackendDirectory({ isPackaged = false, resourcesPath = process.resourcesPath } = {}) {
  return isPackaged
    ? path.join(resourcesPath, 'backend')
    : path.resolve(fileURLToPath(new URL('../.runtime/backend/', import.meta.url)));
}

function checkPort() {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.once('error', () => reject(new Error('Desktop backend port 5277 is unavailable. Close the application using it and retry.')));
    server.listen({ host: '127.0.0.1', port: 5277, exclusive: true }, () => server.close(resolve));
  });
}

function probe(route, signal) {
  return new Promise((resolve) => {
    const request = http.get(`${backendOrigin}${route}`, { signal, agent: false }, (response) => {
      response.resume();
      response.once('end', () => resolve(response.statusCode === 200));
      response.once('error', () => resolve(false));
    });
    request.once('error', () => resolve(false));
  });
}

// Dependencies are private test seams; the Electron caller supplies only path options.
export function createBackendProcess(options = {}, dependencies = {}) {
  const launch = dependencies.spawn ?? spawn;
  const available = dependencies.checkPort ?? checkPort;
  const request = dependencies.probe ?? probe;
  const timeoutMs = dependencies.timeoutMs ?? 30_000;
  const stopTimeoutMs = dependencies.stopTimeoutMs ?? 2_000;
  const controller = new AbortController();
  let child;
  let closed;
  let closeChild;
  let startup;
  let stopping;
  let failure;
  let ready = false;
  let listening = false;
  const listeners = new Set();

  function fail(error) {
    failure ??= error;
    controller.abort();
    if (ready) {
      ready = false;
      for (const listener of listeners) listener(failure);
    }
  }

  async function waitForClose(milliseconds) {
    let timer;
    try {
      return await Promise.race([closed.then(() => true), new Promise((resolve) => { timer = setTimeout(() => resolve(false), milliseconds); })]);
    } finally {
      clearTimeout(timer);
    }
  }

  function stop() {
    if (stopping) return stopping;
    ready = false;
    fail(new Error('Desktop backend startup was cancelled.'));
    stopping = (async () => {
      if (!child) return;
      if (child.exitCode !== null || child.signalCode !== null) {
        if (!await waitForClose(stopTimeoutMs)) throw new Error('Desktop backend exited but did not finish closing. Close this desktop instance and retry.');
        return;
      }
      child.kill('SIGTERM');
      if (await waitForClose(stopTimeoutMs)) return;
      child.kill('SIGKILL');
      if (!await waitForClose(stopTimeoutMs)) {
        throw new Error('The owned desktop backend did not stop. Close this desktop instance and retry.');
      }
    })();
    return stopping;
  }

  function start() {
    if (startup) return startup;
    startup = (async () => {
      const timer = setTimeout(() => fail(new Error('Desktop backend did not become ready within 30 seconds. Retry or restage the desktop runtime.')), timeoutMs);
      try {
        const directory = resolveBackendDirectory(options);
        const executable = path.join(directory, resolveRuntimeTarget().executable);
        try { await access(executable, constants.X_OK); } catch {
          throw new Error('Desktop backend executable is missing or inaccessible. Run npm run stage in the desktop project and retry.');
        }
        if (failure) throw failure;
        await available();
        if (failure) throw failure;
        const environment = Object.fromEntries(Object.entries(process.env).filter(([key]) => key.toUpperCase() !== 'SERVICEBUSVIEWER_RUNNING_IN_CONTAINER'));
        try {
          child = launch(executable, ['--urls', backendOrigin], { cwd: directory, env: environment, shell: false, windowsHide: true, stdio: ['ignore', 'pipe', 'ignore'] });
        } catch {
          throw new Error('Desktop backend could not be started. Restage the native runtime and check executable permissions.');
        }
        closed = new Promise((resolve) => { closeChild = resolve; });
        // Only accept HTTP after this child reports its successful bind. A competing
        // listener cannot supply this signal. Never retain or display backend logs.
        const bindMessage = `Now listening on: ${backendOrigin}`;
        let matchedCharacters = 0;
        child.stdout.setEncoding('utf8');
        child.stdout.on('data', (chunk) => {
          for (const character of chunk) {
            matchedCharacters = character === bindMessage[matchedCharacters]
              ? matchedCharacters + 1 : character === bindMessage[0] ? 1 : 0;
            if (matchedCharacters === bindMessage.length) {
              listening = true;
              matchedCharacters = 0;
            }
          }
        });
        child.once('error', () => {
          fail(new Error('Desktop backend could not be started. Restage the native runtime and check executable permissions.'));
          closeChild();
        });
        child.once('exit', () => fail(new Error('Desktop backend exited unexpectedly. Check port 5277 availability and restage the native runtime.')));
        child.once('close', closeChild);
        while (!failure) {
          if (listening && await request('/', controller.signal) && await request('/api/session-state', controller.signal)) {
            if (!failure && child.exitCode === null && child.signalCode === null) {
              ready = true;
              return backendOrigin;
            }
          }
          if (!failure) await new Promise((resolve) => setTimeout(resolve, 100));
        }
        throw failure;
      } catch (error) {
        fail(error);
        await stop();
        throw failure;
      } finally {
        clearTimeout(timer);
      }
    })();
    return startup;
  }

  return { start, stop, cancel: stop, onFailure(listener) { listeners.add(listener); return () => listeners.delete(listener); } };
}
