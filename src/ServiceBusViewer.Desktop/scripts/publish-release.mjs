import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { access, readdir, readFile } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseVersionTag } from './release-info.mjs';
import { validateDebContents } from './package-deb.mjs';
import { scanReleaseAssets, scanNotes } from './scan-release-assets.mjs';

const targets = [
  { platform: 'win32', arch: 'x64', extension: 'zip' },
  { platform: 'linux', arch: 'x64', extension: 'tar.gz' },
  { platform: 'linux', arch: 'x64', extension: 'deb' },
];

export function previousReleaseTag(releases, version) {
  const compare = (left, right) => {
    const a = left.split('.').map(BigInt);
    const b = right.split('.').map(BigInt);
    for (let index = 0; index < 3; index++) {
      if (a[index] !== b[index]) return a[index] < b[index] ? -1 : 1;
    }
    return 0;
  };
  return releases.filter(release => !release.draft && !release.prerelease && /^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/.test(release.tag_name))
    .map(release => release.tag_name)
    .filter(tag => compare(tag.slice(1), version) < 0)
    .sort((a, b) => compare(b.slice(1), a.slice(1)))[0];
}

export function releaseNotes({ tag, version, commit, imageReference, imageDigest, repository, generatedNotes = '', scans = [] }) {
  const sourceUrl = `https://github.com/${repository}/commit/${commit}`;
  return [
    ...(generatedNotes.trim() ? [generatedNotes.trim(), ''] : []),
    `<!-- servicebusviewer-release-identity version=${version} tag=${tag} commit=${commit} image=${imageReference} digest=${imageDigest} -->`,
    `Service Bus Viewer **${version}** was built from [${commit}](${sourceUrl}).`,
    '',
    '## Downloads',
    ...targets.map(({ platform, arch, extension }) => `- ${platform}/${arch}${extension === 'deb' ? ' (Ubuntu 24.04 installer)' : ''}: \`ServiceBusViewer-${version}-${platform}-${arch}.${extension}\``),
    ...(scans.length ? ['', scanNotes(scans)] : []),
    '',
    '## Container image',
    `- Versioned image: \`${imageReference}\``,
    `- Immutable digest: \`${imageDigest}\``,
    `- Pull this release: \`docker pull ${imageReference.split(':').slice(0, -1).join(':')}@${imageDigest}\``,
    '',
    `Source tag: [${tag}](https://github.com/${repository}/releases/tag/${tag})`,
  ].join('\n');
}

export async function prepareReleaseAssets({ artifactDirectory, version, tag, commit, inspectArchive = inspectReleaseArchive }) {
  if (parseVersionTag(tag) !== version) throw new Error(`Release tag ${tag} does not match version ${version}.`);
  if (!/^[0-9a-f]{40,64}$/i.test(commit)) throw new Error('Source commit must be a full Git commit SHA.');
  const assets = [];
  for (const target of targets) {
    const { platform, arch, extension } = target;
    const name = `ServiceBusViewer-${version}-${platform}-${arch}.${extension}`;
    const directory = path.join(artifactDirectory, `${platform}-${arch}`);
    const file = path.join(directory, name);
    const metadataFile = path.join(directory, 'verification.json');
    await Promise.all([access(file), access(metadataFile)]).catch(() => {
      throw new Error(`Missing release archive or verification metadata for ${platform}/${arch}.`);
    });
    const files = await readdir(directory);
    const expectedFiles = targets.filter(item => item.platform === platform && item.arch === arch)
      .map(item => `ServiceBusViewer-${version}-${platform}-${arch}.${item.extension}`).concat('verification.json');
    if (files.length !== expectedFiles.length || expectedFiles.some(expected => !files.includes(expected))) {
      throw new Error(`Unexpected files in release artifact set for ${platform}/${arch}.`);
    }
    const metadata = JSON.parse(await readFile(metadataFile, 'utf8'));
    if (metadata.tag !== tag || metadata.version !== version || metadata.commit !== commit || metadata.platform !== platform || metadata.arch !== arch) {
      throw new Error(`Release artifact identity mismatch for ${platform}/${arch}.`);
    }
    await inspectArchive(file, target, version);
    assets.push({ name, path: file, digest: await sha256(file) });
  }
  return assets;
}

