namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Base type for Service Bus entity identifiers.</summary>
/// <param name="Name">The entity name.</param>
public abstract record EntityId(string Name);

/// <summary>Provides extension methods related to <see cref="EntityId"/>.</summary>
public static class EntityPropertiesExtensions
{
	/// <summary>Converts an <see cref="EntityProperties"/> instance to its corresponding <see cref="EntityId"/> representation.</summary>
	/// <param name="entity">The <see cref="EntityProperties"/> instance to convert.</param>
	/// <returns>The corresponding <see cref="EntityId"/> representation.</returns>
	public static EntityId ToEntityId(this EntityProperties entity)
		=> entity switch {
			QueueEntityProperties queue => new QueueEntityId(queue.Name),
			TopicEntityProperties topic => new TopicEntityId(topic.Name),
			SubscriptionEntityProperties subscription => new SubscriptionEntityId(subscription.Name, subscription.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
		};
}