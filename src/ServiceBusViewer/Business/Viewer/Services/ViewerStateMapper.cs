namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

internal static class ViewerStateMapper
{
	internal static ViewerState ToViewerState(this IViewerSessionState session, IServiceBusConnection connection)
	{
		EntityProperties? selectedEntity = session.SelectedEntityId is not null
			? connection.GetEntityProperties(session.SelectedEntityId)
			: null;

		bool requiresSession = selectedEntity?.RequiresSession ?? false;

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

}
