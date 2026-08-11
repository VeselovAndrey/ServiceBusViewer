# Service Bus Viewer

Service Bus Viewer is a browser-based tool for inspecting and working with Azure Service Bus queues, topics, and subscriptions. It is primarily intended for local development and testing with the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer), but it can also connect to Azure Service Bus namespaces.

## Features

- Connect directly to a queue or topic subscription, or use a root connection string to browse an entire namespace.
- Browse queues, topics, subscriptions, entity properties, and subscription filters.
- Peek and receive messages from queues or topic subscriptions, including session-enabled entities.
- View message bodies, system properties, and application properties, with client-side JSON formatting.
- Send messages to queues or topics with system and typed application properties.

## Run with Docker

The release image contains both the compiled SPA and API. It is published with the `latest` tag and version-specific tags:

- `ghcr.io/veselovandrey/ServiceBusViewer`

Run the viewer:

```powershell
docker run --name ServiceBusViewer -p 8080:8080 -d ghcr.io/veselovandrey/ServiceBusViewer:latest
```

Open `http://localhost:8080`. The container listens on port `8080` and serves the SPA and `/api` endpoints from the same ASP.NET Core process.

## Connection modes

- **Namespace browsing:** enter both a **Connection String** for message operations and a **Root Connection String** for administration operations. The viewer discovers all queues, topics, and subscriptions in the namespace.
- **Direct entity access:** leave **Root Connection String** empty. Enter a **Connection String** and a **Queue/Topic Name**. For a topic subscription, also enter the **Subscription Name**; otherwise, the specified entity is treated as a queue.

## Runtime configuration

The viewer can be preconfigured with environment variables, but none of them is required. Their values become the initial settings shown on the connection page for each new browser session. You can review or replace those values before connecting.

Settings entered on the connection page apply to the current browser session. They do not change the container or host environment variables.

| Environment variable | Purpose | Default |
| --- | --- | --- |
| `CONNECTION_STRING` | Connection used to peek, receive, and send messages. | Local emulator connection string |
| `ROOT_CONNECTION_STRING` | Administration connection used to browse and refresh the namespace entity list. | Not set |
| `QUEUE_OR_TOPIC_NAME` | Queue or topic to open in direct entity mode. | Empty |
| `SUBSCRIPTION_NAME` | Subscription to open when `QUEUE_OR_TOPIC_NAME` identifies a topic. | Not set |

For example, the following command preconfigures namespace browsing for an emulator running on the Docker host:

```powershell
docker run --name ServiceBusViewer -p 8080:8080 `
  -e CONNECTION_STRING="Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -e ROOT_CONNECTION_STRING="Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -d ghcr.io/veselovandrey/ServiceBusViewer:latest
```

## Accessing the Service Bus Emulator

- Use `localhost` when the viewer runs on the same host as the emulator.
- Use `host.docker.internal` when a containerized viewer needs to reach an emulator running on the host.
- Use the emulator container or compose service name when both are running on the same container network.
- For a real Azure Service Bus namespace, use an appropriate namespace connection string from the Azure portal.

The emulator uses its regular Service Bus endpoint for message operations and port `5300` for administration operations.

When the viewer runs directly on the host, use:

```text
Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

When the viewer runs in a container and the emulator runs on the host, use:

```text
Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

## Browser-session isolation

The API keeps viewer state in a server-side bucket identified by the `sbv-session` cookie. Different browser profiles and private browsing sessions have isolated viewer state. Tabs that share the same browser cookie jar also share the same Service Bus Viewer session.

## Version history

- 30.0.1 (2026-08-11):
  - Prevented long expanded message bodies from increasing the width of the Peeked Messages table.

- 0.30.0 (2026-08-09):
  - Replaced the Razor UI with a React + Vite SPA and reorganized the backend as a minimal API.
  - Isolated viewer state and Service Bus connections by browser session.
  - Added centralized RFC `ProblemDetails` API error handling.
  - Consolidated production deployment into one ASP.NET Core container that serves both the SPA and API.

- 0.11.1 (2026-08-04):
  - Fixed failed connection attempts leaving the app stuck in a connected state. You can now retry immediately with a corrected connection string.

- 0.11.0 (2026-08-01):
  - Redesigned the application UI and user experience.
  - Improved packaging and build configuration.

- 0.10.0 (2026-02-15):
  - Added client-side JSON formatting and a message-body formatting toggle.
  - Added subscription filter details.
  - Fixed sending messages to topics.
  - Included other minor fixes.

- 0.9.0 (2026-02-10):
  - Added support for receiving messages from session-enabled entities with an optional Session ID.
  - Added outgoing message properties: `MessageId`, `SessionId`, `CorrelationId`, `ScheduledEnqueueTime`, and `TimeToLive`.
  - Added typed application properties.
  - Added entity list caching.

- 0.8.0 (2026-02-05): Added queue/topic/subscription properties view.

- 0.7.0 (2026-02-01): Implemented client-side message expansion/collapse for better UX.

- 0.6.0 (2026-01-31): Added entity selection feature with visual icons for queues, topics, and subscriptions.

- 0.5.2 (2026-01-24): Added support for setting the connection string through an environment variable.

- 0.5.1 (2026-01-22): Updated to .NET 10.

- 0.5.0 (2025-05-26): Initial version of Service Bus Viewer.

## Development

### Project structure

- `src\ServiceBusViewer` - ASP.NET Core minimal API backend
- `src\ServiceBusViewer.Web` - React + Vite SPA frontend
- `src\ServiceBusViewer.AppHost` - Aspire orchestration for local development

Local development runs the API and Vite as separate processes. The production Docker build compiles the SPA and packages it with the API so that one ASP.NET Core process serves both.

### Prerequisites

- .NET SDK 10.0 or later
- Node.js 24 LTS or later
- Docker Desktop or Podman Desktop when using AppHost to run the emulator containers

### Run the full stack with Aspire

Install the SPA dependencies once:

```powershell
npm run web:install
```

Start the full local stack:

```powershell
dotnet run --project src\ServiceBusViewer.AppHost
```

AppHost starts:

- SQL Server for emulator storage
- Azure Service Bus Emulator 2.0.0
- `ServiceBusViewer`
- `ServiceBusViewer.Web` via the Vite development server

The Aspire dashboard opens in the browser. Select the `servicebusviewer-web` endpoint to open the app. 
The Vite server proxies `/api` requests to the API automatically.

### Run the API and SPA separately

Start the API:

```powershell
dotnet run --project src\ServiceBusViewer
```

Start the SPA development server in another terminal:

```powershell
$env:VITE_PROXY_TARGET='http://localhost:5221'
npm run web:dev
```

The API's `http` launch profile listens on `http://localhost:5221`. 
If you override it with `ASPNETCORE_URLS`, set `VITE_PROXY_TARGET` to the same API origin. 
`VITE_PROXY_TARGET` defaults to `http://localhost:5221`.

See [the web project README](src/ServiceBusViewer.Web/README.md) for frontend-specific scripts and environment settings.

### Build locally

Build the .NET solution:

```powershell
dotnet build src\ServiceBusViewer.sln
```

Build the SPA bundle:

```powershell
npm run web:build
```

Build the combined SPA and API image:

```powershell
docker build -f src\ServiceBusViewer\Dockerfile -t ServiceBusViewer .
```

The development workflow publishes the combined image with the `canary` tag.

## Copyright

Copyright © 2025–2026 Andrey Veselov.

This project is distributed under the [MIT License](LICENSE).
