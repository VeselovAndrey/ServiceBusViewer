# Service Bus Viewer

Service Bus Viewer is a tool for inspecting and working with Azure Service Bus queues, topics, and subscriptions, available in the browser and as an Electron desktop application. It is primarily intended for local development and testing with the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer), but it can also connect to Azure Service Bus namespaces.

The repository also includes the `SolidCode.Aspire.Hosting.ServiceBusViewer` Aspire support package for registering the published Service Bus Viewer container from .NET Aspire AppHosts. See [`src\SolidCode.Aspire.Hosting.ServiceBusViewer\README.md`](src/SolidCode.Aspire.Hosting.ServiceBusViewer/README.md) for package installation and usage details.

## Features

- Automatically browse an Azure namespace when the primary connection has Manage permission, or connect directly to a queue or subscription.
- Browse queues, topics, subscriptions, entity properties, and subscription filters.
- Peek and receive messages from queues or topic subscriptions, including session-enabled entities, with a configurable peek count (presets 1-100 or a custom value up to 500) persisted in the browser.
- View the selected entity's dead-lettered messages with their dead-letter reason and source, and refresh the dead-letter list as the queue's contents change.
- Republish dead-lettered messages to the original entity, one at a time or in bulk, choosing how the republished copy's message id is assigned.
- Drop dead-lettered messages, one at a time or the entire dead-letter queue with a confirmation, with the affected and failed counts reported for the operation.
- View message bodies, system properties, and application properties, with client-side JSON formatting.
- Copy a received or peeked message body as plain text, or copy the full message as JSON including system and application properties.
- Send messages to queues or topics with system properties and typed application properties.

## Run with Docker

The release image contains both the compiled SPA and API. It is published with the `latest` tag and version-specific tags:

- `ghcr.io/veselovandrey/ServiceBusViewer`

Run the viewer:

```powershell
docker run --name ServiceBusViewer -p 8080:8080 -d ghcr.io/veselovandrey/ServiceBusViewer:latest
```

Open `http://localhost:8080`. The container listens on port `8080` and serves the SPA and `/api` endpoints from the same ASP.NET Core process.

## Connection modes

- **Azure namespace browsing:** enter a Manage-capable **Connection String**. The viewer detects management access automatically, discovers all queues, topics, and subscriptions, and uses the same credential for message operations.
- **Azure direct entity access:** enter a **Connection String** without Manage permission and a **Queue/Topic Name**. The name identifies a queue unless you also enter a **Subscription Name**, in which case it identifies the parent topic. Standalone topic access is not supported without management access.
- **Emulator namespace browsing:** enter the emulator messaging **Connection String** and its optional **Emulator Management Connection String**, normally using port `5300` for management.
- **Emulator direct entity access:** leave **Emulator Management Connection String** empty and provide either a queue name or a topic and subscription name with the emulator messaging connection string.

Direct entity access requires Listen permission for peeking and receiving messages and Send permission for sending messages. Azure Manage permission includes both Listen and Send.

## Runtime configuration

The viewer can be preconfigured with environment variables, but none of them is required. Their values become the initial settings shown on the connection page for each new browser session. You can review or replace those values before connecting.

Settings entered on the connection page apply to the current browser session. They do not change the container or host environment variables.

| Environment variable | Purpose | Default |
| --- | --- | --- |
| `SERVICEBUSVIEWER_CONNECTION_STRING` | Primary connection used for messages and, for Azure, automatic management discovery. | Local emulator connection string |
| `SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING` | Optional emulator-only administration connection used to browse and refresh entities. | Not set |
| `SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME` | Queue to open directly, or parent topic when a subscription is also configured. | Empty |
| `SERVICEBUSVIEWER_SUBSCRIPTION_NAME` | Optional subscription to open under the configured parent topic. | Not set |
| `SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT` | Controls whether the connect screen shows the emulator connect hint and the insert button. | Shown (hidden only when set to `false`) |

Automatic entity message-count refresh in the entity menu is configured through the `ServiceBusViewer:EntityCountAutoRefresh` configuration section instead, available from `appsettings.json` or environment variables:

