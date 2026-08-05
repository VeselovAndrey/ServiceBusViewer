namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Request to connect the viewer to a Service Bus namespace and optionally select an entity.
/// </summary>
/// <param name="ConnectionString">Primary Service Bus connection string used for sending/receiving.</param>
/// <param name="RootConnectionString">Optional root/management connection string for namespace-level operations (e.g. emulator or management API).</param>
/// <param name="QueueOrTopicName">Optional queue or topic name to select after connecting.</param>
/// <param name="SubscriptionName">Optional subscription name when selecting a topic subscription.</param>
internal sealed record ConnectCommand(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
