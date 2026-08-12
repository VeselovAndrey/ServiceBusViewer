namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Represents the settings used to connect to a Service Bus namespace.</summary>
/// <param name="ConnectionString">Primary Service Bus connection string used for messaging operations.</param>
/// <param name="EmulatorManagementConnectionString">Optional emulator connection string used for management operations.</param>
/// <param name="QueueOrTopicName">Optional default queue or topic name to select after connecting.</param>
/// <param name="SubscriptionName">Optional subscription name when a topic subscription is selected.</param>
public sealed record ConnectionSettings(
	string ConnectionString,
	string? EmulatorManagementConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
