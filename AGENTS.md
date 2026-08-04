# AI Coding Agent Instructions

> 📌 This document is the primary memory store for all AI agents working on this project. Conventions, architecture notes, and developer workflows recorded here should be followed and updated when making repository changes.

## Project Context
A temporary, AI-assisted Service Bus viewer now reorganized into three cooperating projects:
- `src\ServiceBusViewer` — ASP.NET Core minimal API (backend)
- `src\ServiceBusViewer.Web` — React + Vite SPA (frontend)
- `src\ServiceBusViewer.AppHost` — AppHost orchestration for local development (emulator, SQL, and dev servers)

Primary use case: local development and testing with the Azure Service Bus Emulator.

## Architecture

### Core Components
- `src\ServiceBusViewer/Program.cs` — minimal API host; registers business services, session-state middleware, CORS, and maps API endpoints via `MapApiEndpoints()`.
- `src\ServiceBusViewer/Api/**/*` — HTTP endpoint handlers and request/response contracts. The API surface is implemented as grouped minimal API endpoints.
- `src\ServiceBusViewer/Business/**/*` — domain/business services (e.g., `ViewerBusinessService`) and contracts used by the API.
- `src\ServiceBusViewer/Business/Services/ServiceBusSessionConnectionService.cs` — session-aware Service Bus connection/session manager (replaces the older singleton Razor `ServiceBusService`).
- `src\ServiceBusViewer/SessionState/*` — browser session-state registry and middleware (BrowserSessionStateRegistry, middleware, and cleanup hosted service) for per-browser-session isolation.
- `src\ServiceBusViewer.Web` — SPA source and build config (Vite, React, TypeScript). The SPA talks to the API under `/api`.
- `src\ServiceBusViewer.AppHost` — orchestration host that starts local services (emulator, SQL) and the SPA dev server for an integrated dev experience.

### Key Design Patterns
- Singleton registration for infrastructure services where appropriate (ServiceBus client factory/manager).
- Session-scoped viewer state stored in a server-side registry keyed by a browser cookie (`sbv-session`) and exposed via middleware.
- Minimal API endpoints organized into logical groups under `Api` with request/response DTOs in `Business.Contracts`.

## Development Workflows

### Run the full local stack (recommended)
Install SPA deps once from the repo root:

```powershell
npm run web:install
```

Start the AppHost to orchestrate emulator + API + SPA dev server:

```powershell
dotnet run --project src\ServiceBusViewer.AppHost
```

AppHost brings up required services and proxies the SPA dev server to the API during development.

### Run components independently
Start the API (choose a port via ASPNETCORE_URLS):

```powershell
setx ASPNETCORE_URLS "http://localhost:5221"; dotnet run --project src\ServiceBusViewer
```

Start the SPA dev server in another terminal (point proxy to the API):

```powershell
$env:VITE_PROXY_TARGET='http://localhost:5221'
npm run web:dev --prefix src\ServiceBusViewer.Web
```

Note: ensure VITE_PROXY_TARGET matches the API URL (ASPNETCORE_URLS) used.

### Build for production
- Build the .NET solution: `dotnet build src\ServiceBusViewer.sln`
- Build the SPA bundle: `npm run web:build --prefix src\ServiceBusViewer.Web`

## Containers
- Dockerfiles exist for both the API and the SPA: `src\ServiceBusViewer\Dockerfile` and `src\ServiceBusViewer.Web\Dockerfile`.
- CI workflows publish images for the API and the SPA.
- When running containers, use `host.docker.internal` for host emulator access or attach containers to a shared Docker network.

## Service Bus usage
- The code uses Azure.Messaging.ServiceBus for SDK calls; the session-aware connection manager handles receivers/senders per-session.
- Prefer `await using` on receivers/senders to ensure disposal.
- Receivers are created in PeekLock by default; follow the Business/Services implementation for details.

### C# Patterns
- Use `record` types for DTOs and simple data carriers (positional records preferred for compactness).
- Public methods and properties on services must have XML `<summary>` documentation. If the summary fits on one line, keep it on a single line: `<summary>Short description.</summary>`.
- Nullable reference types are enabled; annotate optional values with `?` and prefer explicit null checks where appropriate.
- Prefer primary constructors for simple page models or small types when it improves readability.
- ServiceBus clients and receivers: prefer `await using var receiver = GetReceiver()` for deterministic disposal and to avoid resource leaks.
- Receivers should be created in `ServiceBusReceiveMode.PeekLock` unless a different mode is explicitly required by the handler.
- Keep minimal API handlers thin: validate requests, call `Business` services (in `Business.Services`), and map results to response DTOs from `Business.Contracts`.
- Name DTOs with the `{Action}Request/Response` convention and keep them in the `Api` subfolders to match endpoint grouping.
- Add concise `<summary>` docs to request/response DTOs where the intent is not obvious from property names.

## Project-specific conventions
- Minimal API handlers live under `Api` and should use request/response DTOs from `Business.Contracts`.
- Follow the repository-wide patterns above when adding or refactoring code.

## External dependencies
- Azure.Messaging.ServiceBus (project-managed version)
- React + Vite + TypeScript for the SPA (see `src\ServiceBusViewer.Web/package.json`)
- .NET 10.0 target framework for backend projects

## Testing & Debugging
- No automated tests in the repo — manual testing only.
- Launch profiles exist in each project where applicable (see `Properties/launchSettings.json`).
- Environment variables:
  - `CONNECTION_STRING` overrides the default Service Bus connection string for the API.
  - `ROOT_CONNECTION_STRING` may be used when emulating namespace/root operations.
  - `ASPNETCORE_URLS` is recommended to fix the API listening port for local dev and container scenarios.


---

If any further repository-specific conventions or utility scripts should be added here (e.g., standardized dev commands, linting rules for the SPA), provide them and this document will be updated accordingly.
