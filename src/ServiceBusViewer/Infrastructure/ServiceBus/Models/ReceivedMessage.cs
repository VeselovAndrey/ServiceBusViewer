namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents the details of a message.</summary>
/// <param name="Body">The body content of the message.</param>
/// <param name="Properties">The received message properties.</param>
/// <param name="ApplicationProperties">The application-specific properties associated with the message.</param>
public record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, object> ApplicationProperties);

/// <summary>Represents the properties of a message.</summary>
/// <param name="MessageId">The unique identifier of the message.</param>
/// <param name="PartitionKey">The partition key for the message.</param>
/// <param name="SessionId">The session identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
/// <param name="ContentType">The content type of the message body.</param>
/// <param name="EnqueuedTimeUtc">The UTC time when the message was enqueued.</param>
/// <param name="ScheduledEnqueueTime">The scheduled enqueue time, if any.</param>
/// <param name="TimeToLive">The time-to-live duration, if any.</param>
public record ReceivedMessageProperties(
	string MessageId,
	string? PartitionKey,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	DateTimeOffset EnqueuedTimeUtc,
	DateTimeOffset? ScheduledEnqueueTime,
	TimeSpan? TimeToLive);