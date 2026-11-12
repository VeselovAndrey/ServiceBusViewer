import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdtemp, mkdir, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { inspectReleaseArchive, publishRelease, releaseNotes } from '../scripts/publish-release.mjs';

const version = '1.2.0';
const tag = `v${version}`;
const commit = 'a'.repeat(40);
const imageReference = 'ghcr.io/veselovandrey/servicebusviewer:1.2.0';
const imageDigest = `sha256:${'b'.repeat(64)}`;
const repository = 'veselovandrey/ServiceBusViewer';
const targets = [
  { platform: 'win32', arch: 'x64', extension: 'zip' },
  { platform: 'linux', arch: 'x64', extension: 'tar.gz' },
];

async function fixture() {
  const artifactDirectory = await mkdtemp(path.join(os.tmpdir(), 'desktop-publish-'));
  for (const { platform, arch, extension } of targets) {
    const directory = path.join(artifactDirectory, `${platform}-${arch}`);
    await mkdir(directory);
    const name = `ServiceBusViewer-${version}-${platform}-${arch}.${extension}`;
    await writeFile(path.join(directory, name), `archive:${platform}:${arch}`);
    await writeFile(path.join(directory, 'verification.json'), JSON.stringify({ tag, version, commit, platform, arch }));
  }
  return artifactDirectory;
}

function digest(fileContents) {
  return createHash('sha256').update(fileContents).digest('hex');
}

function client({ existing = null, assets: initialAssets = existing?.assets ?? [], failUpload = false } = {}) {
  let release = existing;
  let assets = [...initialAssets];
  let uploadCount = 0;
  let publishCount = 0;
  return {
    get state() { return { release, assets, uploadCount, publishCount }; },
    async getRelease() { return release; },
    async createDraft({ tag: releaseTag, target, title, body, prerelease }) {
      release = { id: 7, tag_name: releaseTag, target_commitish: target, name: title, body, prerelease, draft: true, html_url: 'https://example.test/release' };
      return release;
    },
    async listAssets() { return [...assets]; },
    async uploadAsset(_id, file, name) {
      uploadCount += 1;
      if (failUpload) throw new Error('simulated upload failure');
      const platform = targets.find(item => name.includes(`-${item.platform}-${item.arch}.`));
      const content = `archive:${platform.platform}:${platform.arch}`;
      assets.push({ name, digest: `sha256:${digest(content)}` });
    },
    async publishDraft() {
      publishCount += 1;
      release = { ...release, draft: false };
      return release;
    },
  };
}

function options(artifactDirectory, github, overrides = {}) {
  return {
    tag, version, commit, imageReference, imageDigest, repository, artifactDirectory, github,
    inspectArchive: async () => {},
    ...overrides,
  };
}

test('publisher refuses to proceed when a Windows or Linux archive is missing', async () => {
  const artifactDirectory = await fixture();
  const github = client();
  try {
    await rm(path.join(artifactDirectory, 'linux-x64', `ServiceBusViewer-${version}-linux-x64.tar.gz`));
    await assert.rejects(publishRelease(options(artifactDirectory, github)), /Missing release archive/);
    assert.equal(github.state.release, null);
  } finally { await rm(artifactDirectory, { recursive: true, force: true }); }
});

test('archive inspection preserves member paths containing spaces', async () => {
  const root = 'ServiceBusViewer-1.2.0-linux-x64';
  const members = [
    `${root}/ServiceBusViewer`,
    `${root}/resources/backend/ServiceBusViewer`,
    `${root}/resources/app/package.json`,
    `${root}/resources/app/first launch notes.txt`,
  ];
  const verbose = [
    `-rwxr-xr-x root/root 3 2026-11-12 00:00 ${members[0]}`,
    `-rwxr-xr-x root/root 7 2026-11-12 00:00 ${members[1]}`,
    `-rw-r--r-- root/root 2 2026-11-12 00:00 ${members[2]}`,
    `-rw-r--r-- root/root 3 2026-11-12 00:00 ${members[3]}`,
  ].join('\n');
  await inspectReleaseArchive('fixture.tar.gz', { platform: 'linux', arch: 'x64' }, version, async (_command, args) =>
    args[0] === '-tzf' ? members.join('\n') : verbose);
});

test('publisher rejects a mismatched container identity already recorded on the draft', async () => {
  const artifactDirectory = await fixture();
  const wrongDigest = `sha256:${'c'.repeat(64)}`;
  const existing = {
    id: 7, tag_name: tag, target_commitish: commit, draft: true,
    body: releaseNotes({ tag, version, commit, imageReference, imageDigest: wrongDigest, repository }), assets: [],
  };
  try {
    await assert.rejects(publishRelease(options(artifactDirectory, client({ existing }))), /different release identity evidence/);
  } finally { await rm(artifactDirectory, { recursive: true, force: true }); }
});

test('failed asset upload leaves the release as a draft', async () => {
  const artifactDirectory = await fixture();
  const github = client({ failUpload: true });
  try {
    await assert.rejects(publishRelease(options(artifactDirectory, github)), /simulated upload failure/);
    assert.equal(github.state.release?.draft, true);
    assert.equal(github.state.publishCount, 0);
  } finally { await rm(artifactDirectory, { recursive: true, force: true }); }
});

test('same-identity rerun reuses matching assets and publishes a recoverable draft', async () => {
  const artifactDirectory = await fixture();
  const github = client();
  try {
    await publishRelease(options(artifactDirectory, github));
    const firstUploads = github.state.uploadCount;
    assert.equal(firstUploads, 2);
    const rerun = client({ existing: { ...github.state.release, draft: true }, assets: github.state.assets });
    const result = await publishRelease(options(artifactDirectory, rerun));
    assert.equal(result.published, true);
    assert.equal(rerun.state.uploadCount, 0);
    assert.equal(rerun.state.publishCount, 1);
  } finally { await rm(artifactDirectory, { recursive: true, force: true }); }
});

test('published release refuses to replace an asset with different bytes', async () => {
  const artifactDirectory = await fixture();
  const expectedNames = targets.map(({ platform, arch, extension }) => `ServiceBusViewer-${version}-${platform}-${arch}.${extension}`);
  const existing = {
    id: 7, tag_name: tag, target_commitish: commit, draft: false,
    body: releaseNotes({ tag, version, commit, imageReference, imageDigest, repository }),
    assets: expectedNames.map((name, index) => ({ name, digest: `sha256:${index === 0 ? '0'.repeat(64) : 'a'.repeat(64)}` })),
  };
  try {
    const github = client({ existing });
    await assert.rejects(publishRelease(options(artifactDirectory, github)), /refusing to replace it/);
    assert.equal(github.state.uploadCount, 0);
    assert.equal(github.state.publishCount, 0);
  } finally { await rm(artifactDirectory, { recursive: true, force: true }); }
});

