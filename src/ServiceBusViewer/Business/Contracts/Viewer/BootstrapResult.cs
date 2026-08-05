namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Result of the viewer bootstrap, containing application and connection information plus optional viewer state.
/// </summary>
/// <param name="ApplicationVersion">The running application version string.</param>
/// <param name="Connection">Connection settings used by the viewer.</param>
/// <param name="Viewer">Optional current viewer state if available.</param>
/// <param name="IsConnected">Indicates whether the viewer is currently connected to Service Bus.</param>
internal sealed record BootstrapResult(
	string ApplicationVersion,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
