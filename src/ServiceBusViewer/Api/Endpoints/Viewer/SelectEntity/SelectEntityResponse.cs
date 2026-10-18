namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using ServiceBusViewer.Api.Models;

/// <summary>
/// Response returned after selecting an entity; contains updated viewer state.
/// </summary>
/// <param name="ServiceBusHostName">Host name of the connected Service Bus.</param>
/// <param name="EntityName">Selected entity name or null.</param>
/// <param name="TopicName">Topic name when applicable.</param>
/// <param name="RequiresSession">Whether the selected entity requires sessions.</param>
/// <param name="IsManagementApiAvailable">Whether management API is available.</param>
/// <param name="AvailableEntities">Available entities to choose from.</param>
/// <param name="Messages">Messages returned for the entity.</param>
/// <param name="HasMoreMessages">Indicates if more messages are available.</param>
/// <param name="DisplayedMessage">Currently displayed message or null.</param>
/// <param name="SendResultMessage">Result message from the last send operation or null.</param>
/// <param name="ReceiveSessionId">Session id used for receive or null.</param>
/// <param name="TotalMessageCount">Total number of messages in the selected entity (dead-letter count for a dead-letter list), when known from the management API; otherwise null.</param>
/// <param name="UsesDevelopmentEmulator">Whether the connection targets the local Service Bus emulator, whose runtime message counts are unavailable.</param>
public sealed record SelectEntityResponse(
	string ServiceBusHostName,
	string? EntityName,
	string? TopicName,
	bool RequiresSession,
	bool IsManagementApiAvailable,
	IReadOnlyList<AvailableEntity> AvailableEntities,
	IReadOnlyList<ReceivedMessage> Messages,
	bool HasMoreMessages,
	ReceivedMessage? DisplayedMessage,
	string? SendResultMessage,
	string? ReceiveSessionId,
	long? TotalMessageCount,
	bool UsesDevelopmentEmulator)
	: ViewerState(
		ServiceBusHostName,
		EntityName,
		TopicName,
		RequiresSession,
		IsManagementApiAvailable,
		AvailableEntities,
		Messages,
		HasMoreMessages,
		DisplayedMessage,
		SendResultMessage,
		ReceiveSessionId,
		TotalMessageCount,
		UsesDevelopmentEmulator);
