import test from 'node:test';
import assert from 'node:assert/strict';
import { parseVersionTag, validateReleaseInfo } from '../scripts/release-info.mjs';

test('parseVersionTag rejects malformed and non-semantic tags', () => {
  assert.throws(() => parseVersionTag('release-1.2.0'), /Invalid release tag/);
  assert.throws(() => parseVersionTag('v01.2.0'), /Invalid release tag/);
  assert.throws(() => parseVersionTag('v1.2.3-01'), /Invalid release tag/);
});

test('validateReleaseInfo rejects a project version mismatch before resolving a commit', async () => {
  const calls = [];
  await assert.rejects(validateReleaseInfo({
    tag: 'v1.2.1', project: 'project.csproj', repository: '.',
    command: async (...args) => { calls.push(args); return '1.2.0'; },
  }), /does not match app project version/);
  assert.equal(calls.length, 1);
});

test('validateReleaseInfo returns tag version and exact checked out commit', async () => {
  const sha = 'a'.repeat(40);
  let call = 0;
  const result = await validateReleaseInfo({
    tag: 'v1.2.0', project: 'project.csproj', repository: '.',
    command: async () => ['1.2.0', sha, sha][call++],
  });
  assert.deepEqual(result, { tag: 'v1.2.0', version: '1.2.0', commit: sha });
});

test('validateReleaseInfo rejects a checkout that differs from the tagged commit', async () => {
  let call = 0;
  await assert.rejects(validateReleaseInfo({
    tag: 'v1.2.0', project: 'project.csproj', repository: '.',
    command: async () => ['1.2.0', 'a'.repeat(40), 'b'.repeat(40)][call++],
  }), /does not match the validated release tag/);
});
