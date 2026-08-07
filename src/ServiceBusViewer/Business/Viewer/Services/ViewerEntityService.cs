namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates entity selection and details for a single browser session.</summary>
internal sealed class ViewerEntityService : IViewerEntityService
{
	public async Task<ViewerState> SelectEntityAsync(IViewerSessionState session, EntityId entityId)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = ViewerSessionStateMapper.EnsureConnected(session);
			connection.GetEntityProperties(entityId);

			session.SelectedEntityId = entityId;
			session.DisplayedMessage = null;
			session.SendResultMessage = null;
			session.ReceiveSessionId = null;
			session.CurrentMessages = await connection.PeekMessagesAsync(entityId);

			return ViewerSessionStateMapper.ToViewerState(session, connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<EntityDetailsResult> GetDetailsAsync(IViewerSessionState session, EntityId entityId)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = ViewerSessionStateMapper.EnsureConnected(session);
			EntityProperties properties = connection.GetEntityProperties(entityId);

			return new EntityDetailsResult(
				connection.NamespaceHost,
				connection.IsManagementApiAvailable,
				[.. connection.AvailableEntities.Select(ViewerSessionStateMapper.ToEntityId)],
				ViewerSessionStateMapper.GetEntityType(entityId),
				entityId.Name,
				(entityId as SubscriptionEntityId)?.TopicName,
				properties);
		}
		finally {
			session.Gate.Release();
		}
	}
}
