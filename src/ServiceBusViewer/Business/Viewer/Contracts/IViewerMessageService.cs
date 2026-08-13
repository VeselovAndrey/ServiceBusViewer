namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides message operations for the selected entity in a viewer session.</summary>
public interface IViewerMessageService
{
	/// <summary>Refreshes the message list for the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The refreshed viewer state.</returns>
	Task<ViewerState> RefreshAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Receives the next message from the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="sessionId">Optional Service Bus session identifier used when the selected entity is session-enabled.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>TThe viewer state after attempting to receive a message.</returns>
	Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId, CancellationToken cancellationToken);

	/// <summary>Sends a message to the currently selected entity.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="command">The message body and properties to send.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	Task SendAsync(IViewerSessionState session, SendCommand command, CancellationToken cancellationToken);
}
