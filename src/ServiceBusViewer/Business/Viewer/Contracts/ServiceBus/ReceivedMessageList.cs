namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents a page of received messages and whether more items are available.</summary>
/// <param name="Messages">The messages in the page.</param>
/// <param name="HasMore">True when more messages are available.</param>
/// <param name="TotalCount">Total number of messages in the source, when known from the management API; otherwise null. Set for dead-letter peeks only.</param>
public sealed record ReceivedMessageList(
	IReadOnlyList<ReceivedMessage> Messages,
	bool HasMore,
	long? TotalCount = null)
{
	/// <summary>Gets an empty message page.</summary>
	public static ReceivedMessageList Empty { get; } = new([], false);
}
