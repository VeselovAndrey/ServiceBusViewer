namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>
/// Coordinates message-oriented operations for the selected entity in a single browser session.
/// </summary>
public interface IViewerMessageService
{
	Task<ViewerState> RefreshAsync(IViewerSessionState session);

	Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId);

	Task<ViewerState> SendAsync(IViewerSessionState session, SendCommand command);
}