export async function publishRelease({
  tag, version, commit, imageReference, imageDigest, repository, artifactDirectory,
  github, inspectArchive = inspectReleaseArchive, scanAssets = scanReleaseAssets,
}) {
  if (parseVersionTag(tag) !== version) throw new Error(`Release tag ${tag} does not match version ${version}.`);
  if (!/^[0-9a-f]{40,64}$/i.test(commit)) throw new Error('Source commit must be a full Git commit SHA.');
  if (!/^ghcr\.io\/[a-z0-9_.-]+\/[a-z0-9_.-]+:[^:@\s]+$/i.test(imageReference) || !imageReference.endsWith(`:${version}`)) {
    throw new Error('Container image reference must be the versioned GHCR image for this release.');
  }
  if (!/^sha256:[0-9a-f]{64}$/.test(imageDigest)) throw new Error('Container image digest must be an immutable sha256 digest.');
  const assets = await prepareReleaseAssets({ artifactDirectory, version, tag, commit, inspectArchive });
  const existingRelease = await github.getRelease(tag);
  const generatedNotes = existingRelease ? '' : (await github.generateNotes({ tag, commit })).body;
  const scans = existingRelease ? [] : await scanAssets(assets);
  const body = releaseNotes({ tag, version, commit, imageReference, imageDigest, repository, generatedNotes, scans });
  const release = existingRelease ?? await github.createDraft({
    tag, target: commit, title: `Service Bus Viewer ${version}`, body,
    prerelease: version.split('+', 1)[0].includes('-'),
  });
  if (release.tag_name !== tag || release.target_commitish !== commit) {
    throw new Error(`Existing release ${tag} points to a different tag or source commit.`);
  }
  if (!identityMatches(release.body, { version, tag, commit, imageReference, imageDigest })) {
    throw new Error(`Existing release ${tag} has different release identity evidence.`);
  }
  const existingAssets = await github.listAssets(release.id);
  const expectedNames = new Set(assets.map(asset => asset.name));
  const unexpected = existingAssets.find(asset => !expectedNames.has(asset.name));
  if (unexpected) throw new Error(`Existing release contains unexpected asset ${unexpected.name}; refusing to publish an incomplete or mixed release.`);
  const existingByName = new Map(existingAssets.map(asset => [asset.name, asset]));
  for (const asset of assets) {
    const existing = existingByName.get(asset.name);
    if (existing && normalizeDigest(existing.digest) !== asset.digest) {
      throw new Error(`Existing release asset ${asset.name} differs from this verified build; refusing to replace it.`);
    }
  }
  const missing = assets.filter(asset => !existingByName.has(asset.name));
  if (!release.draft && missing.length) throw new Error('Published releases are immutable; refusing to upload missing assets.');
  for (const asset of missing) await github.uploadAsset(release.id, asset.path, asset.name);
  const finalAssets = await github.listAssets(release.id);
  if (finalAssets.length !== assets.length) throw new Error('Release asset set does not match the three verified desktop downloads.');
  const finalByName = new Map(finalAssets.map(asset => [asset.name, asset]));
  for (const asset of assets) {
    if (normalizeDigest(finalByName.get(asset.name)?.digest) !== asset.digest) {
      throw new Error(`Uploaded release asset ${asset.name} failed identity verification.`);
    }
  }
  if (!release.draft) return { published: true, releaseUrl: release.html_url, reused: true };
  const published = await github.publishDraft(release.id);
  if (published.draft) throw new Error('GitHub did not publish the verified release draft.');
  return { published: true, releaseUrl: published.html_url, reused: false };
}

function identityMatches(body, expected) {
  const marker = `<!-- servicebusviewer-release-identity version=${expected.version} tag=${expected.tag} commit=${expected.commit} image=${expected.imageReference} digest=${expected.imageDigest} -->`;
  return typeof body === 'string' && body.includes(marker);
}

function normalizeDigest(value) {
  return typeof value === 'string' ? value.replace(/^sha256:/, '').toLowerCase() : '';
}

async function sha256(file) {
  const hash = createHash('sha256');
  for await (const chunk of createReadStream(file)) hash.update(chunk);
  return hash.digest('hex');
}

