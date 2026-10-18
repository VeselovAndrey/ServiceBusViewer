namespace ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

using ServiceBusViewer.Api.Models;

/// <summary>Response returned by the session-state endpoint.</summary>
/// <param name="ApplicationVersion">Application version string.</param>
/// <param name="IsRunningInContainer">Whether the application image identifies the process as containerized.</param>
/// <param name="ShowEmulatorConnectHint">Whether the emulator connect hint message and insert button are shown on the connect screen.</param>
/// <param name="Connection">Connection settings snapshot, or null when the session's settings do not match the application-configured settings.</param>
/// <param name="Viewer">Current viewer state or null.</param>
/// <param name="IsConnected">Whether the viewer is currently connected.</param>
/// <param name="EntityCountAutoRefreshEnabled">Whether the sidebar refresh control offers automatic entity message-count refresh.</param>
public sealed record SessionStateResponse(
	string ApplicationVersion,
	bool IsRunningInContainer,
	bool ShowEmulatorConnectHint,
	ConnectionSettings? Connection,
	ViewerState? Viewer,
	bool IsConnected,
	bool EntityCountAutoRefreshEnabled);
