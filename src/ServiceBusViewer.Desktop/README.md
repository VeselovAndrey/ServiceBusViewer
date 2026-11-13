# Service Bus Viewer Desktop

Electron packages the existing React app with a self-contained .NET backend. Bundles need a graphical desktop and native OS libraries, but no checkout, Node.js or installed .NET runtime.

## Build a bundle

Current supported bundle targets are Windows, Linux and macOS. Build natively with matching x64 or arm64 Node.js. Windows can also cross-build Linux.

**Requirements:** Node.js/npm compatible with the locked dependencies, the .NET SDK targeted by the backend, and network access for uncached dependencies or runtime downloads. From the repository root:

```sh
npm run web:install
cd src/ServiceBusViewer.Desktop
npm ci
npm exec -- install-electron
npm run bundle
```

Dependency versions are managed in `package.json` and `package-lock.json`. Versioned bundles go to `out/`:

| Target | Bundle directory | Launch file |
| --- | --- | --- |
| Windows | `ServiceBusViewer-<version>-win32-<arch>` | `ServiceBusViewer.exe` |
| Linux | `ServiceBusViewer-<version>-linux-<arch>` | `ServiceBusViewer` |
| macOS | `ServiceBusViewer-<version>-darwin-arm64` | `ServiceBusViewer.app` |

The Windows backend is one self-contained `ServiceBusViewer.exe`. .NET extracts native runtime libraries under `%TEMP%\.net` at launch; the compiled UI remains in `resources/backend/wwwroot`.

Copy the whole bundle to a matching machine, preserving Linux executable modes and symlinks. Unsupported targets or versions fail. A build replaces only its target directory and removes it on failure; verify success before distribution.

### Create a Windows ZIP

From `src/ServiceBusViewer.Desktop`, build and archive the Windows bundle:

```powershell
npm run bundle
$version = (Get-Content package.json -Raw | ConvertFrom-Json).version
$arch = "x64" # Set to "arm64" for an arm64 build.
$bundle = "out/ServiceBusViewer-$version-win32-$arch"
Compress-Archive -LiteralPath $bundle -DestinationPath "$bundle.zip" -Force
```

The archive is written to `out/ServiceBusViewer-<version>-win32-<arch>.zip`.

### Build Linux on Windows

From `src/ServiceBusViewer.Desktop`, build and archive Linux x64:

```powershell
npm run bundle -- --platform=linux --arch=x64
$version = (Get-Content package.json -Raw | ConvertFrom-Json).version
$bundle = "out/ServiceBusViewer-$version-linux-x64"
Compress-Archive -LiteralPath $bundle -DestinationPath "$bundle.zip" -Force
```

Use `--arch=arm64` for arm64. Windows-to-Linux is the only supported cross-build; it needs no WSL or extra tools. Validate the bundle on Linux before distribution.

After extracting a Windows-created ZIP on Linux, restore permissions and launch as a regular user:

```sh
chmod u+x ./ServiceBusViewer ./resources/backend/ServiceBusViewer ./chrome-sandbox ./chrome_crashpad_handler
./ServiceBusViewer
```

Keep Electron's sandbox enabled; do not run the app as root.

## Ubuntu DEB installer

The only installer target is Ubuntu 24.04 amd64 (x64). Run `npm run package:deb` on Ubuntu with `dpkg-deb` and Bash, or on Windows with the existing `Ubuntu-24.04` WSL distribution. It writes `out/ServiceBusViewer-<version>-linux-x64.deb` and checks package metadata and permissions; it installs no tools and changes no system configuration.

Open the DEB in Ubuntu's installer or install it from a terminal (replace `VERSION` with the filename's version):

```sh
sudo apt install ./ServiceBusViewer-VERSION-linux-x64.deb
```

Launch **Service Bus Viewer** from the application menu or run `servicebusviewer`. It installs under `/opt/servicebusviewer`; preferences stay in the user profile. The installer applies Electron's required sandbox permission without changing global settings. The app needs port 5277, broker access and Ubuntu's Electron/.NET libraries. Other Ubuntu releases, Debian derivatives and arm64 are unvalidated installer targets.

## Update the app

There is no in-app updater; update manually:

