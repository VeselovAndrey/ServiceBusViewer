namespace ServiceBusViewer.UnitTests.Assets;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

internal static class EntitiesData
{
	public static QueueEntityProperties CreateQueueProperties(string name, bool requiresSession = false)
		=> new QueueEntityProperties(
			Name: name,
			RequiresSession: requiresSession,
			LockDuration: TimeSpan.FromMinutes(1),
			MaxDeliveryCount: 10,
			DefaultMessageTimeToLive: TimeSpan.FromDays(1),
			RequiresDuplicateDetection: false,
			DuplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(10),
			DeadLetteringOnMessageExpiration: false,
			EnableBatchedOperations: true,
			EnablePartitioning: false,
			AutoDeleteOnIdle: TimeSpan.FromDays(30));
}
