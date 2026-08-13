namespace ServiceBusViewer.Configuration.Settings;

using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;

internal sealed class ClientSessionSettings : IClientSessionSettings
{
	public string ConnectionString => Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_CONNECTION_STRING")
									  ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

	public string? EmulatorManagementConnectionString => Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING");

	public string? QueueOrTopicName => Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME");

	public string? SubscriptionName => Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SUBSCRIPTION_NAME");
}
