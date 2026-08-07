namespace ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

using ServiceBusViewer.Api.Models;

/// <summary>Response returned by the session-state endpoint.</summary>
/// <param name="ApplicationVersion">Application version string.</param>
/// <param name="Connection">Connection settings snapshot.</param>
/// <param name="Viewer">Current viewer state or null.</param>
/// <param name="IsConnected">Whether the viewer is currently connected.</param>
internal sealed record SessionStateResponse(
	string ApplicationVersion,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
