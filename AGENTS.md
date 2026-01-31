# AI Coding Agent Instructions

## Project Context
A **temporary, AI-generated** ASP.NET Razor Pages web app for viewing/interacting with Azure Service Bus queues/topics. Primary use case: local development with the [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator-installer). No long-term maintenance planned.

## Architecture

### Core Components
- **[ServiceBusService.cs](src/ServiceBusViewer/Infrastructure/ServiceBus/ServiceBusService.cs)**: Singleton service managing a single persistent `ServiceBusClient` connection. Provides peek/receive/send operations.
- **[Index.cshtml.cs](src/ServiceBusViewer/Pages/Index.cshtml.cs)**: The only page. Handles all UI interactions via `OnPost*` handlers (Connect, Disconnect, Refresh, Peek, Receive, Send).
- **Models**: Simple record types ([MessageDetails](src/ServiceBusViewer/Infrastructure/ServiceBus/Models/MessageDetails.cs), [PeekedMessageInfo](src/ServiceBusViewer/Infrastructure/ServiceBus/Models/PeekedMessageInfo.cs)) with no business logic.

### Key Design Patterns
- **Singleton Service**: `ServiceBusService` is registered as singleton in [Program.cs](src/ServiceBusViewer/Program.cs). Only one connection active at a time.
- **State Management**: Connection state tracked via `Connected` property + string fields (`Host`, `EntityName`, `SubscriptionName`). No distributed state.
- **Error Handling**: Exceptions caught in `OnPost*` handlers, added to `ModelState.AddModelError()`, displayed in UI via Razor validation summary.

## Development Workflows

### Running Locally
```powershell
cd src\ServiceBusViewer
dotnet run
```
Defaults to `localhost` emulator connection string (see [Index.cshtml.cs#L25-26](src/ServiceBusViewer/Pages/Index.cshtml.cs)).

### Docker Build (from solution directory)
```powershell
cd src
docker build -f ServiceBusViewer/Dockerfile -t servicebusviewer .
docker run -p 5000:8080 -e CONNECTION_STRING="..." servicebusviewer
```
**Critical**: Dockerfile expects to run from `src/` (solution directory), not project directory.

### Connection String Patterns
- **Emulator from host**: `Endpoint=sb://localhost;SharedAccessKeyName=...;UseDevelopmentEmulator=true;`
- **Emulator from container**: Use `host.docker.internal` instead of `localhost`
- See [README.md](README.md#accessing-the-service-bus-emulator) for full examples

## Project-Specific Conventions

### C# Patterns
- **Records for DTOs**: All model classes use `record` types with positional parameters (e.g., `MessageDetails`, `PeekedMessageInfo`).
- **XML docs**: Public methods/properties in `ServiceBusService` have `<summary>` tags. Apply same pattern to new public members.
- **Nullable reference types**: Enabled via `<Nullable>enable</Nullable>`. Use `?` for optional parameters/properties.
- **Primary constructors**: Page models use C# 12 primary constructors (e.g., `IndexModel(ServiceBusService serviceBusService)`).

### Razor Pages
- **Single-page app**: All functionality in `Index.cshtml` + code-behind. No navigation/routing beyond root.
- **Form handlers**: Each action has dedicated `OnPost{Action}` method. Always call `FillHeaderValue()` before returning `Page()`.
- **Model binding**: Use `[BindProperty]` for form inputs. Use `[BindProperty(SupportsGet = true)]` for query string params.

### Service Bus Client Usage
- **Receiver disposal**: Always use `await using var receiver = GetReceiver()` for automatic cleanup.
- **PeekLock mode**: All receivers created with `ServiceBusReceiveMode.PeekLock` (see [ServiceBusService.cs#L148](src/ServiceBusViewer/Infrastructure/ServiceBus/ServiceBusService.cs)).
- **Queue vs Topic**: `GetReceiver()` auto-detects based on `SubscriptionName` presence. Senders ignore subscriptions (topics only).

## External Dependencies
- **Azure.Messaging.ServiceBus 7.20.1**: Primary SDK. No custom wrappers/abstractions beyond `ServiceBusService`.
- **Bootstrap 5.3.6**: Vendored in [wwwroot/lib/bootstrap](src/ServiceBusViewer/wwwroot/lib/bootstrap). No CDN usage.
- **.NET 10.0**: Targeting `net10.0` framework. Uses implicit usings and file-scoped namespaces.

## Testing & Debugging
- **No automated tests**: Project has no test projects or test code. Manual testing only.
- **Launch profiles**: See [launchSettings.json](src/ServiceBusViewer/Properties/launchSettings.json). Use "ServiceBusViewer" for local dev, "Container (Dockerfile)" for Docker testing.
- **Environment variable**: Set `CONNECTION_STRING` env var to override default connection string.
