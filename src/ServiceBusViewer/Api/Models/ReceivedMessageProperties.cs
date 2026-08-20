namespace ServiceBusViewer.Api.Models;

/// <summary>
/// System properties for a received message.
/// </summary>
/// <param name="MessageId">Message id.</param>
/// <param name="PartitionKey">Partition key if present.</param>
/// <param name="SessionId">Session id if present.</param>
/// <param name="CorrelationId">Correlation id if present.</param>
/// <param name="ContentType">Content type if present.</param>
/// <param name="EnqueuedTimeUtc">Enqueued time in UTC (ISO-8601).</param>
/// <param name="ScheduledEnqueueTime">Scheduled enqueue time (ISO-8601) or null.</param>
/// <param name="TimeToLive">Time-to-live as string or null.</param>
public sealed record ReceivedMessageProperties(
	string MessageId,
	string? PartitionKey,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	string EnqueuedTimeUtc,
	string? ScheduledEnqueueTime,
	string? TimeToLive);
