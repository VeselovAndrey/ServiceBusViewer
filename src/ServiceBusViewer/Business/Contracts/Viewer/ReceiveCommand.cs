namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Command to receive messages from the currently selected entity. Optionally accepts a session id for session receivers.
/// </summary>
/// <param name="ReceiveSessionId">Optional session id to receive from when the entity requires sessions.</param>
internal sealed record ReceiveCommand(string? ReceiveSessionId);
