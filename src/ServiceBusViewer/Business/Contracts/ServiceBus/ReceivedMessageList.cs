namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Represents a page of received messages and whether more items are available.</summary>
/// <param name="Messages">The messages in the page.</param>
/// <param name="HasMore">True when more messages are available.</param>
internal sealed record ReceivedMessageList(
	IReadOnlyList<ReceivedMessage> Messages,
	bool HasMore)
{
	public static ReceivedMessageList Empty { get; } = new([], false);
}
