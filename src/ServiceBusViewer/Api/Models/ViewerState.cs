namespace ServiceBusViewer.Api.Models;
/// <summary>
/// Common viewer state returned by viewer-related endpoints.
/// </summary>
/// <param name="ServiceBusHostName">Host name of the connected Service Bus namespace.</param>
/// <param name="EntityName">Currently selected entity name (queue or subscription) or null.</param>
/// <param name="TopicName">Topic name when a subscription is selected, otherwise null.</param>
/// <param name="RequiresSession">Whether the selected entity requires sessions.</param>
/// <param name="IsManagementApiAvailable">Whether management API is available for entity discovery.</param>
/// <param name="AvailableEntities">List of available entities.</param>
/// <param name="Messages">List of received messages for the selected entity.</param>
/// <param name="HasMoreMessages">Indicates if more messages are available to fetch.</param>
/// <param name="DisplayedMessage">Currently selected/displayed message or null.</param>
/// <param name="SendResultMessage">Result message from the last send operation or null.</param>
/// <param name="ReceiveSessionId">Session id currently used for receive operations or null.</param>
public record ViewerState(
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



/// <summary>Extension methods for <see cref="ViewerState"/>.</summary>
internal static class ViewerStateExtensions
{
	/// <summary>Converts a business model <see cref="Business.Viewer.Contracts.ViewerState"/> to an API model <see cref="ViewerState"/>.</summary>
	/// <param name="state">The business model viewer state to convert.</param>
	/// <returns>The converted API model viewer state.</returns>
	internal static ViewerState ToApiModel(this Business.Viewer.Contracts.ViewerState state)
		=> new ViewerState(
			state.ServiceBusHostName,
			state.EntityName,
			state.TopicName,
			state.RequiresSession,
			state.IsManagementApiAvailable,
			[.. state.AvailableEntities.Select(e => e.ToApiModel())],
			[.. state.Messages.Select(m => m.ToApiModel())],
			state.HasMoreMessages,
			state.DisplayedMessage?.ToApiModel(),
			state.SendResultMessage,
			state.ReceiveSessionId);
}