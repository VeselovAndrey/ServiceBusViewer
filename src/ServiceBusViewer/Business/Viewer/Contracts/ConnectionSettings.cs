namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Represents the settings used to connect to a Service Bus namespace.</summary>
/// <param name="ConnectionString">Primary Service Bus connection string used for messaging operations.</param>
/// <param name="EmulatorManagementConnectionString">Optional emulator connection string used for management operations.</param>
/// <param name="QueueOrTopicName">
/// Optional queue or topic name to select after connecting. In direct-entity mode, this identifies a queue unless <paramref name="SubscriptionName"/> is also provided, in which
/// case it identifies the parent topic.
/// </param>
/// <param name="SubscriptionName">Optional subscription name for direct access through its parent topic.</param>
public sealed record ConnectionSettings(
	string ConnectionString,
	string? EmulatorManagementConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
