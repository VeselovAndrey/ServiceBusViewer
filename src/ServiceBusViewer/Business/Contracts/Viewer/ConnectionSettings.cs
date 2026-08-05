namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Settings required to establish a connection to a Service Bus namespace and optionally select an entity.
/// </summary>
/// <param name="ConnectionString">Primary Service Bus connection string used for messaging operations.</param>
/// <param name="RootConnectionString">Optional root/management connection string for namespace-level management operations.</param>
/// <param name="QueueOrTopicName">Optional default queue or topic name to select after connecting.</param>
/// <param name="SubscriptionName">Optional subscription name when a topic subscription is selected.</param>
internal sealed record ConnectionSettings(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
