namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides message operations for the selected entity in a viewer session.</summary>
public interface IViewerMessageService
{
	/// <summary>Refreshes the message list for the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <returns>The refreshed viewer state.</returns>
	Task<ViewerState> RefreshAsync(IViewerSessionState session);

	/// <summary>Receives the next message from the currently selected entity.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="sessionId">Optional Service Bus session identifier used when the selected entity requires sessions.</param>
	/// <returns>The viewer state after the receive attempt.</returns>
	Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId);

	/// <summary>Sends a message to the currently selected entity.</summary>
	/// <param name="session">The viewer session state to use.</param>
	/// <param name="command">The message body and properties to send.</param>
	/// <returns>The viewer state after the send completes.</returns>
	Task<ViewerState> SendAsync(IViewerSessionState session, SendCommand command);
}
