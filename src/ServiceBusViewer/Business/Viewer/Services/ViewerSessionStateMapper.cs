namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

internal static class ViewerSessionStateMapper
{
	public static BootstrapResult ToBootstrapResult(IViewerSessionState session, string applicationVersion)
	{
		return new BootstrapResult(
			applicationVersion,
			session.ConnectionSettings,
			session.IsConnected ? ToViewerState(session, session.Connection!) : null,
			session.IsConnected);
	}

	public static ViewerState ToViewerState(IViewerSessionState session, IServiceBusConnection connection)
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
			[.. connection.AvailableEntities.Select(ToEntityId)],
			session.CurrentMessages.Messages,
			session.CurrentMessages.HasMore,
			session.DisplayedMessage,
			session.SendResultMessage,
			requiresSession ? session.ReceiveSessionId : null);
	}

	public static IServiceBusConnection EnsureConnected(IViewerSessionState session)
	{
		return session.Connection
			?? throw new ApiProblemException(StatusCodes.Status409Conflict, "Not connected", "Not connected to any Service Bus instance.");
	}

	public static EntityId EnsureSelectedEntity(IViewerSessionState session)
	{
		return session.SelectedEntityId
			?? throw new InvalidOperationException("No entity selected.");
	}

	public static string GetEntityType(EntityId entityId) => entityId switch {
		QueueEntityId => "Queue",
		TopicEntityId => "Topic",
		SubscriptionEntityId => "Subscription",
		_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}", nameof(entityId))
	};

	public static string BuildSendResultMessage(string? sentContentType, string? sentMessageId)
	{
		string contentTypeDescription = string.IsNullOrWhiteSpace(sentContentType)
			? "without a content type"
			: $"with content type '{sentContentType}'";

		return string.IsNullOrWhiteSpace(sentMessageId)
			? $"Message sent successfully {contentTypeDescription}."
			: $"Message '{sentMessageId}' sent successfully {contentTypeDescription}.";
	}

	public static ConnectionSettings NormalizeConnectionSettings(ConnectionSettings settings)
	{
		return new ConnectionSettings(
			settings.ConnectionString,
			settings.RootConnectionString,
			settings.RootConnectionString is null ? settings.QueueOrTopicName : null,
			settings.RootConnectionString is null ? settings.SubscriptionName : null);
	}

	public static EntityId? GetInitialSelectedEntityId(ConnectionSettings settings)
	{
		if (string.IsNullOrWhiteSpace(settings.RootConnectionString) && !string.IsNullOrWhiteSpace(settings.QueueOrTopicName)) {
			return string.IsNullOrWhiteSpace(settings.SubscriptionName)
				? new QueueEntityId(settings.QueueOrTopicName)
				: new SubscriptionEntityId(settings.SubscriptionName, settings.QueueOrTopicName);
		}

		return null;
	}

	public static bool SelectedEntityRequiresSession(IViewerSessionState session, IServiceBusConnection connection)
	{
		return RequiresSession(connection.GetEntityProperties(EnsureSelectedEntity(session)));
	}

	public static EntityId ToEntityId(EntityProperties entity) => entity switch {
		QueueEntityProperties queue => new QueueEntityId(queue.Name),
		TopicEntityProperties topic => new TopicEntityId(topic.Name),
		SubscriptionEntityProperties subscription => new SubscriptionEntityId(subscription.Name, subscription.TopicName),
		_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
	};

	private static bool RequiresSession(EntityProperties? entity)
	{
		return entity switch {
			QueueEntityProperties queue => queue.RequiresSession,
			SubscriptionEntityProperties subscription => subscription.RequiresSession,
			_ => false
		};
	}
}
