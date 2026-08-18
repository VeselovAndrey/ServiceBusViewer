namespace ServiceBusViewer.Api.Models;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>
/// Lightweight identifier for a Service Bus entity (queue, topic, subscription).
/// </summary>
/// <param name="Type">Entity kind: "Queue", "Topic" or "Subscription".</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions; null for queues/topics.</param>
public sealed record EntityId(
	string Type,
	string Name,
	string? TopicName);

/// <summary>Extension methods for <see cref="EntityId"/>.</summary>
internal static class EntityIdExtensions
{
	/// <summary>Converts a business layer <see cref="Business.Viewer.Contracts.ServiceBus.EntityId"/> to an API layer <see cref="EntityId"/>.</summary>
	/// <param name="entity">The business layer entity identifier.</param>
	/// <returns>The corresponding API layer entity identifier.</returns>
	internal static EntityId ToApiModel(this Business.Viewer.Contracts.ServiceBus.EntityId entity)
		=> entity switch {
			QueueEntityId queue => new Models.EntityId("Queue", queue.Name, null),
			TopicEntityId topic => new Models.EntityId("Topic", topic.Name, null),
			SubscriptionEntityId subscription => new Models.EntityId("Subscription", subscription.Name, subscription.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
		};
}