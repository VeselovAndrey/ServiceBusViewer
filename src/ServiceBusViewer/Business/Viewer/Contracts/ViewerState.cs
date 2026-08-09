namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Represents the current state of the Service Bus viewer.</summary>
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
/// <param name="ReceiveSessionId">Optional active receive session identifier for session-enabled entities.</param>
public sealed record ViewerState(
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


/// <summary>Provides extension methods related to <see cref="ViewerState"/>.</summary>
public static class ViewerStateExtensions
{
	/// <summary>Converts the current viewer session state and service bus connection into a <see cref="ViewerState"/> instance.</summary>
	/// <param name="session">The current viewer session state.</param>
	/// <param name="connection">The service bus connection.</param>
	/// <returns>A <see cref="ViewerState"/> instance representing the current state.</returns>
	public static ViewerState ToViewerState(this IViewerSessionState session, IServiceBusConnection connection)
	{
		EntityProperties? selectedEntity = session.SelectedEntityId is null
			? null
			: connection.GetEntityProperties(session.SelectedEntityId);

		bool requiresSession = RequiresSession(selectedEntity);

		return new ViewerState(
			connection.NamespaceHost,
			selectedEntity?.Name,
			(selectedEntity as SubscriptionEntityProperties)?.TopicName,
			requiresSession,
			connection.IsManagementApiAvailable,
			[.. connection.AvailableEntities.Select(x => x.ToEntityId())],
			session.CurrentMessages.Messages,
			session.CurrentMessages.HasMore,
			session.DisplayedMessage,
			session.SendResultMessage,
			requiresSession ? session.ReceiveSessionId : null);
	}

	private static bool RequiresSession(EntityProperties? entity)
		=> entity switch {
			QueueEntityProperties queue => queue.RequiresSession,
			SubscriptionEntityProperties subscription => subscription.RequiresSession,
			_ => false
		};
}