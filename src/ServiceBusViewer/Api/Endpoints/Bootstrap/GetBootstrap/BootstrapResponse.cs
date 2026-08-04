namespace ServiceBusViewer.Api.Endpoints.Bootstrap.GetBootstrap;

using ServiceBusViewer.Api.Models;

/// <summary>
/// Response returned by the bootstrap endpoint with initial connection and viewer state.
/// </summary>
/// <param name="ApplicationVersion">Application version string.</param>
/// <param name="Connection">Connection settings snapshot.</param>
/// <param name="Viewer">Initial viewer state or null.</param>
/// <param name="IsConnected">Whether the viewer is currently connected.</param>
internal sealed record BootstrapResponse(
	string ApplicationVersion,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
