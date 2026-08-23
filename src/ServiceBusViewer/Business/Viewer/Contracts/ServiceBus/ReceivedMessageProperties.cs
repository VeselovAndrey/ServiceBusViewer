namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents the system properties of a received message.</summary>
/// <param name="MessageId">The message id.</param>
/// <param name="PartitionKey">The partition key, if any.</param>
/// <param name="SessionId">The session id, if any.</param>
/// <param name="CorrelationId">The correlation id, if any.</param>
/// <param name="ContentType">The content type, if any.</param>
/// <param name="EnqueuedTimeUtc">The enqueue time in UTC.</param>
/// <param name="ScheduledEnqueueTime">The scheduled enqueue time, if any.</param>
/// <param name="TimeToLive">The time to live, if any.</param>
/// <param name="To">The To property, if any.</param>
/// <param name="ReplyTo">The Reply To property, if any.</param>
/// <param name="Subject">The subject, if any.</param>
public sealed record ReceivedMessageProperties(
	string MessageId,
	string? PartitionKey,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	DateTimeOffset EnqueuedTimeUtc,
	DateTimeOffset? ScheduledEnqueueTime,
	TimeSpan? TimeToLive,
	string? To,
	string? ReplyTo,
	string? Subject);
