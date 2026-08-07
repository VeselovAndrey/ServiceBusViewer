namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides connection lifecycle and state retrieval operations for a viewer session.</summary>
public interface IViewerConnectionService
{
	/// <summary>Gets the current connection snapshot for the viewer session.</summary>
	/// <param name="session">The viewer session state to read.</param>
	/// <returns>The current connection snapshot.</returns>
	Task<ViewerConnectionSnapshot> GetSnapshotAsync(IViewerSessionState session);

	/// <summary>Opens a Service Bus connection for the viewer session.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="settings">Connection settings provided by the client.</param>
	/// <returns>The resulting viewer state after the connection is established.</returns>
	Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings);

	/// <summary>Disconnects the current Service Bus connection and clears connection-scoped viewer state.</summary>
	/// <param name="session">The viewer session state to reset.</param>
	/// <returns>The resulting disconnected snapshot.</returns>
	Task<ViewerConnectionSnapshot> DisconnectAsync(IViewerSessionState session);

	/// <summary>Gets the current viewer state for a connected session.</summary>
	/// <param name="session">The viewer session state to read.</param>
	/// <returns>The current viewer state.</returns>
	Task<ViewerState> GetCurrentAsync(IViewerSessionState session);
}
