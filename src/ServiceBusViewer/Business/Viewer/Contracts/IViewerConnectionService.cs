namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>
/// Coordinates connection lifecycle and bootstrap operations for a single browser session.
/// </summary>
public interface IViewerConnectionService
{
	Task<BootstrapResult> GetBootstrapAsync(IViewerSessionState session);

	Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings);

	Task<BootstrapResult> DisconnectAsync(IViewerSessionState session);

	Task<ViewerState> GetCurrentAsync(IViewerSessionState session);
}
