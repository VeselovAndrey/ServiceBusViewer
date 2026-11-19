import { createReadStream } from 'node:fs';
import { stat } from 'node:fs/promises';
import { Readable } from 'node:stream';
import { randomUUID } from 'node:crypto';

const apiRoot = 'https://www.virustotal.com/api/v3';
const delay = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

// Reports describe the exact downloadable bytes, not every executable inside an archive.
export async function scanReleaseAssets(assets, {
  apiKey = process.env.VIRUSTOTAL_API_KEY,
  fetchImpl = fetch,
  sleep = delay,
  now = Date.now,
  pollAttempts = 30,
  fileTimeout = 15 * 60 * 1000,
  requestInterval = Number(process.env.VIRUSTOTAL_REQUEST_INTERVAL_MS ?? 61000),
} = {}) {
  const invalidInterval = !Number.isFinite(requestInterval) || requestInterval < 61000;
  let nextRequest = 0;
  let quotaExceeded = false;
  let previousAttempted = false;
  let deadline = Infinity;

  // Quota waits and network calls share the current file's deadline. Waiting for
  // the next API slot must not extend a file's 15-minute budget.
  const wait = async milliseconds => {
    const remaining = deadline - now();
    if (remaining <= 0) {
      throw new Error('VirusTotal scan reached its 15-minute deadline.');
    }

    await sleep(Math.min(milliseconds, remaining));

    if (now() >= deadline) {
      throw new Error('VirusTotal scan reached its 15-minute deadline.');
    }
  };

  const request = async (url, options = {}) => {
    const parsed = new URL(url);
    const isVirusTotalHost = parsed.hostname === 'virustotal.com'
      || parsed.hostname.endsWith('.virustotal.com');

    // The large-file endpoint returns a separate upload URL. Validate its host
    // before attaching credentials to that request.
    if (parsed.protocol !== 'https:' || !isVirusTotalHost) {
      throw new Error('VirusTotal returned an unexpected upload host.');
    }

    await wait(Math.max(0, nextRequest - now()));
    nextRequest = now() + requestInterval;

    const requestTimeout = options.body ? 600000 : 60000;
    const remainingTime = deadline - now();
    const response = await fetchImpl(url, {
      ...options,
      headers: { ...options.headers, 'x-apikey': apiKey },
      signal: AbortSignal.timeout(Math.max(1, Math.min(requestTimeout, remainingTime))),
    });

    if (!response.ok) {
      if (response.status === 429) {
        quotaExceeded = true;
      }
      throw new Error(`VirusTotal request failed (HTTP ${response.status}).`);
    }

    return response.json();
  };

  const results = [];
  for (const asset of assets) {
    const result = {
      name: asset.name,
      digest: asset.digest,
      url: `https://www.virustotal.com/gui/file/${asset.digest}`,
      checkedAt: new Date(now()).toISOString(),
      status: 'not-submitted',
    };

    // A quota failure applies to the account, so avoid submitting later files.
    // Ordinary file errors and analysis timeouts are handled per file below.
    if (!apiKey || quotaExceeded || invalidInterval) {
      if (invalidInterval) {
        result.detail = 'VirusTotal request interval must be at least 61000 ms; no submission was attempted.';
      } else if (quotaExceeded) {
        result.detail = 'VirusTotal quota/rate limit reached; no further submission was attempted.';
      } else {
        result.detail = 'API key unavailable; report link is a hash lookup, not evidence of a scan.';
      }

      results.push(result);
      continue;
    }

    // The inter-file pause is separate from both the quota spacing and the next
    // file's deadline. Start a new budget only after that pause has finished.
    if (previousAttempted) {
      await sleep(15000);
    }
    deadline = now() + fileTimeout;

    try {
      const info = await stat(asset.path);
      if (info.size > 650000000) {
        throw new Error('Asset exceeds the VirusTotal 650 MB upload limit.');
      }

      const uploadUrl = info.size > 32000000
        ? (await request(`${apiRoot}/files/upload_url`)).data
        : `${apiRoot}/files`;
      const boundary = `sbv-${randomUUID()}`;
      const header = Buffer.from(
        `--${boundary}\r\n`
        + `Content-Disposition: form-data; name="file"; filename="${asset.name}"\r\n`
        + 'Content-Type: application/octet-stream\r\n\r\n',
      );
      const footer = Buffer.from(`\r\n--${boundary}--\r\n`);

      // Stream large packages without loading the complete multipart body into
      // memory. Content-Length includes the framing bytes as well as the file.
      const body = Readable.from((async function* () {
        yield header;
        yield* createReadStream(asset.path);
        yield footer;
      })());
      const submitted = await request(uploadUrl, {
        method: 'POST',
        body,
        duplex: 'half',
        headers: {
          'Content-Type': `multipart/form-data; boundary=${boundary}`,
          'Content-Length': String(header.length + info.size + footer.length),
        },
      });
      const analysisId = submitted.data?.id;
      if (!analysisId) {
        throw new Error('VirusTotal returned no analysis identifier.');
      }

      // Finish polling this analysis before the outer loop submits another file.
      result.status = 'queued';
      for (let attempt = 0; attempt < pollAttempts; attempt++) {
        const analysis = await request(`${apiRoot}/analyses/${encodeURIComponent(analysisId)}`);
        result.status = analysis.data?.attributes?.status ?? 'unknown';
        if (result.status === 'completed') {
          // A successful response is relevant only if it describes these bytes.
          if (analysis.meta?.file_info?.sha256 !== asset.digest) {
            throw new Error('VirusTotal analysis hash does not match the release asset.');
          }

          result.stats = analysis.data.attributes.stats;
          if (!result.stats
            || !Number.isInteger(result.stats.malicious)
            || !Number.isInteger(result.stats.suspicious)) {
            throw new Error('VirusTotal returned incomplete analysis statistics.');
          }
          break;
        }
        if (attempt + 1 < pollAttempts) {
          await wait(requestInterval);
        }
      }

      if (result.status !== 'completed') {
        result.status = 'timed-out';
        result.detail = 'VirusTotal analysis did not complete within the polling limit.';
      }
      result.checkedAt = new Date(now()).toISOString();
    } catch (error) {
      result.status = now() >= deadline ? 'timed-out' : 'error';

      // Never include request headers, response bodies, or API keys in release notes.
      result.detail = error.message.startsWith('VirusTotal') || error.message.startsWith('Asset exceeds')
        ? error.message : 'VirusTotal scanning could not be completed.';
    }

    previousAttempted = true;
    results.push(result);
  }

  return results;
}

export function scanNotes(results) {
  return [
    '## VirusTotal reports',
    'Reports cover the exact downloadable packages. Archive scans may not inspect every embedded file; results are not a guarantee of safety. Scan findings do not block publication.',
    ...results.map(result => `- [${result.name}](${result.url})`),
  ].join('\n');
}
