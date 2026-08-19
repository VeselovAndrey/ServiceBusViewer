namespace ServiceBusViewer.Api.Endpoints.Connection.Disconnect;

using ServiceBusViewer.Api.Models;

/// <summary>
/// Response for the Disconnect endpoint containing connection summary and viewer state.
/// </summary>
/// <param name="ApplicationVersion">Application version string.</param>
/// <param name="Connection">Connection settings snapshot.</param>
/// <param name="Viewer">Current viewer state or null.</param>
/// <param name="IsConnected">Whether the viewer is currently connected.</param>
public sealed record DisconnectResponse(
	string ApplicationVersion,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
