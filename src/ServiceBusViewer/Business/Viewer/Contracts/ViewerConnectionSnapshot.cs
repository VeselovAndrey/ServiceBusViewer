namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Represents the current connection state for a viewer session.</summary>
/// <param name="Connection">Connection settings used by the viewer.</param>
/// <param name="Viewer">Current viewer state when connected; otherwise <c>null</c>.</param>
/// <param name="IsConnected">Indicates whether the viewer is currently connected to Service Bus.</param>
public sealed record ViewerConnectionSnapshot(
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
