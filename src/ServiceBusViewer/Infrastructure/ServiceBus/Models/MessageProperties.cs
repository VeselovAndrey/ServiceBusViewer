namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents optional system properties applied when sending a message.</summary>
public sealed record MessageProperties
{
	/// <summary>Gets or sets the message identifier.</summary>
	public string? MessageId { get; init; }

	/// <summary>Gets or sets the session identifier.</summary>
	public string? SessionId { get; init; }

	/// <summary>Gets or sets the correlation identifier.</summary>
	public string? CorrelationId { get; init; }

	/// <summary>Gets or sets the content type of the message.</summary>
	public string? ContentType { get; init; }

	/// <summary>Gets or sets the scheduled enqueue time.</summary>
	public DateTimeOffset? ScheduledEnqueueTime { get; init; }

	/// <summary>Gets or sets the time-to-live duration for the message.</summary>
	public TimeSpan? TimeToLive { get; init; }
}
