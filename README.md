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

Automatic entity message-count refresh in the entity menu is configured through the `ServiceBusViewer:EntityCountAutoRefresh` configuration section instead, available from `appsettings.json` or environment variables:

| Configuration key | Environment variable | Purpose | Default |
| --- | --- | --- | --- |
| `ServiceBusViewer:EntityCountAutoRefresh:Enabled` | `ServiceBusViewer__EntityCountAutoRefresh__Enabled` | Offers an auto mode on the entity-menu refresh control. The refresh interval is chosen per browser (presets or custom). | `false` |

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

## 1.0.12 (2026-10-26)

- **Security:** Non-emulator connection strings are now masked by default on the Connect screen, while emulator values are shown automatically for local development. Independent show/hide controls remain available until a field's value changes, without changing prefill, editing, connection, or Escape-to-clear behavior.
- **Security:** The Vite development server now listens only on `localhost` by default, so other machines cannot reach the development SPA or `/api` proxy and retrieve configured Service Bus credentials through it. Local and Aspire development workflows are preserved.
- **Fixed:** Edits made to the message payload, properties, or custom properties while a send is pending are now preserved when the earlier send succeeds. The sent message still uses the exact content captured when the request started.
- **Fixed:** Entity count badges now refresh after single and bulk dead-letter drop/republish operations. Manual and automatic count refreshes on Entity Details now update the displayed badges instead of leaving cached values visible.
- **Fixed:** Hard reloads now explicitly refresh the active message list before displaying normal mode, preventing DLQ-backed session messages from appearing under Peeked Messages. Queue and subscription selections retain their identity, and subsequent active/DLQ toggles use the correct labels, counts, and actions.
- **Fixed:** Scheduled enqueue times now preserve the instant selected in the browser by converting the local input to UTC before sending it to the API, regardless of the server timezone.
- **Fixed:** Delayed viewer responses can no longer overwrite a newer entity selection or restore viewer state after Disconnect. Entity-count refreshes now apply only to the entity and connection that requested them, keeping the displayed entity consistent with subsequent message operations.
- **Fixed:** Overlapping viewer actions (for example Refresh started while a Bulk Drop All or Republish All is running) no longer clear the in-flight operation's busy state when they finish: destructive drop/republish controls stay disabled until every in-flight operation has truly completed, and the progress, cancellation, and outcome state remain owned by the active operation.
- **Fixed:** Persistent per-message failures in Bulk Drop All and Republish All now stop the operation immediately and report the exact affected/failed counts, instead of retrying the dead-lettered messages indefinitely and inflating the progress. A republish step that could not complete is no longer retried automatically, so an already-delivered copy cannot be resent; cancellation still stops the operation promptly.
- **Fixed:** Single-message Drop and Republish now target the exact selected broker message by its Service Bus sequence number, so they work even when message IDs are null or duplicated. Only actually processed rows are removed from the dead-letter list and displayed totals change by the true affected count; unprocessed duplicate rows remain visible, and message IDs stay display metadata.
- **Fixed:** Single-message republish now rejects messages without a message ID before any API request, preventing a row action from becoming an unintended Republish All. Intentional bulk republish still requires confirmation, and message search handles null IDs safely.
- **Fixed:** Bulk Drop All and Republish All now lock entity selection and property navigation from confirmation through completion or cancellation, preventing later batches from affecting a different entity. Progress and cancellation remain available, and other entities are unchanged.

## 1.0.0 (2026-10-18)

- **Added:** Message count badges in the entity menu: queues and subscriptions show active, scheduled, and dead-letter counts (e.g. `5 | 0 | 2`); topics show the sums across their subscriptions. Counts load on connect and are omitted when unavailable or unreliable (e.g. the Service Bus emulator).
- **Added:** Hover/focus hint on each counter naming its category (e.g. "DLQ: 2"); clicking a counter selects the entity.
- **Added:** Entity counts refresh automatically after viewer message operations (send, receive, dead-letter drop/republish) and on demand via the refresh control in the entity menu.
- **Added:** Optional auto-refresh mode on the entity-menu refresh control with presets (1, 3, 5, 10, 15, 30, 60 seconds) or a custom interval saved in the browser. Auto mode is offered only when enabled in application configuration (`ServiceBusViewer:EntityCountAutoRefresh:Enabled`, disabled by default; enabled in the Docker image and the Aspire AppHost).

## 1.0.0-rc2 (2026-10-14)

- **Added:** Peek count selection on the Viewer page — presets (1, 5, 10, 30, 50, 100) or a custom value up to 500 — applied without reconnecting; the choice is saved in the browser.
- **Added:** A theme option that follows the OS light/dark setting live; the choice is saved in the browser.
- **Added:** Emulator connect hint on the Connect screen, with a button that fills in the emulator connection strings (separate Docker and Podman buttons when the viewer runs in a container).
- **Added:** Sortable message-list columns (Message ID, Session, Delivery Count, Enqueued Time / TTL, Content Type, and Dead Letter Reason in dead-letter mode) with direction indicators, and a search box over the loaded messages — in both peeked and dead-letter modes.
- **Added:** Confirmation prompt for Republish All, matching the Drop All confirmation.
- **Changed:** The **Dead Letter** toggle now shows its target — **To Dead Letter** / **To Active** — with a matching archive/inbox icon and a fixed button width.
- **Changed:** **Republish All** uses the same neutral button style as the other buttons on the page.
- **Changed:** Viewer header layout: Refresh, the mode toggle, search, and the receive controls moved into the message-list and Last Received Message headers; the mode toggle is now "To Dead Letter" / "To Active"; in dead-letter mode Republish All and Drop All sit on a separate line below a divider.
- **Fixed:** Dark-mode tints (dead-letter toggle, dead-letter header bar, button shadows) that were not rendering because the `brand-950` theme color was missing.
- **Fixed:** Page flicker while resizing the window near layout wrap points.

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

