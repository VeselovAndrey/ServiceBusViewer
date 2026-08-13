namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates entity selection and details for a single browser session.</summary>
internal sealed class ViewerEntityService : IViewerEntityService
{
	public async Task<ViewerState> SelectEntityAsync(IViewerSessionState session, EntityId entityId, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);

			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			connection.GetEntityProperties(entityId);
			ReceivedMessageList messages = await connection.PeekMessagesAsync(entityId, 50, operationCancellationSource.Token);

			session.SelectedEntityId = entityId;
			session.CurrentMessages = messages;
			session.DisplayedMessage = null;
			session.SendResultMessage = null;
			session.ReceiveSessionId = null;

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<EntityDetailsResult> GetDetailsAsync(IViewerSessionState session, EntityId entityId, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityProperties properties = connection.GetEntityProperties(entityId);

			return new EntityDetailsResult(
				connection.NamespaceHost,
				connection.IsManagementApiAvailable,
				[.. connection.AvailableEntities.Select(x => x.ToEntityId())],
				GetEntityType(entityId),
				entityId.Name,
				(entityId as SubscriptionEntityId)?.TopicName,
				properties);
		}
		finally {
			session.Gate.Release();
		}
	}

	private static string GetEntityType(EntityId entityId)
		=> entityId switch {
			QueueEntityId => "Queue",
			TopicEntityId => "Topic",
			SubscriptionEntityId => "Subscription",
			_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}", nameof(entityId))
		};
}
