namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>
/// Details about a Service Bus entity including available related entities and properties.
/// </summary>
/// <param name="ServiceBusHostName">The host name of the Service Bus namespace or emulator.</param>
/// <param name="IsManagementApiAvailable">Whether the management API is reachable for this namespace.</param>
/// <param name="AvailableEntities">List of entities available in the namespace (queues/topics).</param>
/// <param name="Type">The entity type (e.g. "Queue", "Topic", "Subscription").</param>
/// <param name="Name">The entity name.</param>
/// <param name="TopicName">Parent topic name when the entity is a subscription; otherwise null.</param>
/// <param name="Properties">Optional management properties for the entity.</param>
public sealed record EntityDetailsResult(
	string ServiceBusHostName,
	bool IsManagementApiAvailable,
	IReadOnlyList<EntityId> AvailableEntities,
	string Type,
	string Name,
	string? TopicName,
	EntityProperties? Properties);
