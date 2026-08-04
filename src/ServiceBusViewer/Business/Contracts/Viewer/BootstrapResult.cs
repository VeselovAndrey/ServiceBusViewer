namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record BootstrapResult(
	string ApplicationVersion,
	ConnectionSettings Connection,
	ViewerState? Viewer,
	bool IsConnected);
