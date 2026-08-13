namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
/// <summary>Describes a cached Service Bus queue.</summary>
/// <param name="Name">The queue name.</param>
/// <param name="RequiresSession">Indicates whether the queue requires a session (guarantee the FIFO order).</param>
/// <param name="LockDuration">The lock duration.</param>
/// <param name="MaxDeliveryCount">The maximum delivery count.</param>
/// <param name="DefaultMessageTimeToLive">The default message TTL.</param>
/// <param name="RequiresDuplicateDetection">Whether duplicate detection is enabled.</param>
/// <param name="DuplicateDetectionHistoryTimeWindow">The duplicate detection window.</param>
/// <param name="DeadLetteringOnMessageExpiration">Whether expired messages are dead-lettered.</param>
/// <param name="EnableBatchedOperations">Whether batched operations are enabled.</param>
/// <param name="EnablePartitioning">Whether partitioning is enabled.</param>
/// <param name="AutoDeleteOnIdle">The auto-delete timeout.</param>
public sealed record QueueEntityProperties(
	string Name,
	bool RequiresSession,
	TimeSpan LockDuration,
	int MaxDeliveryCount,
	TimeSpan DefaultMessageTimeToLive,
	bool RequiresDuplicateDetection,
	TimeSpan DuplicateDetectionHistoryTimeWindow,
	bool DeadLetteringOnMessageExpiration,
	bool EnableBatchedOperations,
	bool EnablePartitioning,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name, RequiresSession);
