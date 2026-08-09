# ServiceBusViewer.Web

ServiceBusViewer.Web is the React and Vite frontend for Service Bus Viewer. It communicates with the ASP.NET Core backend through HTTP endpoints under `/api`.

## Prerequisites

- Node.js 24 LTS or later
- The ServiceBusViewer backend, either started separately or through AppHost

## Install dependencies

Run the repository-level install command from the repository root:

```powershell
npm run web:install
```

This runs `npm ci` against this project's lock file.

## Run for development

### With AppHost

From the repository root:

```powershell
dotnet run --project src\ServiceBusViewer.AppHost
```

AppHost starts the backend, the Vite development server, and the Azure Service Bus Emulator dependencies. Open the `servicebusviewer-web` endpoint from the Aspire dashboard.

### With a separately running backend

Start the backend from the repository root:

```powershell
dotnet run --project src\ServiceBusViewer
```

In another terminal, start Vite:

```powershell
$env:VITE_PROXY_TARGET='http://localhost:5221'
npm run web:dev
```

Vite listens on port `5173` by default and proxies `/api` to `VITE_PROXY_TARGET`. The backend's `http` launch profile uses `http://localhost:5221` by default.

## Frontend environment settings

| Variable | Purpose | Default |
| --- | --- | --- |
| `VITE_PROXY_TARGET` | Backend origin used by the Vite development-server proxy for `/api`. | `http://localhost:5221` |
| `VITE_API_BASE_PATH` | API base path embedded in the frontend bundle by Vite. | `/api` |
| `PORT` | Port used by the Vite development server. | `5173` |

`VITE_API_BASE_PATH` is read when the Vite development server starts or when the SPA is built. It cannot reconfigure an already-built SPA. The current Vite proxy and combined container route `/api`; a custom base path also requires matching proxy or hosting configuration.

## Scripts

Run these commands from the repository root:

| Command | Purpose |
| --- | --- |
| `npm run web:dev` | Start the Vite development server with hot module replacement. |
| `npm run web:build` | Type-check the frontend and build the production bundle into `dist`. |
| `npm run web:lint` | Run Oxlint against the frontend project. |
| `npm run web:install` | Install dependencies from the lock file with `npm ci`. |

Equivalent `npm run dev`, `npm run build`, `npm run lint`, and `npm ci` commands can be run directly from `src\ServiceBusViewer.Web`.

## Production packaging

The production Docker build runs the frontend build and copies `dist` into the published ASP.NET Core application's `wwwroot`. The final combined image serves both the SPA and `/api` from port `8080`.
