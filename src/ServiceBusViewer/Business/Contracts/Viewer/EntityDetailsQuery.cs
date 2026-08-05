namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Query to request details for a specific Service Bus entity (queue, topic, or subscription).
/// </summary>
/// <param name="Type">Entity type (e.g. "Queue", "Topic", "Subscription").</param>
/// <param name="Name">Name of the entity to query.</param>
/// <param name="TopicName">When querying a subscription, the parent topic name; otherwise null.</param>
internal sealed record EntityDetailsQuery(
	string Type,
	string Name,
	string? TopicName);
