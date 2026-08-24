namespace ServiceBusViewer.Api.Models;

/// <summary>System properties for a received message.</summary>
/// <param name="MessageId">Message id.</param>
/// <param name="PartitionKey">Partition key if present.</param>
/// <param name="SessionId">Session id if present.</param>
/// <param name="CorrelationId">Correlation id if present.</param>
/// <param name="ContentType">Content type if present.</param>
/// <param name="DeliveryCount">Delivery count reported by Service Bus.</param>
/// <param name="EnqueuedTimeUtc">Enqueued time in UTC (ISO-8601).</param>
/// <param name="ScheduledEnqueueTime">Scheduled enqueue time (ISO-8601) or null.</param>
/// <param name="TimeToLive">Time-to-live as string or null.</param>
/// <param name="To">To property or null.</param>
/// <param name="ReplyTo">Reply To property or null.</param>
/// <param name="Subject">Subject or null.</param>
public sealed record ReceivedMessageProperties(
	string MessageId,
	string? PartitionKey,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	int DeliveryCount,
	string EnqueuedTimeUtc,
	string? ScheduledEnqueueTime,
	string? TimeToLive,
	string? To,
	string? ReplyTo,
	string? Subject);
