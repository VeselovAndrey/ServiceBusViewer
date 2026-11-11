# Service Bus Viewer desktop development

The desktop project stages the existing API and React application, starts an owned backend, and opens it in Electron. Installers and distributable package generation are handled separately.

Use Node.js 22.12.0 or newer compatible with the repository frontend, npm, and a .NET SDK supporting the backend target framework. Staging requires network access for any uncached .NET runtime packs. Install the existing frontend dependencies with `npm run web:install` from the repository root. In this directory, run:

```powershell
npm ci
npm exec -- install-electron
npm run stage
npm start
npm test
```

`npm ci` installs the pinned project-local Electron npm package. Electron 45.3.0 requires the separate `npm exec -- install-electron` step to download and extract its native runtime; this needs network access unless the download is cached. The equivalent explicit command is `node node_modules/electron/install.js`. `npm start` always stages before launching, so a failed build cannot open stale output. To launch already staged output for diagnostics, use `npm exec -- electron .` from this directory. No global Electron installation is required. Native Windows, Linux and macOS x64/arm64 staging selects the host runtime; cross-platform bundle verification belongs to the packaging work.

Development resolves `.runtime/backend` relative to the Electron source, independently of the working directory. Packaged execution resolves `backend` under Electron's `process.resourcesPath`; a future bundle must place the same staged native output there.

The shell waits for both `/` and `/api/session-state` before opening `http://127.0.0.1:5277`. The existing `/api` requests and session cookie remain on that origin. Port 5277 must be free; the shell never selects another port or attaches to an unrelated process. Existing backend environment settings still apply.

Readiness also requires the owned backend's standard `Microsoft.Hosting.Lifetime` information-level listening message. Inherited logging overrides that suppress this message cause a readiness timeout; remove those overrides before launch. Backend logs are never displayed by the shell.

Frontend preferences use the unchanged browser storage formats in the persistent `persist:servicebusviewer` Electron session under the application's user-data directory. Normal relaunch retains them. A second desktop invocation focuses the existing window. Closing the final window quits on Windows/Linux; on macOS the backend remains running and activation opens a new window. Explicit Quit cancels startup and waits for owned-backend cleanup.

The desktop also remembers normal window width and height in `window-size.json` under Electron's user-data directory. Resizing saves after a short pause and closing flushes pending changes. Relaunch centers the saved size on the current display, clamped to its available work area; a temporarily smaller display does not itself replace the saved preference. Position, minimized/maximized/fullscreen state and credentials are not stored there. Missing or invalid settings use the 1280×1600 default, subject to the same display limits. Settings write failures do not prevent shutdown.

The renderer has no Node integration or preload bridge and runs with isolation and sandboxing. Navigation and redirects remain within the exact trusted origin, new windows and webviews are denied, and clipboard read/write permissions are granted only to the owned trusted main frame. Other permission requests are denied. Missing runtime assets, occupied ports, startup timeouts, backend exits and renderer/load failures show fixed actionable messages without backend output or credentials, then clean up and exit.

For targeted policy validation, run `node --test tests/shell-policy.test.mjs`. Manual acceptance also requires a configured test broker: exercise the existing Connect/Viewer, browsing, peek/receive, send, sessions, dead-letter and clipboard flows; change a preference and relaunch; verify a second invocation focuses one instance; test occupied-port, missing-runtime, timeout and backend-exit failures. Verify macOS activation on macOS and native lifecycle on each target platform.
