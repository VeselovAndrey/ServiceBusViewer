namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides message operations for the selected entity in a viewer session.</summary>
public interface IViewerMessageService
{
	/// <summary>Refreshes the message list for the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The refreshed viewer state.</returns>
	Task<ViewerState> RefreshAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Refreshes the dead-letter list for the currently selected entity.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The viewer state carrying the selected entity's dead-letter messages.</returns>
	Task<ViewerState> RefreshDeadLetterMessagesAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Republishes the selected entity's dead-lettered messages to the original entity.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="messageId">Message id of the dead-lettered message to republish, or <c>null</c> to republish every message currently in the dead-letter queue.</param>
	/// <param name="sequenceNumber">
	/// The exact broker sequence number of the single dead-lettered message to republish, or <c>null</c> to keep the message-id-based or bulk behavior.
	/// When set, it selects exactly one message independently of its message id.
	/// </param>
	/// <param name="strategy">Message id strategy as resolved by the viewer strategy prompt.</param>
	/// <param name="manualMessageId">The manually provided message id when the strategy is Manual.</param>
	/// <param name="maxMessages">
	/// Maximum number of messages a bulk republish processes in this call, returning the partial outcome; <c>null</c> keeps the unbounded behavior.
	/// Applies only to bulk republishes (<paramref name="messageId"/> is <c>null</c>).
	/// </param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The outcome summary of the republish operation.</returns>
	Task<DeadLetterOperationResult> RepublishDeadLetterMessagesAsync(IViewerSessionState session, string? messageId, long? sequenceNumber, RepublishMessageIdStrategy strategy, string? manualMessageId, int? maxMessages, CancellationToken cancellationToken);

	/// <summary>Permanently deletes the selected entity's dead-lettered messages without re-delivering them.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="messageId">Message id of the dead-lettered message to drop, or <c>null</c> to drop every message currently in the dead-letter queue.</param>
	/// <param name="sequenceNumber">
	/// The exact broker sequence number of the single dead-lettered message to drop, or <c>null</c> to keep the message-id-based or bulk behavior.
	/// When set, it selects exactly one message independently of its message id.
	/// </param>
	/// <param name="maxMessages">
	/// Maximum number of messages a bulk drop processes in this call, returning the partial outcome; <c>null</c> keeps the unbounded behavior.
	/// Applies only to bulk drops (<paramref name="messageId"/> is <c>null</c>).
	/// </param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The outcome summary of the drop operation.</returns>
	Task<DeadLetterOperationResult> DropDeadLetterMessagesAsync(IViewerSessionState session, string? messageId, long? sequenceNumber, int? maxMessages, CancellationToken cancellationToken);

	/// <summary>Receives the next message from the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="sessionId">Optional Service Bus session identifier used when the selected entity is session-enabled.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>TThe viewer state after attempting to receive a message.</returns>
	Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId, CancellationToken cancellationToken);

	/// <summary>Removes the last received message from the session so a restored client session does not redisplay it.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The viewer state after clearing the displayed message.</returns>
	Task<ViewerState> ClearDisplayedMessageAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Updates the number of messages peeked for the viewer message list and re-peeks the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="peekMessageCount">Number of messages to peek for the viewer message list; must be a whole number from 1 through 500.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The viewer state after applying the new peek message count.</returns>
	Task<ViewerState> UpdatePeekMessageCountAsync(IViewerSessionState session, int peekMessageCount, CancellationToken cancellationToken);

	/// <summary>Sends a message to the currently selected entity.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="command">The message body and properties to send.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	Task SendAsync(IViewerSessionState session, SendCommand command, CancellationToken cancellationToken);
}
