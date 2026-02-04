namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents the result of a message list operation.</summary>
/// <param name="Messages">The list of messages retrieved.</param>
/// <param name="HasMore">Indicates whether there are more messages available beyond the returned list.</param>
public record MessageList(
	IReadOnlyList<MessageDetails> Messages,
	bool HasMore)
{
	/// <summary>Gets an empty result with no messages.</summary>
	public static MessageList Empty { get; } = new([], false);
}
