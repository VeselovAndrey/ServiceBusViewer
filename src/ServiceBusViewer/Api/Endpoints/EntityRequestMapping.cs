namespace ServiceBusViewer.Api.Endpoints;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

internal static class EntityRequestMapping
{
	internal static EntityId CreateEntityId(string entityType, string entityName, string? topicName, string topicNameField)
	{
		return NormalizeEntityType(entityType) switch {
			"Queue" => new QueueEntityId(entityName),
			"Topic" => new TopicEntityId(entityName),
			"Subscription" when topicName is not null => new SubscriptionEntityId(entityName, topicName),
			"Subscription" => throw ApiProblemException.Validation(new Dictionary<string, string[]> {
				[topicNameField] = ["Topic name is required when selecting a subscription."]
			}),
			_ => throw new ApiProblemException(StatusCodes.Status400BadRequest, "Invalid entity type", $"Unsupported entity type '{entityType}'.")
		};
	}

	internal static string NormalizeEntityType(string entityType)
	{
		return entityType.Trim().ToLowerInvariant() switch {
			"queue" => "Queue",
			"topic" => "Topic",
			"subscription" => "Subscription",
			_ => throw new ApiProblemException(StatusCodes.Status400BadRequest, "Invalid entity type", $"Unsupported entity type '{entityType}'.")
		};
	}
}