export async function inspectReleaseArchive(file, { platform, arch, extension }, version, runCommand = commandOutput) {
  if (!targets.some(target => target.platform === platform && target.arch === arch)) {
    throw new Error(`Unsupported release archive target: ${platform}/${arch}.`);
  }
  if (extension === 'deb') {
    for (const [field, expected] of [['Package', 'servicebusviewer'], ['Version', version], ['Architecture', 'amd64']]) {
      const actual = (await runCommand('dpkg-deb', ['-f', file, field])).trim();
      if (actual !== expected) {
        throw new Error(`DEB ${field} does not match the release identity: expected ${expected}, got ${actual}`);
      }
    }
    validateDebContents(await runCommand('dpkg-deb', ['--contents', file]));
    return;
  }
  const root = `ServiceBusViewer-${version}-${platform}-${arch}`;
  const listing = platform === 'win32'
    ? await runCommand('unzip', ['-Z1', file])
    : await runCommand('tar', ['-tzf', file]);
  const names = listing.split(/\r?\n/).filter(Boolean);
  const required = [
    `${root}/ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`,
    `${root}/resources/backend/ServiceBusViewer${platform === 'win32' ? '.exe' : ''}`,
    `${root}/resources/app/package.json`,
  ];
  const outsideRoot = names.filter(name => !name.startsWith(`${root}/`));
  if (outsideRoot.length) {
    throw new Error(`Archive ${path.basename(file)} contains entries outside its expected root: ${outsideRoot.slice(0, 5).join(', ')}`);
  }
  for (const name of required) if (!names.includes(name)) throw new Error(`Archive ${path.basename(file)} is missing ${name}.`);
  if (platform !== 'win32') {
    const verboseListing = await runCommand('tar', ['-tvzf', file]);
    const entries = verboseListing.split(/\r?\n/).filter(Boolean);
    for (const name of required.slice(0, 2)) {
      const entry = entries.find(line => line.endsWith(` ${name}`));
      if (!entry || !entry.startsWith('-rwx')) throw new Error(`Archive executable ${name} does not preserve executable mode bits.`);
    }
  }
}

function commandOutput(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { shell: false, stdio: ['ignore', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';
    child.stdout.setEncoding('utf8').on('data', chunk => { stdout += chunk; });
    child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });
    child.once('error', reject);
    child.once('close', code => code === 0 ? resolve(stdout.trim()) : reject(new Error(`${command} failed (exit ${code}): ${stderr.trim()}`)));
  });
}

function createGithubClient({ repository }) {
  const api = async (endpoint, args = []) => {
    const result = await commandOutput('gh', ['api', endpoint, ...args]);
    return result ? JSON.parse(result) : {};
  };
  return {
    async generateNotes({ tag, commit }) {
      const pages = await api(`repos/${repository}/releases?per_page=100`, ['--paginate', '--slurp']);
      const previousTag = previousReleaseTag(pages.flat(), parseVersionTag(tag));
      return api(`repos/${repository}/releases/generate-notes`, [
        '--method', 'POST', '-f', `tag_name=${tag}`, '-f', `target_commitish=${commit}`,
        ...(previousTag ? ['-f', `previous_tag_name=${previousTag}`] : []),
      ]);
    },
    async getRelease(tag) {
      try { return await api(`repos/${repository}/releases/tags/${encodeURIComponent(tag)}`); }
      catch (error) {
        if (/HTTP 404|Not Found/i.test(error.message)) return null;
        throw error;
      }
    },
    createDraft({ tag, target, title, body, prerelease }) {
      return api(`repos/${repository}/releases`, [
        '--method', 'POST', '-f', `tag_name=${tag}`, '-f', `target_commitish=${target}`,
        '-f', `name=${title}`, '-f', `body=${body}`, '-F', 'draft=true',
        '-F', `prerelease=${prerelease}`, '-f', 'make_latest=false',
      ]);
    },
    listAssets(releaseId) { return api(`repos/${repository}/releases/${releaseId}/assets?per_page=100`); },
    async uploadAsset(_releaseId, file, name) {
      await commandOutput('gh', ['release', 'upload', process.env.RELEASE_TAG, file, '--repo', repository]);
      return name;
    },
    publishDraft(releaseId) {
      return api(`repos/${repository}/releases/${releaseId}`, ['--method', 'PATCH', '-F', 'draft=false']);
    },
  };
}

async function main() {
  const values = {
    tag: process.env.RELEASE_TAG,
    version: process.env.RELEASE_VERSION,
    commit: process.env.SOURCE_SHA,
    imageReference: process.env.IMAGE_REFERENCE,
    imageDigest: process.env.IMAGE_DIGEST,
    repository: process.env.GITHUB_REPOSITORY,
    artifactDirectory: process.env.RELEASE_ARTIFACTS,
  };
  if (!values.repository || !values.artifactDirectory) throw new Error('GITHUB_REPOSITORY and RELEASE_ARTIFACTS are required.');
  const result = await publishRelease({ ...values, github: createGithubClient(values) });
  console.log(`${result.reused ? 'Verified existing' : 'Published'} release ${values.tag}: ${result.releaseUrl}`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => { console.error(error.message); process.exitCode = 1; });
}
