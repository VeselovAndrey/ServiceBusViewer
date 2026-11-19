#pragma warning disable ASPIREJAVASCRIPT001

using Aspire.Hosting.Dotnet;
using Microsoft.Extensions.Configuration;
using SolidCode.Aspire.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

const string serviceBusEmulatorMessagingEndpointName = "emulator";
const string serviceBusEmulatorManagementEndpointName = "emulatorhealth";
AppHostSettings? appHostSettings = builder.Configuration.GetSection("AppHost").Get<AppHostSettings>();

// Add SQL Server (Service Bus Emulator storage)
const string serviceBusEmulatorSqlServerContainerName = "servicebusviewer-servicebusemulator-storage";
string sqlPassword = Guid.NewGuid().ToString("D");

// Add Azure Service Bus emulator
IResourceBuilder<IResourceWithEndpoints> serviceBusEmulator;
if (appHostSettings?.UseAspireManagedServiceBusEmulator == true) {
	// Use Aspire Managed Service Bus Emulator
	serviceBusEmulator = builder.AddAzureServiceBus("ServiceBusViewer-ServiceBusEmulator")
		.RunAsEmulator(c => {
			c.WithContainerName("servicebusviewer-servicebusemulator");
			c.WithConfigurationFile(Path.Combine(AppContext.BaseDirectory, "ServiceBusConfig.json"));
		});
}
else {
	// Use published Service Bus Emulator container
	IResourceBuilder<ContainerResource> serviceBusEmulatorStorage = builder.AddContainer("ServiceBusViewer-ServiceBusEmulator-Storage", "mcr.microsoft.com/mssql/server:2025-latest")
		.WithContainerName(serviceBusEmulatorSqlServerContainerName)
		.WithEnvironment("ACCEPT_EULA", "Y")
		.WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword);

	serviceBusEmulator = builder.AddContainer("ServiceBusViewer-ServiceBusEmulator", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "2.0.1")
		.WithContainerName("servicebusviewer-servicebusemulator")
		.WaitFor(serviceBusEmulatorStorage, WaitBehavior.WaitOnResourceUnavailable)
		.WithEndpoint(port: 5672, targetPort: 5672, name: serviceBusEmulatorMessagingEndpointName)
		.WithHttpEndpoint(port: 5300, targetPort: 5300, name: serviceBusEmulatorManagementEndpointName)
		.WithHttpHealthCheck("/health", endpointName: serviceBusEmulatorManagementEndpointName)
		.WithBindMount(source: "ServiceBusConfig.json", target: "/ServiceBus_Emulator/ConfigFiles/Config.json", isReadOnly: true)
		.WithEnvironment("SQL_SERVER", serviceBusEmulatorSqlServerContainerName)
		.WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
		.WithEnvironment("ACCEPT_EULA", "Y");
}

// Add Service Bus Viewer
if (appHostSettings?.UsePublishedServiceBusViewerContainer == true) {
	// Use published Service Bus Viewer container
	builder.AddServiceBusViewer("ServiceBusViewer-ContainerApp")
		.WithContainerName("servicebusviewer-app")
		.WithServiceBusEmulatorReference(serviceBusEmulator);
}
else {
	// Use Service Bus Viewer project
	ReferenceExpression serviceBusEmulatorConnectionString = CreateServiceBusEmulatorConnectionString(serviceBusEmulator.GetEndpoint(serviceBusEmulatorMessagingEndpointName));
	ReferenceExpression serviceBusEmulatorManagementConnectionString = CreateServiceBusEmulatorConnectionString(serviceBusEmulator.GetEndpoint(serviceBusEmulatorManagementEndpointName));

	IResourceBuilder<DotnetProjectResource> serviceBusViewerApi = builder.AddDotnetProject("ServiceBusViewer-API", "../ServiceBusViewer/ServiceBusViewer.csproj")
		.WithEnvironment("SERVICEBUSVIEWER_CONNECTION_STRING", serviceBusEmulatorConnectionString)
		.WithEnvironment("SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING", serviceBusEmulatorManagementConnectionString)
		.WithEnvironment("ServiceBusViewer__EntityCountAutoRefresh__Enabled", "true")
		.WithExternalHttpEndpoints()
		.WaitFor(serviceBusEmulator, WaitBehavior.WaitOnResourceUnavailable);

	builder.AddViteApp("ServiceBusViewer-Frontend", @"..\ServiceBusViewer.Web")
	 	.WithExternalHttpEndpoints()
		.WithReference(serviceBusViewerApi)
	 	.WaitFor(serviceBusViewerApi, WaitBehavior.WaitOnResourceUnavailable)
		.WithEnvironment("VITE_PROXY_TARGET", serviceBusViewerApi.GetEndpoint("http"))
		.PublishAsStaticWebsite("/api", serviceBusViewerApi);
}

DistributedApplication app = builder.Build();

await app.RunAsync();

static ReferenceExpression CreateServiceBusEmulatorConnectionString(EndpointReference endpoint)
	=> ReferenceExpression.Create($"Endpoint=sb://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");

sealed class AppHostSettings
{
	public bool UseAspireManagedServiceBusEmulator { get; init; }

	public bool UsePublishedServiceBusViewerContainer { get; init; }
}
