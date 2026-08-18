namespace ServiceBusViewer.Api.Models;

/// <summary>
/// Properties describing an entity (queue/topic/subscription).
/// </summary>
/// <param name="Kind">Kind of entity: "Queue", "Topic" or "Subscription".</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions.</param>
/// <param name="LockDuration">Lock duration as string or null.</param>
/// <param name="MaxDeliveryCount">Max delivery count for the entity.</param>
/// <param name="DefaultMessageTimeToLive">Default TTL as string or null.</param>
/// <param name="RequiresDuplicateDetection">Whether duplicate detection is enabled.</param>
/// <param name="DuplicateDetectionHistoryTimeWindow">Duplicate detection history window as string or null.</param>
/// <param name="DeadLetteringOnMessageExpiration">Dead-letter on expiration enabled.</param>
/// <param name="EnableBatchedOperations">Whether batched operations are enabled.</param>
/// <param name="RequiresSession">Whether the entity requires sessions.</param>
/// <param name="EnablePartitioning">Whether partitioning is enabled.</param>
/// <param name="AutoDeleteOnIdle">Auto-delete on idle as string or null.</param>
/// <param name="Rules">Subscription rules if applicable.</param>
public sealed record EntityProperties(
	string Kind,
	string Name,
	string? TopicName,
	string? LockDuration,
	int? MaxDeliveryCount,
	string? DefaultMessageTimeToLive,
	bool? RequiresDuplicateDetection,
	string? DuplicateDetectionHistoryTimeWindow,
	bool? DeadLetteringOnMessageExpiration,
	bool? EnableBatchedOperations,
	bool? RequiresSession,
	bool? EnablePartitioning,
	string? AutoDeleteOnIdle,
	IReadOnlyList<SubscriptionRule>? Rules);
