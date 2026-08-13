namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Describes a cached Service Bus topic.</summary>
/// <param name="Name">The topic name.</param>
/// <param name="DefaultMessageTimeToLive">The default message TTL.</param>
/// <param name="RequiresDuplicateDetection">Whether duplicate detection is enabled.</param>
/// <param name="DuplicateDetectionHistoryTimeWindow">The duplicate detection window.</param>
/// <param name="EnableBatchedOperations">Whether batched operations are enabled.</param>
/// <param name="EnablePartitioning">Whether partitioning is enabled.</param>
/// <param name="AutoDeleteOnIdle">The auto-delete timeout.</param>
public sealed record TopicEntityProperties(
	string Name,
	TimeSpan DefaultMessageTimeToLive,
	bool RequiresDuplicateDetection,
	TimeSpan DuplicateDetectionHistoryTimeWindow,
	bool EnableBatchedOperations,
	bool EnablePartitioning,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name, RequiresSession: false);
