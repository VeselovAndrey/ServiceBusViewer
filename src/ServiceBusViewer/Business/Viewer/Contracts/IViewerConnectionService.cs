namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Provides connection lifecycle and state retrieval operations for a viewer session.</summary>
public interface IViewerConnectionService
{
	/// <summary>Gets the current connection snapshot for the viewer session.</summary>
	/// <param name="session">The viewer session state to read.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The current connection snapshot.</returns>
	Task<ViewerConnectionSnapshot> GetSnapshotAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Opens a Service Bus connection for the viewer session.</summary>
	/// <param name="session">The viewer session state to update.</param>
	/// <param name="settings">Connection settings provided by the client.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The resulting viewer state after the connection is established.</returns>
	Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings, CancellationToken cancellationToken);

	/// <summary>Disconnects the current Service Bus connection and clears connection-scoped viewer state.</summary>
	/// <param name="session">The viewer session state to reset.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The resulting disconnected snapshot.</returns>
	Task<ViewerConnectionSnapshot> DisconnectAsync(IViewerSessionState session, CancellationToken cancellationToken);

	/// <summary>Gets the current viewer state for a connected session.</summary>
	/// <param name="session">The viewer session state to read.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>The current viewer state.</returns>
	Task<ViewerState> GetCurrentStateAsync(IViewerSessionState session, CancellationToken cancellationToken);
}

/// <summary>Represents the current connection state for a viewer session.</summary>
/// <param name="Connection">Connection settings used by the viewer.</param>
/// <param name="Viewer">Current viewer state when connected; otherwise <c>null</c>.</param>
/// <param name="IsConnected">Indicates whether the viewer is currently connected to Service Bus.</param>
public sealed record ViewerConnectionSnapshot(
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
