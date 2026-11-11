import { backendOrigin } from './backend-process.mjs';

export function isTrustedUrl(value) {
  if (typeof value !== 'string' || !/^http:\/\/127\.0\.0\.1:5277(?:[/?#]|$)/.test(value)) return false;
  try {
    const url = new URL(value);
    return url.origin === backendOrigin && !url.username && !url.password;
  } catch { return false; }
}

export function allowClipboard({ owned, currentUrl, permission, requestingUrl, embeddingOrigin, isMainFrame }) {
  return owned === true && isMainFrame === true
    && ['clipboard-read', 'clipboard-sanitized-write'].includes(permission)
    && isTrustedUrl(currentUrl) && isTrustedUrl(requestingUrl)
    && (embeddingOrigin === undefined || isTrustedUrl(embeddingOrigin));
}

export function protectContents(contents) {
  contents.setWindowOpenHandler(() => ({ action: 'deny' }));
  contents.on('will-attach-webview', event => event.preventDefault());
  for (const name of ['will-navigate', 'will-frame-navigate', 'will-redirect']) {
    contents.on(name, (event, url) => {
      if (!isTrustedUrl(event.url ?? url)) event.preventDefault();
    });
  }
}
