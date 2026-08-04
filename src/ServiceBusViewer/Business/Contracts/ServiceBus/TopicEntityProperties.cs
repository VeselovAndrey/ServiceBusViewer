namespace ServiceBusViewer.Business.Contracts.ServiceBus;

internal sealed record TopicEntityProperties(
	string Name,
	TimeSpan DefaultMessageTimeToLive,
	bool RequiresDuplicateDetection,
	TimeSpan DuplicateDetectionHistoryTimeWindow,
	bool EnableBatchedOperations,
	bool EnablePartitioning,
	TimeSpan AutoDeleteOnIdle) : EntityProperties(Name);
