namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents information about a peeked message.</summary>
/// <param name="MessageId">The unique identifier of the message.</param>
/// <param name="EnqueuedTimeUtc">The UTC time when the message was enqueued.</param>
public record PeekedMessageInfo(
	string MessageId,
	DateTimeOffset EnqueuedTimeUtc);