namespace ServiceBusViewer.Api.Models;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>
/// Lightweight identifier for a Service Bus entity (queue, topic, subscription).
/// </summary>
/// <param name="Type">Entity kind: "Queue", "Topic" or "Subscription".</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions; null for queues/topics.</param>
internal sealed record EntityId(
	string Type,
	string Name,
	string? TopicName);

internal static class EntityIdExtensions
{
	internal static EntityId ToApiModel(this Business.Viewer.Contracts.ServiceBus.EntityId entity)
		=> entity switch {
			QueueEntityId queue => new Models.EntityId("Queue", queue.Name, null),
			TopicEntityId topic => new Models.EntityId("Topic", topic.Name, null),
			SubscriptionEntityId subscription => new Models.EntityId("Subscription", subscription.Name, subscription.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
		};
}