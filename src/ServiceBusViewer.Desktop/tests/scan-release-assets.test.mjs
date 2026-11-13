import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdir, mkdtemp, rm, truncate, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { scanReleaseAssets, scanNotes } from '../scripts/scan-release-assets.mjs';

const root = fileURLToPath(new URL('../../../.agents/.tmp/release-scans/', import.meta.url));
async function fixture(t) {
  await mkdir(root, { recursive: true });
  const directory = await mkdtemp(path.join(root, 'scan-'));
  t.after(() => rm(directory, { recursive: true, force: true }));
  const file = path.join(directory, 'bundle.zip');
  await writeFile(file, 'fixture bytes');
  return { name: 'bundle.zip', path: file, digest: createHash('sha256').update('fixture bytes').digest('hex') };
}
const response = data => ({ ok: true, json: async () => data });

test('each file gets a 15-minute deadline and timed-out scans allow the next upload', async t => {
  const asset = await fixture(t);
  let clock = 0;
  const uploads = [];
  const results = await scanReleaseAssets([asset, { ...asset, name: 'second.deb' }], {
    apiKey: 'test', now: () => clock, sleep: async ms => { clock += ms; },
    fetchImpl: async (_url, options) => {
      if (options.method === 'POST') {
        uploads.push(clock);
        return response({ data: { id: 'pending' } });
      }
      return response({ data: { attributes: { status: 'queued' } } });
    },
  });
  assert.deepEqual(uploads, [0, 915000]);
  assert.equal(clock, 1815000);
  assert.deepEqual(results.map(result => result.status), ['timed-out', 'timed-out']);
});

test('next file waits for completed analysis and a 15-second inter-file pause', async t => {
  const asset = await fixture(t);
  let clock = 0;
  const events = [];
  let analyses = 0;
  await scanReleaseAssets([asset, { ...asset, name: 'second.deb' }], {
    apiKey: 'test', now: () => clock,
    sleep: async ms => { events.push(`wait:${ms}`); clock += ms; },
    fetchImpl: async (_url, options) => {
      if (options.method === 'POST') {
        events.push('upload');
        return response({ data: { id: 'analysis' } });
      }
      analyses++;
      events.push(analyses === 1 ? 'queued' : 'completed');
      return response({ data: { attributes: { status: analyses === 1 ? 'queued' : 'completed', stats: { malicious: 0, suspicious: 0 } } }, meta: { file_info: { sha256: asset.digest } } });
    },
  });
  assert.equal(events.filter(event => event === 'upload').length, 2);
  const firstCompleted = events.indexOf('completed');
  const interFilePause = events.indexOf('wait:15000');
  const secondUpload = events.lastIndexOf('upload');
  assert.ok(firstCompleted > events.indexOf('queued'));
  assert.ok(interFilePause > firstCompleted && interFilePause < secondUpload);
});

test('missing key produces honest lookup links without API calls', async () => {
  const [result] = await scanReleaseAssets([{ name: 'bundle.zip', digest: 'a'.repeat(64) }], {
    apiKey: '', fetchImpl: () => assert.fail('Unexpected API call'),
  });
  assert.equal(result.status, 'not-submitted');
  assert.doesNotMatch(scanNotes([result]), /not-submitted|checked /);
});

test('submission streams exact bytes and requests are spaced for one lookup per minute', async t => {
  const asset = await fixture(t);
  let clock = 0;
  const times = [];
  const results = await scanReleaseAssets([asset], {
    apiKey: 'test-secret', now: () => clock, sleep: async ms => { clock += ms; },
    fetchImpl: async (url, options) => {
      times.push(clock);
      assert.equal(options.headers['x-apikey'], 'test-secret');
      if (options.method === 'POST') {
        const chunks = [];
        for await (const chunk of options.body) chunks.push(Buffer.from(chunk));
        assert.match(Buffer.concat(chunks).toString(), /fixture bytes/);
        return response({ data: { id: 'analysis-1' } });
      }
      assert.match(url, /analyses\/analysis-1$/);
      return response({ data: { attributes: { status: 'completed', stats: { malicious: 1, suspicious: 0 } } }, meta: { file_info: { sha256: asset.digest } } });
    },
  });
  assert.deepEqual(times, [0, 61000]);
  assert.equal(results[0].status, 'completed');
  assert.doesNotMatch(scanNotes(results), /1 malicious|completed;|checked /);
  assert.doesNotMatch(scanNotes(results), /test-secret/);
});

test('pending analysis reaches the polling bound and continues with the next file', async t => {
  const asset = await fixture(t);
  let calls = 0;
  const [result, skipped] = await scanReleaseAssets([asset, { ...asset, name: 'second.deb' }], {
    apiKey: 'test', sleep: async () => {}, pollAttempts: 2,
    fetchImpl: async (_url, options) => {
      calls++;
      if (options.method === 'POST') return response({ data: { id: 'pending' } });
      return response({ data: { attributes: { status: 'queued' } } });
    },
  });
  assert.equal(calls, 6);
  assert.equal(result.status, 'timed-out');
  assert.equal(skipped.status, 'timed-out');
  assert.doesNotMatch(scanNotes([result]), /queued/);
});

test('quota exhaustion stops subsequent file submissions and does not throw', async t => {
  const asset = await fixture(t);
  let calls = 0;
  const results = await scanReleaseAssets([asset, { ...asset, name: 'second.deb' }], {
    apiKey: 'test', sleep: async () => {},
    fetchImpl: async () => { calls++; return { ok: false, status: 429 }; },
  });
  assert.equal(calls, 1);
  assert.equal(results[0].status, 'error');
  assert.equal(results[1].status, 'not-submitted');
  assert.match(results[1].detail, /quota/);
});

test('analysis for different bytes is rejected rather than called completed', async t => {
  const asset = await fixture(t);
  const [result] = await scanReleaseAssets([asset], {
    apiKey: 'test', sleep: async () => {},
    fetchImpl: async (_url, options) => options.method === 'POST'
      ? response({ data: { id: 'wrong' } })
      : response({ data: { attributes: { status: 'completed' } }, meta: { file_info: { sha256: '0'.repeat(64) } } }),
  });
  assert.equal(result.status, 'error');
  assert.match(result.detail, /hash does not match/);
});

test('large-file upload URLs are validated before sending API credentials', async t => {
  const asset = await fixture(t);
  await truncate(asset.path, 32000001);
  let calls = 0;
  const [result] = await scanReleaseAssets([asset], {
    apiKey: 'test-secret', sleep: async () => {},
    fetchImpl: async url => {
      calls++;
      assert.match(url, /files\/upload_url$/);
      return response({ data: 'https://unexpected.example/upload' });
    },
  });
  assert.equal(calls, 1);
  assert.equal(result.status, 'error');
  assert.match(result.detail, /unexpected upload host/);
});

test('invalid rate configuration skips scans without blocking publication', async () => {
  const [result] = await scanReleaseAssets([{ name: 'bundle.zip', digest: 'a'.repeat(64) }], {
    apiKey: 'test', requestInterval: 0, fetchImpl: () => assert.fail('Unexpected request'),
  });
  assert.equal(result.status, 'not-submitted');
  assert.match(result.detail, /61000 ms/);
});
