namespace ServiceBusViewer.Api.Converters;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Creates strongly typed Service Bus entity identifiers from API request values.</summary>
internal static class EntityIdFactory
{
	/// <summary>Creates an entity identifier for a queue, topic, or subscription.</summary>
	/// <param name="entityType">The entity type to create.</param>
	/// <param name="entityName">The name of the entity.</param>
	/// <param name="topicName">The parent topic name for a subscription.</param>
	/// <param name="topicNameField">The request field name used when reporting a missing topic name.</param>
	/// <returns>The strongly typed entity identifier.</returns>
	internal static EntityId Create(string entityType, string entityName, string? topicName)
	{
		ReadOnlySpan<char> normalizedEntityType = entityType.AsSpan().Trim();

		if (normalizedEntityType.Equals("Queue", StringComparison.OrdinalIgnoreCase))
			return new QueueEntityId(entityName);

		if (normalizedEntityType.Equals("Topic", StringComparison.OrdinalIgnoreCase))
			return new TopicEntityId(entityName);

		if (!normalizedEntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException($"Unsupported entity type '{entityType}'.", nameof(entityType));

		return topicName is not null
			? new SubscriptionEntityId(entityName, topicName)
			: throw new ArgumentException("Topic name is required for a subscription.");
	}
}
