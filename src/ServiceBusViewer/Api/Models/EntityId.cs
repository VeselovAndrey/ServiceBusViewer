namespace ServiceBusViewer.Api.Models;

/// <summary>Lightweight identifier for a Service Bus entity (queue, topic, subscription).</summary>
/// <param name="Type">Entity kind: "Queue", "Topic" or "Subscription".</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions; null for queues/topics.</param>
public sealed record EntityId(
	string Type,
	string Name,
	string? TopicName);
