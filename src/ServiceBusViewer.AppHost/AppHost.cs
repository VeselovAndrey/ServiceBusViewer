using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string serviceBusEmulatorManagementConnectionString = $"Endpoint=sb://localhost:5300;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
const string serviceBusEmulatorConnectionString = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";


// Add SQL Server (Service Bus Emulator storage)
const string serviceBusEmulatorSqlServerContainerName = "servicebusviewer-servicebusemulator-storage";
string sqlPassword = Guid.NewGuid().ToString("D");

IResourceBuilder<ContainerResource> serviceBusEmulatorStorage = builder.AddContainer("servicebusviewer-servicebusemulator-storage", "mcr.microsoft.com/mssql/server:2025-latest")
	.WithContainerName(serviceBusEmulatorSqlServerContainerName)
	.WithEnvironment("ACCEPT_EULA", "Y")
	.WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword);

// Add Azure Service Bus emulator
IResourceBuilder<ContainerResource> serviceBusEmulator = builder.AddContainer("servicebusviewer-servicebusemulator", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "2.0.0")
	.WithContainerName("servicebusviewer-servicebusemulator")
	.WaitFor(serviceBusEmulatorStorage, WaitBehavior.WaitOnResourceUnavailable)
	.WithEndpoint(port: 5672, targetPort: 5672)
	.WithHttpEndpoint(port: 5300, targetPort: 5300)
	.WithBindMount(
		source: "ServiceBusConfig.json",
		target: "/ServiceBus_Emulator/ConfigFiles/Config.json",
		isReadOnly: true)
	.WithHttpHealthCheck("/health")
	.WithEnvironment("SQL_SERVER", serviceBusEmulatorSqlServerContainerName)
	.WithEnvironment("MSSQL_SA_PASSWORD", sqlPassword)
	.WithEnvironment("ACCEPT_EULA", "Y");

// Add Service Bus Viewer
IResourceBuilder<ProjectResource> serviceBusViewer = builder.AddProject<ServiceBusViewer>("ServiceBusViewer")
	.WithEnvironment("CONNECTION_STRING", serviceBusEmulatorConnectionString)
	.WithEnvironment("ROOT_CONNECTION_STRING", serviceBusEmulatorManagementConnectionString)
	.WaitFor(serviceBusEmulator, WaitBehavior.WaitOnResourceUnavailable);

DistributedApplication app = builder.Build();

await app.RunAsync();
