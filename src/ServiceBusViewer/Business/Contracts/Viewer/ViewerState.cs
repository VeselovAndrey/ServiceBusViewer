namespace ServiceBusViewer.Business.Contracts.Viewer;

using ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>
/// Snapshot of the viewer UI state including available entities, received messages, and selection info.
/// </summary>
/// <param name="ServiceBusHostName">The host name of the connected Service Bus namespace or emulator.</param>
/// <param name="EntityName">Currently selected entity name (queue or subscription) or null if none selected.</param>
/// <param name="TopicName">When viewing a subscription, the parent topic name; otherwise null.</param>
/// <param name="RequiresSession">Whether the currently selected entity requires session-enabled receivers.</param>
/// <param name="IsManagementApiAvailable">Whether the management API is reachable for the connected namespace.</param>
/// <param name="AvailableEntities">List of available entities (queues/topics) in the namespace.</param>
/// <param name="Messages">List of messages retrieved for display in the viewer.</param>
/// <param name="HasMoreMessages">Indicates if additional messages are available beyond the returned list.</param>
/// <param name="DisplayedMessage">Message currently opened for detailed viewing, if any.</param>
/// <param name="SendResultMessage">Optional result message after a send operation (success/error).</param>
/// <param name="ReceiveSessionId">Optional active receive session id when receiving from session-enabled entities.</param>
internal sealed record ViewerState(
	string ServiceBusHostName,
	string? EntityName,
	string? TopicName,
	bool RequiresSession,
	bool IsManagementApiAvailable,
	IReadOnlyList<EntityId> AvailableEntities,
	IReadOnlyList<ReceivedMessage> Messages,
	bool HasMoreMessages,
	ReceivedMessage? DisplayedMessage,
	string? SendResultMessage,
	string? ReceiveSessionId);
