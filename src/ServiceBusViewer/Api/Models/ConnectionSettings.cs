namespace ServiceBusViewer.Api.Models;

/// <summary>Connection settings returned by session-state and disconnect endpoints.</summary>
/// <param name="ConnectionString">Primary connection string used to connect to Service Bus.</param>
/// <param name="EmulatorManagementConnectionString">Optional emulator-only management connection string.</param>
/// <param name="QueueOrTopicName">Optional default queue or topic name.</param>
/// <param name="SubscriptionName">Optional subscription name.</param>
internal sealed record ConnectionSettings(
	string ConnectionString,
	string? EmulatorManagementConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);


internal static class ConnectionSettingsExtensions
{
	internal static ConnectionSettings ToApiModel(this Business.Viewer.Contracts.ConnectionSettings settings)
		=> new ConnectionSettings(
			settings.ConnectionString,
			settings.EmulatorManagementConnectionString,
			settings.QueueOrTopicName,
			settings.SubscriptionName);
}