- **Windows/Linux bundles:** download the matching OS/architecture bundle, close the app, extract and launch the complete new bundle. Remove the old folder after confirming the new one works. Preferences remain in the user profile.
- **Ubuntu DEB:** close the app and install the newer DEB with the command above; apt upgrades the app and retains preferences. Remove it with `sudo apt remove servicebusviewer`.

For a source release, update the desktop version in `package.json` and its lockfile, then rerun the matching build command. Output names derive from package metadata.

## Development

From `src/ServiceBusViewer.Desktop`:

```sh
npm ci
npm exec -- install-electron
npm run stage
npm start
npm test
```

`npm start` stages the app before launch. For diagnostics with staged output, run `npm exec -- electron .`.

The app waits for its backend before showing the UI. API requests stay same-origin, renderer isolation and sandboxing remain enabled, and preferences/window size persist. A second launch focuses the existing window; quitting stops the owned backend.

## Native requirements and troubleshooting

Use an OS supported by both Electron and .NET. See [Electron support](https://github.com/electron/electron#platform-support) and [.NET Linux support](https://learn.microsoft.com/en-us/dotnet/core/install/linux). Linux needs a graphical session plus GTK, NSS, X11/GBM, audio, .NET, timezone, certificate and crypto libraries; see the [.NET Ubuntu dependencies](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install#dependencies). `ldd` can help locate missing libraries but cannot verify GUI startup.

Bundles are unsigned; OS security policy or missing libraries may block launch. Keep system protections enabled. For startup problems, check port 5277 and permissions, then restore the whole bundle or rebuild on a supported host.

## Versioned GitHub Releases

Pushing a tag such as `v1.2.0` starts the coordinated release workflow. The tag version must match the existing Service Bus Viewer project version. The workflow builds from the commit named by the tag and publishes these downloads after both native builds and the matching container image succeed:

| Platform | Download |
| --- | --- |
| Windows x64 | `ServiceBusViewer-<version>-win32-x64.zip` |
| Linux x64 | `ServiceBusViewer-<version>-linux-x64.tar.gz` |
| Ubuntu 24.04 amd64 installer | `ServiceBusViewer-<version>-linux-x64.deb` |

Each release identifies the exact source commit and the versioned GHCR image digest. Pull the image using the immutable command shown in the release notes, for example `docker pull ghcr.io/veselovandrey/servicebusviewer@sha256:<digest>`; `latest` is not the release identity.

Release publication uses a draft until all three downloads and their source/image identity checks succeed. Linux runs `npm run package:deb` once, then archives the same generated bundle. The publisher checks Debian package name/version/architecture, ownership, permissions and required files using `dpkg-deb`. Installer releases currently require stable versions (`X.Y.Z`); prerelease/build identifiers are rejected by the existing Debian packager.

Release notes start with GitHub-generated changes and a comparison link, followed by source identity, downloads, VirusTotal reports and container identity. Add a repository Actions secret named `VIRUSTOTAL_API_KEY` to enable submissions. Only the three release packages are submitted; reports identify their SHA-256 hashes. Package scans may not inspect every embedded executable. Uploaded files are submitted to VirusTotal's public scanning service, so use an account/API plan appropriate for this public release workflow.

VirusTotal calls run sequentially, at least 61 seconds apart, including uploads and polling. The optional repository variable `VIRUSTOTAL_REQUEST_INTERVAL_MS` can increase that interval for stricter quotas. Each asset receives at most one analysis-status lookup after submission to conserve monthly lookup quota; pending reports remain available through their links. Upload URL requests and uploads may have additional account-specific quota costs. An HTTP 429 stops further submissions for this release. Missing credentials, API errors and pending analyses are explicitly reported with hash-lookup links, rather than described as successful scans. Detections and incomplete scans do not block publication. Report status/counts are a snapshot at release creation; follow the links for later results. Draft recovery preserves the original report snapshot.

A rerun may complete a partial draft only when its tag, commit, image identity and existing asset bytes match. Published releases and tags are immutable; mismatched or unverifiable existing assets stop recovery and require maintainer investigation rather than replacement. Native launch validation still requires matching operating systems and their documented libraries.
