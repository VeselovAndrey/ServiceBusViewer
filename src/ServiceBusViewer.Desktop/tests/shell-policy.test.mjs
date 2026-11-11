import test from 'node:test';
import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { isTrustedUrl, allowClipboard, protectContents } from '../electron/shell-policy.mjs';

const origin = 'http://127.0.0.1:5277';
const trusted = { owned: true, currentUrl: origin, requestingUrl: `${origin}/viewer`, permission: 'clipboard-read', isMainFrame: true };

test('only the literal stable HTTP application origin is trusted', () => {
  for (const url of [origin, `${origin}/`, `${origin}/viewer?q=yes#fragment`]) assert.equal(isTrustedUrl(url), true, url);
  for (const url of [undefined, null, {}, '', 'invalid', `${origin}.evil.test/`, 'http://localhost:5277', 'http://127.0.0.1:5278', 'http://127.0.0.1', 'https://127.0.0.1:5277', 'http://127.0.0.1:52770', 'http://user@127.0.0.1:5277', 'http://127.0.0.1:5277@evil.test', 'http://2130706433:5277', 'http://0x7f000001:5277', 'http://127.1:5277', 'file:///app', 'javascript:alert(1)', `blob:${origin}/id`, 'data:text/html,hello', ` ${origin}`, `${origin}\\@evil.test`]) assert.equal(isTrustedUrl(url), false, String(url));
});

test('only owned trusted main-frame clipboard workflows receive permission', () => {
  assert.equal(allowClipboard(trusted), true);
  assert.equal(allowClipboard({ ...trusted, permission: 'clipboard-sanitized-write', embeddingOrigin: origin }), true);
  for (const patch of [{ owned: false }, { owned: undefined }, { currentUrl: 'https://evil.test' }, { requestingUrl: 'https://evil.test' }, { requestingUrl: undefined }, { isMainFrame: false }, { isMainFrame: undefined }, { embeddingOrigin: 'https://evil.test' }, { permission: 'media' }, { permission: 'notifications' }, { permission: 'clipboard-write' }, { permission: 'deprecated-sync-clipboard-read' }, { permission: 'unknown' }]) assert.equal(allowClipboard({ ...trusted, ...patch }), false, JSON.stringify(patch));
});

test('navigation and redirect hooks reject every untrusted frame target; windows and webviews are denied', () => {
  const contents = new EventEmitter();
  contents.setWindowOpenHandler = handler => { contents.windowHandler = handler; };
  protectContents(contents);
  assert.deepEqual(contents.windowHandler({ url: origin }), { action: 'deny' });
  assert.deepEqual(contents.windowHandler({ url: 'https://evil.test' }), { action: 'deny' });
  let prevented = false;
  contents.emit('will-attach-webview', { preventDefault() { prevented = true; } });
  assert.equal(prevented, true);
  for (const name of ['will-navigate', 'will-frame-navigate', 'will-redirect']) {
    for (const url of [origin, `${origin}/viewer`, 'https://evil.test', 'file:///secret', 'http://127.0.0.1:5278']) {
      for (const modern of [false, true]) {
        let blocked = false;
        contents.emit(name, { ...(modern ? { url } : {}), preventDefault() { blocked = true; } }, url, false, false);
        assert.equal(blocked, !isTrustedUrl(url), `${name}: ${url}`);
      }
    }
  }
});
