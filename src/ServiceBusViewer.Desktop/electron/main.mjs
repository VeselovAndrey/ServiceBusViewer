import { app, BrowserWindow, dialog, screen, session } from 'electron';
import { fileURLToPath } from 'node:url';
import { createBackendProcess, backendOrigin } from './backend-process.mjs';
import { allowClipboard, isTrustedUrl, protectContents } from './shell-policy.mjs';
import { clampWindowSize, createWindowSizeStore } from './window-size.mjs';

const applicationIcon = fileURLToPath(new URL('../assets/app-logo.png', import.meta.url));

let window;
let backend;
let ready = false;
let quitting = false;
let mayQuit = false;
let cleanup;
let failed = false;
let windowSizeStore;

function stopBackend() {
  cleanup ??= backend?.stop() ?? Promise.resolve();
  return cleanup;
}

async function quit() {
  if (quitting) return;
  quitting = true;
  windowSizeStore?.flush();
  try { await stopBackend(); }
  catch {
    dialog.showErrorBox('Service Bus Viewer could not stop', 'The owned backend did not finish stopping. Close this desktop instance and check port 5277 before retrying.');
  } finally {
    mayQuit = true;
    app.quit();
  }
}

async function fail(stage) {
  if (failed || quitting) return;
  failed = true;
  ready = false;
  // Never display exception text, process output, URLs or connection settings.
  window?.destroy();
  window = undefined;
  try { await stopBackend(); } catch { /* quit reports cleanup failures below */ }
  dialog.showErrorBox('Service Bus Viewer could not continue', stage === 'backend'
    ? 'The local backend could not start or stopped unexpectedly. Check that port 5277 is free, run npm run stage in the desktop project, and retry. Startup must complete within 30 seconds.'
    : 'The application window could not load or its renderer stopped. Close the desktop application, run npm run stage in the desktop project, and retry.');
  await quit();
}

async function openWindow() {
  if (quitting || failed || !ready || window) return;
  const workArea = screen.getDisplayNearestPoint(screen.getCursorScreenPoint()).workArea;
  const { width, height } = clampWindowSize(windowSizeStore.load(), workArea);
  const created = new BrowserWindow({
    width, height, show: false, icon: applicationIcon,
    x: workArea.x + Math.floor((workArea.width - width) / 2),
    y: workArea.y + Math.floor((workArea.height - height) / 2),
    webPreferences: {
      partition: 'persist:servicebusviewer', nodeIntegration: false,
      contextIsolation: true, sandbox: true, webviewTag: false,
    },
  });
  window = created;
  let observedSize = { width, height };
  created.on('resize', () => {
    if (created.isMinimized() || created.isMaximized() || created.isFullScreen()) return;
    const size = created.getNormalBounds();
    if (size.width === observedSize.width && size.height === observedSize.height) return;
    observedSize = { width: size.width, height: size.height };
    windowSizeStore.remember(observedSize);
  });
  created.on('close', () => windowSizeStore.flush());
  protectContents(created.webContents);
  created.on('closed', () => { if (window === created) window = undefined; });
  created.webContents.on('render-process-gone', () => { void fail('window'); });
  created.webContents.on('did-fail-load', (_event, code, _description, _url, isMainFrame) => {
    if (isMainFrame && code !== -3) void fail('window');
  });
  try {
    await created.loadURL(backendOrigin);
    if (!quitting && !failed && !created.isDestroyed()) created.show();
  } catch {
    // Closing during loading is ordinary shutdown, including macOS last-window close.
    if (!created.isDestroyed()) await fail('window');
  }
}

if (!app.requestSingleInstanceLock()) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (quitting || failed) return;
    if (window && !window.isDestroyed()) {
      if (window.isMinimized()) window.restore();
      window.show();
      window.focus();
    } else { void openWindow(); }
  });
  app.on('before-quit', event => {
    if (!mayQuit) { event.preventDefault(); void quit(); }
  });
  app.on('window-all-closed', () => { if (process.platform !== 'darwin' && !failed) void quit(); });
  app.on('activate', () => { void openWindow(); });
  app.whenReady().then(async () => {
    if (quitting) return;
    windowSizeStore = createWindowSizeStore(app.getPath('userData'));
    if (process.platform === 'darwin') app.dock.setIcon(applicationIcon);
    const persistentSession = session.fromPartition('persist:servicebusviewer');
    const permitted = (contents, permission, requestingUrl, details) => allowClipboard({
      owned: Boolean(window && !window.isDestroyed() && contents === window.webContents),
      currentUrl: contents && !contents.isDestroyed() ? contents.getURL() : '',
      permission, requestingUrl, embeddingOrigin: details.embeddingOrigin,
      isMainFrame: details.isMainFrame,
    });
    persistentSession.setPermissionCheckHandler((contents, permission, origin, details) =>
      isTrustedUrl(origin) && permitted(contents, permission, details.requestingUrl, details));
    persistentSession.setPermissionRequestHandler((contents, permission, callback, details) =>
      callback(permitted(contents, permission, details.requestingUrl, details)));
    backend = createBackendProcess({ isPackaged: app.isPackaged, resourcesPath: process.resourcesPath });
    backend.onFailure(() => { void fail('backend'); });
    try {
      await backend.start();
      if (quitting || failed) return;
      ready = true;
      await openWindow();
    } catch { await fail('backend'); }
  }).catch(() => { void fail('window'); });
}
