namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents system properties for an outgoing Service Bus message.</summary>
public sealed record MessageProperties
{
	/// <summary>Gets the message identifier to assign to the outgoing message.</summary>
	public string? MessageId { get; init; }

	/// <summary>Gets the Service Bus session identifier for the outgoing message.</summary>
	public string? SessionId { get; init; }

	/// <summary>Gets the partition key for the outgoing message.</summary>
	public string? PartitionKey { get; init; }

	/// <summary>Gets the correlation identifier for the outgoing message.</summary>
	public string? CorrelationId { get; init; }

	/// <summary>Gets the content type for the outgoing message body.</summary>
	public string? ContentType { get; init; }

	/// <summary>Gets the scheduled enqueue time for deferred delivery.</summary>
	public DateTimeOffset? ScheduledEnqueueTime { get; init; }

	/// <summary>Gets the time-to-live value for the outgoing message.</summary>
	public TimeSpan? TimeToLive { get; init; }

	/// <summary>Gets the To property for the outgoing message.</summary>
	public string? To { get; init; }

	/// <summary>Gets the Reply-To property for the outgoing message.</summary>
	public string? ReplyTo { get; init; }

	/// <summary>Gets the subject for the outgoing message.</summary>
	public string? Subject { get; init; }
}
