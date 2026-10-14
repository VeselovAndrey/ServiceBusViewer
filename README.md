# Service Bus Viewer

Service Bus Viewer is a browser-based tool for inspecting and working with Azure Service Bus queues, topics, and subscriptions. It is primarily intended for local development and testing with the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer), but it can also connect to Azure Service Bus namespaces.

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

## 1.0.0-rc2 (2026-10-14)

- **Added:** Peek count selection on the Viewer page — presets (1, 5, 10, 30, 50, 100) or a custom value up to 500 — applied without reconnecting; the choice is saved in the browser.
- **Added:** A theme option that follows the OS light/dark setting live; the choice is saved in the browser.
- **Added:** Emulator connect hint on the Connect screen, with a button that fills in the emulator connection strings (separate Docker and Podman buttons when the viewer runs in a container).
- **Added:** Sortable message-list columns with direction indicators, and a search box over the loaded messages — in both peeked and dead-letter modes.
- **Added:** Confirmation prompt for Republish All, matching the Drop All confirmation.
- **Changed:** The Dead Letter toggle now shows its target — "To Dead Letter" / "To Active" — with a matching archive/inbox icon and a fixed button width.
- **Changed:** Republish All uses the same neutral button style as the other buttons on the page.
- **Changed:** Viewer header layout: Refresh, the mode toggle ("To Dead Letter" / "To Active"), search, and the receive controls moved into the message-list and Last Received Message headers; in dead-letter mode Republish All and Drop All sit on a separate line below a divider.
- **Fixed:** Dark-mode tints (dead-letter toggle, dead-letter header bar, button shadows) that were not rendering because the brand-950 theme color was missing.
- **Fixed:** Page flicker while resizing the window near layout wrap points.

## 1.0.0-rc1 (2026-10-11)

- **Added:** Dead Letter toggle showing the selected entity's dead-letter queue with the dead-letter reason, source, and error description.
- **Added:** Republish dead-lettered messages to the original entity or drop them permanently — per message or in bulk (Republish All / Drop All with confirmation), with progress, cancellation, and affected/failed counts.
- **Added:** Message-id strategy dropdown for republishing: keep original (hidden with duplicate detection), auto-generate, GUID or GUID v7, or a manual id for a single message.
- **Added:** Total message count in the message list header when reliable counts are available.
- **Changed:** In dead letter mode, Refresh reloads the dead-letter list and selecting another entity exits the mode; single drop/republish animates the row out; dead-letter buttons stay disabled while an operation runs.
- **Changed:** Upgraded to Aspire 13.6.1, `Azure.Messaging.ServiceBus` 7.21.0, React 19.3, TypeScript 7, and Vite 8.3.
- **Fixed:** Dead-letter drop and republish now allow enough time for the initial receive on session-enabled entities.

## 0.48.0 (2026-08-23)

- **Added:** Per-message Delivery Count based on SystemProperties.DeliveryCount as reported by Azure Service Bus, shown as a column in the Peeked Messages list and in the Last Received Message details.
- **Added:** Optional To, Reply To, Subject, and Partition Key fields in the Send Message window, with the values sent on the outgoing message and displayed in received and expanded peeked message details.
- **Added:** Advanced toggle in the Send Message header that shows or hides the new fields; the choice is saved in a browser cookie and defaults to off.
- **Changed:** The delivery count is display-only: it does not affect receiving, completing, or dead-lettering messages and adds no badges, warnings, or color coding.
- **Changed:** The Peeked Messages table shows the partition key as a sub-value under the entity name for partitioned queues and topics.
- **Changed:** Renamed the Application Properties section in the send and receive views to Custom Properties.
- **Changed:** The Aspire AppHost and the `SolidCode.Aspire.Hosting.ServiceBusViewer` support library are now built against Aspire 13.5.2.
- **Changed:** The `SolidCode.Aspire.Hosting.ServiceBusViewer` package version is now aligned with the Aspire version it targets, and future releases will continue to carry the same version number as their target Aspire release.
- **Changed:** Paste From Clipboard now skips the Diagnostic-Id application property when loading a copied message into the send window.
- **Changed:** The duplicate-detection warning for the Message ID field is now always visible with its icon and text on entities with duplicate detection enabled, instead of appearing only after a paste.
- **Fixed:** The "Duplicate detection enabled" indicator in the Send Message window no longer mixes information and warning semantics. It shows as a neutral info notice when duplicate detection is enabled, and the amber warning styling applies to the indicator and Message ID field only when the warning is armed with a non-empty Message ID.
- **Fixed:** Sending a message with a Scheduled Enqueue Time or Time to Live that cannot be parsed now returns a validation error instead of silently dropping the property.
- **Fixed:** The Partition Key in the Send Message window is now limited to 128 characters with client-side and API validation, matching the Azure Service Bus limit.
- **Fixed:** A number of additional minor bugs, with small usability and display improvements.

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