| Configuration key / Environment variable | Purpose | Default |
| --- | --- | --- |
| `ServiceBusViewer:EntityCountAutoRefresh:Enabled` / `ServiceBusViewer__EntityCountAutoRefresh__Enabled` | Offers an auto mode on the entity-menu refresh control. The refresh interval is chosen per browser (presets or custom). | `false` |

For example, the following command preconfigures namespace browsing for an emulator running on the Docker host:

```powershell
docker run --name ServiceBusViewer -p 8080:8080 `
  -e SERVICEBUSVIEWER_CONNECTION_STRING="Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -e SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING="Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;" `
  -d ghcr.io/veselovandrey/ServiceBusViewer:latest
```

## Accessing the Service Bus Emulator

- Use `localhost` when the viewer runs on the same host as the emulator.
- Docker: use `host.docker.internal` to reach an emulator running on the Docker host.
- Podman: use `host.docker.internal` or `host.containers.internal` to reach an emulator running on the Podman host. 
  - IMPORTANT: The host emulator must listen on a non-loopback interface (not just `127.0.0.1`) to be reachable from Podman containers.
- Use the emulator container or compose service name when both are running on the same container network.
- For a real Azure Service Bus namespace, use an appropriate namespace connection string from the Azure portal.

The emulator uses its regular Service Bus endpoint for message operations and port `5300` for administration operations.

When the viewer runs directly on the host, use:

```text
Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```
```text
Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

When the viewer runs in a container and the emulator runs on the host, use:

```text
Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```
```text
Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;
```

For a Podman container host, replace `host.docker.internal` above with `host.containers.internal`.

## Browser-session isolation

The API keeps viewer state in a server-side bucket identified by the `sbv-session` cookie. Different browser profiles and private browsing sessions have isolated viewer state. 
Tabs that share the same browser cookie jar also share the same Service Bus Viewer session.

## Version history

## 1.2.2 (2026-11-19)

- **Changed:** Updated the backend, AppHost, and test projects to .NET 11, and moved the container runtime and CI SDK to .NET 11.
- **Changed:** Added .NET 11 support to the Aspire hosting package while retaining its .NET 10 target.
- **Changed:** Windows desktop bundles and downloads now use `win-<arch>` names (such as `win-x64`) instead of `win32-<arch>`.

## 1.2.1 (2026-11-13)

- **Added:** GitHub-generated release notes and VirusTotal report links with scan status for each download.
- **Fixed:** Releases include the Ubuntu 24.04 amd64 `.deb` installer alongside the desktop archives.

## 1.2.0 (2026-11-11)

- **Added:** Electron desktop application (Windows and Linux).

See the [full changelog](CHANGELOG.md) for the complete version history.

## Development

### Project structure

- `src\ServiceBusViewer` - ASP.NET Core minimal API backend
- `src\ServiceBusViewer.Web` - React + Vite SPA frontend
- `src\ServiceBusViewer.AppHost` - Aspire orchestration for local development
- `src\SolidCode.Aspire.Hosting.ServiceBusViewer` - reusable Aspire hosting integration for the published Service Bus Viewer container

For the package-specific installation and AppHost usage guide, see [`src\SolidCode.Aspire.Hosting.ServiceBusViewer\README.md`](src/SolidCode.Aspire.Hosting.ServiceBusViewer/README.md).

Local development runs the API and Vite as separate processes. The production Docker build compiles the SPA and packages it with the API so that one ASP.NET Core process serves both.

### Prerequisites

- .NET SDK 11.0 or later
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
- Azure Service Bus Emulator 2.0.1
- `ServiceBusViewer`
- `ServiceBusViewer.Web` via the Vite development server when `AppHost:UsePublishedServiceBusViewerContainer` is `false`

Set `AppHost:UsePublishedServiceBusViewerContainer` to `true` in `src\ServiceBusViewer.AppHost\appsettings.json` if you want to run the published combined `ghcr.io/veselovandrey/servicebusviewer:latest` container instead of the local backend project and Vite development server.

The Aspire dashboard opens in the browser. 
Select the `servicebusviewer-web` endpoint in local-project mode, or the `servicebusviewer` endpoint in container mode.

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
dotnet build src\ServiceBusViewer.slnx
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
The release workflows publish the combined image.

## Copyright

Copyright © 2025–2026 Andrey Veselov.

This project is distributed under the [MIT License](LICENSE).

