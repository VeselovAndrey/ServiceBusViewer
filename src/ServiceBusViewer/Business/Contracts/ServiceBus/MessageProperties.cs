namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Represents system properties for an outgoing Service Bus message.</summary>
internal sealed record MessageProperties
{
	public string? MessageId { get; init; }

	public string? SessionId { get; init; }

	public string? CorrelationId { get; init; }

	public string? ContentType { get; init; }

	public DateTimeOffset? ScheduledEnqueueTime { get; init; }

	public TimeSpan? TimeToLive { get; init; }
}
