namespace ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

using ServiceBusViewer.Api.Models;

/// <summary>Response returned by the session-state endpoint.</summary>
/// <param name="ApplicationVersion">Application version string.</param>
/// <param name="IsRunningInContainer">Whether the application image identifies the process as containerized.</param>
/// <param name="Connection">Connection settings snapshot.</param>
/// <param name="Viewer">Current viewer state or null.</param>
/// <param name="IsConnected">Whether the viewer is currently connected.</param>
public sealed record SessionStateResponse(
	string ApplicationVersion,
	bool IsRunningInContainer,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
