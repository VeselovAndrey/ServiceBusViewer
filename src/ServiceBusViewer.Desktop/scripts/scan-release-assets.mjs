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
  pollAttempts = 1,
  requestInterval = Number(process.env.VIRUSTOTAL_REQUEST_INTERVAL_MS ?? 61000),
} = {}) {
  const invalidInterval = !Number.isFinite(requestInterval) || requestInterval < 61000;
  let nextRequest = 0;
  let quotaExceeded = false;
  const request = async (url, options = {}) => {
    const parsed = new URL(url);
    if (parsed.protocol !== 'https:' || !(parsed.hostname === 'virustotal.com' || parsed.hostname.endsWith('.virustotal.com'))) {
      throw new Error('VirusTotal returned an unexpected upload host.');
    }
    await sleep(Math.max(0, nextRequest - now()));
    nextRequest = now() + requestInterval;
    const response = await fetchImpl(url, {
      ...options,
      headers: { ...options.headers, 'x-apikey': apiKey },
      signal: AbortSignal.timeout(options.body ? 600000 : 60000),
    });
    if (!response.ok) {
      if (response.status === 429) quotaExceeded = true;
      throw new Error(`VirusTotal request failed (HTTP ${response.status}).`);
    }
    return response.json();
  };
  const results = [];
  for (const asset of assets) {
    const result = {
      name: asset.name, digest: asset.digest,
      url: `https://www.virustotal.com/gui/file/${asset.digest}`,
      checkedAt: new Date(now()).toISOString(), status: 'not-submitted',
    };
    if (!apiKey || quotaExceeded || invalidInterval) {
      result.detail = invalidInterval
        ? 'VirusTotal request interval must be at least 61000 ms; no submission was attempted.'
        : quotaExceeded
        ? 'VirusTotal quota/rate limit reached; no further submission was attempted.'
        : 'API key unavailable; report link is a hash lookup, not evidence of a scan.';
      results.push(result);
      continue;
    }
    try {
      const info = await stat(asset.path);
      if (info.size > 650000000) throw new Error('Asset exceeds the VirusTotal 650 MB upload limit.');
      const uploadUrl = info.size > 32000000
        ? (await request(`${apiRoot}/files/upload_url`)).data
        : `${apiRoot}/files`;
      const boundary = `sbv-${randomUUID()}`;
      const header = Buffer.from(`--${boundary}\r\nContent-Disposition: form-data; name="file"; filename="${asset.name}"\r\nContent-Type: application/octet-stream\r\n\r\n`);
      const footer = Buffer.from(`\r\n--${boundary}--\r\n`);
      const body = Readable.from((async function* () {
        yield header;
        yield* createReadStream(asset.path);
        yield footer;
      })());
      const submitted = await request(uploadUrl, {
        method: 'POST', body, duplex: 'half',
        headers: { 'Content-Type': `multipart/form-data; boundary=${boundary}`, 'Content-Length': String(header.length + info.size + footer.length) },
      });
      const analysisId = submitted.data?.id;
      if (!analysisId) throw new Error('VirusTotal returned no analysis identifier.');
      result.status = 'queued';
      for (let attempt = 0; attempt < pollAttempts; attempt++) {
        const analysis = await request(`${apiRoot}/analyses/${encodeURIComponent(analysisId)}`);
        result.status = analysis.data?.attributes?.status ?? 'unknown';
        if (result.status === 'completed') {
          if (analysis.meta?.file_info?.sha256 !== asset.digest) throw new Error('VirusTotal analysis hash does not match the release asset.');
          result.stats = analysis.data.attributes.stats;
          if (!result.stats || !Number.isInteger(result.stats.malicious) || !Number.isInteger(result.stats.suspicious)) {
            throw new Error('VirusTotal returned incomplete analysis statistics.');
          }
          break;
        }
        if (attempt + 1 < pollAttempts) await sleep(30000);
      }
      result.checkedAt = new Date(now()).toISOString();
    } catch (error) {
      result.status = 'error';
      // Never include request headers, response bodies, or API keys in release notes.
      result.detail = error.message.startsWith('VirusTotal') || error.message.startsWith('Asset exceeds')
        ? error.message : 'VirusTotal scanning could not be completed.';
    }
    results.push(result);
  }
  return results;
}

export function scanNotes(results) {
  return [
    '## VirusTotal reports',
    'Reports cover the exact downloadable packages. Archive scans may not inspect every embedded file; results are not a guarantee of safety. Scan findings do not block publication.',
    ...results.map(result => {
      const status = result.status === 'completed'
        ? `completed; ${result.stats.malicious} malicious, ${result.stats.suspicious} suspicious detections`
        : `${result.status}${result.detail ? `; ${result.detail}` : '; analysis has not completed'}`;
      return `- [${result.name}](${result.url}): ${status} (checked ${result.checkedAt}).`;
    }),
  ].join('\n');
}
