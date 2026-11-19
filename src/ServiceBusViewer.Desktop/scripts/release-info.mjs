import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const desktopDirectory = fileURLToPath(new URL('..', import.meta.url));

export function run(command, args, cwd) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { cwd, shell: false, stdio: ['ignore', 'pipe', 'pipe'] });
    let stdout = '';
    let stderr = '';
    child.stdout.setEncoding('utf8').on('data', chunk => { stdout += chunk; });
    child.stderr.setEncoding('utf8').on('data', chunk => { stderr += chunk; });
    child.once('error', reject);
    child.once('close', code => code === 0
      ? resolve(stdout.trim())
      : reject(new Error(`${path.basename(command)} failed (exit ${code}): ${stderr.trim()}`)));
  });
}

export function parseVersionTag(tag) {
  // Numeric prerelease identifiers cannot have leading zeroes, even though the
  // main expression also allows alphanumeric identifiers such as "rc01".
  const match = typeof tag === 'string' && /^v(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$/.exec(tag);
  if (!match || match[4]?.split('.').some(part => /^\d+$/.test(part) && !/^(0|[1-9]\d*)$/.test(part))) {
    throw new Error(`Invalid release tag: ${tag}. Expected v<semantic-version>.`);
  }

  return tag.slice(1);
}

export async function validateReleaseInfo({ tag, project, repository, command = run }) {
  const version = parseVersionTag(tag);
  const projectVersion = await command('dotnet', ['msbuild', project, '-getProperty:Version'], repository);
  if (projectVersion !== version) {
    throw new Error(`Release tag version ${version} does not match app project version ${projectVersion}.`);
  }

  // Peel annotated tags to their commit, then require checkout to match it.
  // This binds all downstream desktop and container jobs to the same source.
  const commit = await command('git', ['rev-parse', '--verify', `refs/tags/${tag}^{commit}`], repository);
  if (!/^[0-9a-f]{40,64}$/i.test(commit)) {
    throw new Error('Git returned an invalid tag commit.');
  }
  const head = await command('git', ['rev-parse', 'HEAD'], repository);
  if (head.toLowerCase() !== commit.toLowerCase()) {
    throw new Error('Checked out commit does not match the validated release tag.');
  }

  return { tag, version, commit };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const [tag, output = 'release-info.json'] = process.argv.slice(2);
  Promise.resolve()
    .then(() => validateReleaseInfo({
      tag,
      project: path.resolve(desktopDirectory, '../ServiceBusViewer/ServiceBusViewer.csproj'),
      repository: path.resolve(desktopDirectory, '../..'),
    }))
    .then(info => import('node:fs/promises').then(({ writeFile }) => writeFile(output, `${JSON.stringify(info, null, 2)}\n`)))
    .then(() => console.log(`Validated ${tag}.`))
    .catch(error => {
      console.error(error.message);
      process.exitCode = 1;
    });
}
