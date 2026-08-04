# Service Bus Viewer

> **IMPORTANT NOTE:** This project was initially designed as a temporary, AI-assisted viewer for the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer).

Service Bus Viewer is now split into:
- `src\ServiceBusViewer` - ASP.NET Core minimal API (backend)
- `src\ServiceBusViewer.Web` - React + Vite SPA (frontend)
- `src\ServiceBusViewer.AppHost` - AppHost orchestration for local development

The main goal remains local development and testing against the Service Bus emulator.

## Features

- Connect to any Service Bus instance using a connection string.
- Browse queues and topics when a namespace-level root connection string is available.
- Peek and receive messages from queues or topic subscriptions.
- View message details and properties.
- Send messages to queues or topics.

## Local development

### Prerequisites

- .NET SDK 10.0 or later
- Node.js 24 LTS or later
- Docker Desktop / Podman Desktop if you want the AppHost to run the emulator containers for you

### Preferred: run the split app through Aspire

Install the SPA dependencies once:

```powershell
npm run web:install
```

Then start the full local stack:

```powershell
dotnet run --project src\ServiceBusViewer.AppHost
```

AppHost starts:
- SQL Server for emulator storage
- Azure Service Bus Emulator
- `ServiceBusViewer`
- `ServiceBusViewer.Web` via the Vite dev server

The SPA dev server proxies `/api` requests to the API automatically.

### Run the API and SPA separately

Start the API:

```powershell
dotnet run --project src\\ServiceBusViewer
```

Start the SPA dev server in another terminal:

```powershell
$env:VITE_PROXY_TARGET='http://localhost:5221'
npm run web:dev
```

Note: The API listens on the ASP.NET Core default URLs; set ASPNETCORE_URLS to override the port and make sure VITE_PROXY_TARGET points at the correct API URL when running the SPA dev server.

### Build the split app locally

Build the .NET solution:

```powershell
dotnet build src\ServiceBusViewer.sln
```

Build the SPA bundle:

```powershell
npm run web:build
```

## Containers

### Published images

Release workflows now publish two images:
- `ghcr.io/veselovandrey/ServiceBusViewer`
- `ghcr.io/veselovandrey/servicebusviewer-web`

### Build the images locally

```powershell
docker build -f src\\ServiceBusViewer\Dockerfile -t ServiceBusViewer .
docker build -f src\ServiceBusViewer.Web\Dockerfile -t servicebusviewer-web .
```

### Run the images locally

Create a shared network first:

```powershell
docker network create servicebusviewer
```

Run the API container:

```powershell
docker run --name ServiceBusViewer --network servicebusviewer -p 8080:8080 `
  -e CONNECTION_STRING="Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -e ROOT_CONNECTION_STRING="Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -d ServiceBusViewer
```

Run the SPA container:

```powershell
docker run --name servicebusviewer-web --network servicebusviewer -p 5000:8080 `
  -e ServiceBusViewer_UPSTREAM="http://ServiceBusViewer:8080" `
  -d servicebusviewer-web
```

The nginx-based SPA container serves the static site and reverse-proxies `/api/*` to the API container.

## Browser-session isolation

The API keeps viewer state in a server-side bucket keyed by the `sbv-session` cookie.
Different browser profiles or incognito windows get isolated state automatically.
Tabs that share the same browser cookie jar also share the same Service Bus viewer session.

## Accessing the Service Bus Emulator

- Use `localhost` when the viewer runs on the same host as the emulator.
- Use `host.docker.internal` when a containerized viewer needs to reach an emulator running on the host.
- Use the emulator container or compose service name when both are running on the same container network.
- Use the Azure portal connection string for real Azure Service Bus namespaces when needed.

Examples:

```text
Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

## Version history

- 0.11.1 (2026-08-04): Fixed:
  - failed connection attempts no longer leave the app stuck in a connected state, so you can immediately retry with a corrected connection string.

- 0.11.0 (2026-08-01): Added/fixed:
  - Redesigned application UI and UX: new visual design.
  - Miscellaneous packaging and build improvements.

- 0.10.0 (2026-02-15): Added/fixed:
  - client-side JSON formatting and toggle for message body;
  - display of subscription filters;
  - bugfix: unable to send messages to topics;
  - miscellaneous bugfixes.
- 0.9.0 (2026-02-10): Added support for:
  - session-enabled message receive with Session ID input;
  - sending messages with properties (MessageId, SessionId, CorrelationId, ScheduledEnqueueTime, TimeToLive);
  - application property type;
  - entity list caching.
- 0.8.0 (2026-02-05): Added queue/topic/subscription properties view.
- 0.7.0 (2026-02-01): Implemented client-side message expansion/collapse for better UX.
- 0.6.0 (2026-01-31): Added entity selection feature with visual icons for queues, topics, and subscriptions.
- 0.5.2 (2026-01-24): Took connection string from an environment variable.
- 0.5.1 (2026-01-22): Updated to .NET 10.
- 0.5.0 (2025-05-26): Initial version of ServiceBusViewer.
