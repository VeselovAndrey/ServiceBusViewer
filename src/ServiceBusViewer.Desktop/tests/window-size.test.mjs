import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import test from 'node:test';
import { clampWindowSize, createWindowSizeStore, defaultWindowSize } from '../electron/window-size.mjs';

function fixture(t) {
  const root = path.resolve(import.meta.dirname, '../../../.agents/.tmp/desktop-window-size-tests');
  mkdirSync(root, { recursive: true });
  const directory = mkdtempSync(path.join(root, 'size-'));
  t.after(() => rmSync(directory, { recursive: true, force: true }));
  return { directory, file: path.join(directory, 'window-size.json'), store: createWindowSizeStore(directory) };
}

test('normal dimensions roundtrip, latest pending resize flushes on immediate close', t => {
  const { directory, file, store } = fixture(t);
  store.remember({ width: 1400, height: 1200, x: 42 });
  store.remember({ width: 1500, height: 1300 });
  store.flush();
  assert.deepEqual(JSON.parse(readFileSync(file, 'utf8')), { width: 1500, height: 1300 });
  assert.deepEqual(createWindowSizeStore(directory).load(), { width: 1500, height: 1300 });
});

test('missing, corrupt and invalid settings safely fall back', t => {
  const { file, store } = fixture(t);
  assert.deepEqual(store.load(), defaultWindowSize);
  for (const contents of ['broken json', 'null', '[]', '{}', '{"width":0,"height":900}', '{"width":800,"height":-5}', '{"width":"800","height":900}', '{"width":800.5,"height":900}', '{"width":1e100,"height":900}']) {
    writeFileSync(file, contents);
    assert.deepEqual(store.load(), defaultWindowSize);
  }
});

test('smaller display clamps restored dimensions without rewriting the preference', t => {
  const { file, store } = fixture(t);
  writeFileSync(file, '{"width":1800,"height":1600}');
  assert.deepEqual(clampWindowSize(store.load(), { width: 1000, height: 700 }), { width: 1000, height: 700 });
  store.flush();
  assert.deepEqual(store.load(), { width: 1800, height: 1600 });
});

test('settings write failure leaves previous preference intact and never throws', t => {
  const { file, store } = fixture(t);
  writeFileSync(file, '{"width":1400,"height":1100}');
  mkdirSync(`${file}.tmp`);
  store.remember({ width: 1600, height: 1200 });
  assert.doesNotThrow(() => store.flush());
  assert.deepEqual(store.load(), { width: 1400, height: 1100 });
});
