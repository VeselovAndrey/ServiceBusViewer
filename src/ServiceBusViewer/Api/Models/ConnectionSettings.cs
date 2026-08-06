namespace ServiceBusViewer.Api.Models;

/// <summary>
/// Connection settings returned by bootstrap and disconnect endpoints.
/// </summary>
/// <param name="ConnectionString">Primary connection string used to connect to Service Bus.</param>
/// <param name="RootConnectionString">Optional root connection string used for namespace-level operations.</param>
/// <param name="QueueOrTopicName">Optional default queue or topic name.</param>
/// <param name="SubscriptionName">Optional subscription name.</param>
internal sealed record ConnectionSettings(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);


internal static class ConnectionSettingsExtensions
{
	internal static ConnectionSettings ToApiModel(this Business.Contracts.Viewer.ConnectionSettings settings)
		=> new ConnectionSettings(
			settings.ConnectionString,
			settings.RootConnectionString,
			settings.QueueOrTopicName,
			settings.SubscriptionName);
}