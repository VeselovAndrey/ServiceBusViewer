namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record ReceiveCommand(string? ReceiveSessionId);
