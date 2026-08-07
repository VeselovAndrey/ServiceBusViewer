namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents system properties for an outgoing Service Bus message.</summary>
public sealed record MessageProperties
{
	public string? MessageId { get; init; }

	public string? SessionId { get; init; }

	public string? CorrelationId { get; init; }

	public string? ContentType { get; init; }

	public DateTimeOffset? ScheduledEnqueueTime { get; init; }

	public TimeSpan? TimeToLive { get; init; }
}
