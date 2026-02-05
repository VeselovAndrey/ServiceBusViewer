namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Base class for Service Bus entity properties.</summary>
public abstract record EntityProperties(string Name);

/// <summary>Queue properties.</summary>
/// <param name="Name">Queue name.</param>
/// <param name="LockDuration">Lock duration.</param>
/// <param name="MaxDeliveryCount">Maximum delivery count before dead-lettering.</param>
/// <param name="DefaultMessageTimeToLive">Default message TTL.</param>
/// <param name="RequiresDuplicateDetection">Indicates whether duplicate detection is required.</param>
/// <param name="DuplicateDetectionHistoryTimeWindow">Duplicate detection history window.</param>
/// <param name="DeadLetteringOnMessageExpiration">Dead-letter on expiration.</param>
/// <param name="EnableBatchedOperations">Enable batched operations.</param>
/// <param name="RequiresSession">Indicates whether sessions are required.</param>
/// <param name="EnablePartitioning">Indicates whether partitioning is enabled.</param>
/// <param name="AutoDeleteOnIdle">The <see cref="TimeSpan"/> idle interval after which the queue is automatically deleted.</param>
public sealed record QueueEntityProperties(
	string Name,
	TimeSpan LockDuration,
	int MaxDeliveryCount,
	TimeSpan DefaultMessageTimeToLive,
	bool RequiresDuplicateDetection,
	TimeSpan DuplicateDetectionHistoryTimeWindow,
	bool DeadLetteringOnMessageExpiration,
	bool EnableBatchedOperations,
	bool RequiresSession,
	bool EnablePartitioning,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name);

/// <summary>Topic properties.</summary>
/// <param name="Name">Topic name.</param>
/// <param name="DefaultMessageTimeToLive">Default message TTL.</param>
/// <param name="RequiresDuplicateDetection">Indicates whether duplicate detection is required.</param>
/// <param name="DuplicateDetectionHistoryTimeWindow">Duplicate detection history window.</param>
/// <param name="EnableBatchedOperations">Enable batched operations.</param>
/// <param name="EnablePartitioning">Indicates whether partitioning is enabled.</param>
/// <param name="AutoDeleteOnIdle">The <see cref="TimeSpan"/> idle interval after which the queue is automatically deleted.</param>
public sealed record TopicEntityProperties(
	string Name,
	TimeSpan DefaultMessageTimeToLive,
	bool RequiresDuplicateDetection,
	TimeSpan DuplicateDetectionHistoryTimeWindow,
	bool EnableBatchedOperations,
	bool EnablePartitioning,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name);

/// <summary>Subscription properties.</summary>
/// <param name="Name">Subscription name.</param>
/// <param name="TopicName">Parent topic name.</param>
/// <param name="LockDuration">Lock duration.</param>
/// <param name="MaxDeliveryCount">Maximum delivery count before dead-lettering.</param>
/// <param name="DefaultMessageTimeToLive">Default message TTL.</param>
/// <param name="DeadLetteringOnMessageExpiration">Dead-letter on expiration.</param>
/// <param name="RequiresSession">Indicates whether sessions are required.</param>
/// <param name="EnableBatchedOperations">Enable batched operations.</param>
/// <param name="AutoDeleteOnIdle">The <see cref="TimeSpan"/> idle interval after which the queue is automatically deleted.</param>
public sealed record SubscriptionEntityProperties(
	string Name,
	string TopicName,
	TimeSpan LockDuration,
	int MaxDeliveryCount,
	TimeSpan DefaultMessageTimeToLive,
	bool DeadLetteringOnMessageExpiration,
	bool RequiresSession,
	bool EnableBatchedOperations,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name);
