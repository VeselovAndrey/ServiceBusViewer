namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
/// <summary>Describes a cached Service Bus subscription.</summary>
/// <param name="Name">The subscription name.</param>
/// <param name="TopicName">The parent topic name.</param>
/// <param name="LockDuration">The lock duration.</param>
/// <param name="MaxDeliveryCount">The maximum delivery count.</param>
/// <param name="DefaultMessageTimeToLive">The default message TTL.</param>
/// <param name="DeadLetteringOnMessageExpiration">Whether expired messages are dead-lettered.</param>
/// <param name="RequiresSession">Whether sessions are required.</param>
/// <param name="EnableBatchedOperations">Whether batched operations are enabled.</param>
/// <param name="AutoDeleteOnIdle">The auto-delete timeout.</param>
/// <param name="Rules">The subscription rules.</param>
public sealed record SubscriptionEntityProperties(
	string Name,
	string TopicName,
	TimeSpan LockDuration,
	int MaxDeliveryCount,
	TimeSpan DefaultMessageTimeToLive,
	bool DeadLetteringOnMessageExpiration,
	bool RequiresSession,
	bool EnableBatchedOperations,
	TimeSpan AutoDeleteOnIdle,
	IReadOnlyList<SubscriptionFilterRule> Rules) : EntityProperties(Name);
