# SolidCode.Aspire.Hosting.ServiceBusViewer

`SolidCode.Aspire.Hosting.ServiceBusViewer` is a reusable .NET Aspire hosting integration for running [Service Bus Viewer](https://github.com/VeselovAndrey/ServiceBusViewer) as a container resource in an AppHost.

By default, the package uses the published GitHub Container Registry image:

- `ghcr.io/veselovandrey/servicebusviewer:latest`

It targets container-based Aspire AppHosts and wraps the published Service Bus Viewer image with typed registration and configuration helpers.
The package ships assemblies for both .NET 8 and .NET 10, so it can be consumed by Aspire AppHosts targeting either runtime.

## Install

Add the package to your AppHost project:

```powershell
dotnet add package SolidCode.Aspire.Hosting.ServiceBusViewer
```

## Usage

### Basic usage

```csharp
using SolidCode.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddServiceBusViewer("servicebusviewer");

await builder.Build().RunAsync();
```

The integration exposes Service Bus Viewer on container port `8080`. You can pin a host port and override the image tag during registration:

```csharp
builder.AddServiceBusViewer("servicebusviewer", tag: "0.40.4", httpPort: 8081);
```

### Configure connection settings with `WithConnectionSettings(...)`

`WithConnectionSettings(...)` is the convenience helper for setting the same runtime environment variables supported by the application itself in one call.

If you want the viewer to open directly against one subscription instead of starting with namespace browsing, pass `queueOrTopicName` and `subscriptionName` to `WithConnectionSettings(...)`:

```csharp
builder.AddServiceBusViewer("servicebusviewer")
	.WithConnectionSettings(
		connectionString: "Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=MyPolicy;SharedAccessKey=KEY_VALUE;",
		queueOrTopicName: "orders",
		subscriptionName: "processor");
```

In that example, `queueOrTopicName: "orders"` identifies the parent topic and `subscriptionName: "processor"` selects the subscription under that topic, so the viewer starts in direct subscription access mode. 
Use only `queueOrTopicName` when you want direct queue access instead.

> IMPORTANT: If the Service Bus connection string does not include management permissions, or if the Service Bus emulator management API connection is not configured,
> you must provide either `queueOrTopicName` alone for direct queue access or both `queueOrTopicName` and `subscriptionName` for direct subscription access. 
> In that mode, the viewer connects only to the specified queue or subscription instead of browsing the full namespace.

You can use the same helper when you want to provide the connection strings yourself:

```csharp
builder.AddServiceBusViewer("servicebusviewer", httpPort: 8080)
	.WithConnectionSettings(
		connectionString: "Endpoint=sb://host.docker.internal;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
		emulatorManagementConnectionString: "Endpoint=sb://host.docker.internal:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
		queueOrTopicName: "orders",
		subscriptionName: "processor");
```

Equivalent individual helpers are also available:

- `.WithConnectionString(...)`
- `.WithEmulatorManagementConnectionString(...)`
- `.WithQueueOrTopicName(...)`
- `.WithSubscriptionName(...)`

String and `ReferenceExpression` overloads are available for the connection-string helpers, so you can use either literal values or values derived from other Aspire resources.
`WithConnectionSettings(...)` also provides both string and `ReferenceExpression` overloads.

`AddServiceBusViewer(...)` returns `IResourceBuilder<ServiceBusViewerResource>`, so you can keep composing standard Aspire container configuration such as `.WithEnvironment(...)`, `.WithContainerName(...)`, and endpoint configuration methods.

### Use with the Azure Service Bus Emulator container

This package does not provision the emulator for you. Create or register the emulator separately in your AppHost, then use the viewer helper against the emulator resource.
See the [Azure Service Bus Emulator documentation](https://learn.microsoft.com/azure/service-bus-messaging/test-locally-with-service-bus-emulator) for emulator setup details.

If your AppHost exposes emulator endpoints named `emulator` and `emulatorhealth` (the current Aspire defaults), use `.WithServiceBusEmulatorReference(...)` 
to derive both Service Bus Viewer connection strings automatically and add a dependency on the emulator resource:

```csharp
using Aspire.Hosting.ApplicationModel;
using SolidCode.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ContainerResource> serviceBusEmulator = builder.AddContainer(
		"servicebus-emulator",
		"mcr.microsoft.com/azure-messaging/servicebus-emulator",
		"2.0.1")
	.WithEndpoint(port: 5672, targetPort: 5672, name: "emulator")
	.WithHttpEndpoint(port: 5300, targetPort: 5300, name: "emulatorhealth");

builder.AddServiceBusViewer("servicebusviewer", httpPort: 8081)
	.WithServiceBusEmulatorReference(serviceBusEmulator);
```

The helper generates emulator-style connection strings that use `RootManageSharedAccessKey`, sets both `SERVICEBUSVIEWER_CONNECTION_STRING` and `SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING`,
and waits for the emulator resource before starting the viewer container.

If your emulator resource uses different endpoint names, pass them explicitly:

```csharp
builder.AddServiceBusViewer("servicebusviewer")
	.WithServiceBusEmulatorReference(
		serviceBusEmulator,
		messagingEndpointName: "messaging",
		managementEndpointName: "management");
```

### Use with Aspire's Azure Service Bus hosting package

If your AppHost uses [Aspire's Azure Service Bus integration](https://aspire.dev/integrations/cloud/azure/azure-service-bus/azure-service-bus-host/), 
keep using that package to provision or reference the Service Bus resource and then wire the resulting endpoints or connection string into Service Bus Viewer. 
See the [Aspire Azure Service Bus hosting documentation](https://learn.microsoft.com/dotnet/aspire/messaging/azure-service-bus-integration) for the resource setup details.

For an Aspire-managed emulator resource, use `.WithServiceBusEmulatorReference(...)` so the viewer gets both the messaging and management connection strings automatically:

```csharp
using SolidCode.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureServiceBusResource> serviceBusResourceBuilder = builder.AddAzureServiceBus("servicebus")
	.RunAsEmulator();

builder.AddServiceBusViewer("servicebusviewer")
	.WithServiceBusEmulatorReference(serviceBusResourceBuilder);
```

For an Azure Service Bus namespace, pass the resolved namespace connection string into the viewer:

```csharp
using SolidCode.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<AzureServiceBusResource> serviceBusResourceBuilder = builder.AddAzureServiceBus("servicebus");

builder.AddServiceBusViewer("servicebusviewer")
	.WithConnectionString(serviceBusResourceBuilder.Resource.ConnectionStringExpression);
```

## Notes

- The package version is aligned with the Aspire version it targets, and future releases will continue to carry the same version number as their target Aspire release.
- The package is container-focused.
- `.WithServiceBusEmulatorReference(...)` defaults to endpoint names `emulator` and `emulatorhealth`, matching the current Aspire Azure Service Bus emulator integration.
- You can override those endpoint names when your AppHost uses a custom emulator resource shape.

## Version history

### 0.1.0 (2026-08-13)

- Initial release of the Aspire hosting integration package.

See the [full changelog](CHANGELOG.md) for the complete package version history.

## Copyright

Copyright © 2026 Andrey Veselov.

This project is distributed under the [MIT License](https://github.com/VeselovAndrey/ServiceBusViewer/blob/main/LICENSE).