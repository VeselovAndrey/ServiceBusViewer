namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates connection lifecycle operations for a single browser session.</summary>
internal sealed class ViewerConnectionService(IServiceBusConnectionFactory connectionFactory) : IViewerConnectionService
{
	public async Task<ViewerConnectionSnapshot> GetSnapshotAsync(IViewerSessionState session, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			return session.ToViewerConnectionSnapshot();
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(settings);

		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			if (session.IsConnected)
				throw new ViewerAlreadyConnectedException();

			IServiceBusConnection connection = await connectionFactory.OpenAsync(settings, operationCancellationSource.Token);

			try {
				EntityId? selectedEntityId = GetInitialSelectedEntityId(settings, connection.AvailableEntities);
				ReceivedMessageList currentMessages = selectedEntityId is null
					? ReceivedMessageList.Empty
					: await connection.PeekMessagesAsync(selectedEntityId, 50, operationCancellationSource.Token);

				session.ConnectionSettings = settings;
				session.Connection = connection;
				session.SelectedEntityId = selectedEntityId;
				session.CurrentMessages = currentMessages;
				session.DisplayedMessage = null;
				session.SendResultMessage = null;
				session.ReceiveSessionId = null;

				return session.ToViewerState(connection);
			}
			catch {
				await connection.DisposeAsync();
				throw;
			}
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerConnectionSnapshot> DisconnectAsync(IViewerSessionState session, CancellationToken cancellationToken)
	{
		session.CancelActiveConnectionOperations();
		// Disconnect must complete cleanup after cancelling an in-flight operation, even when its caller aborts the HTTP request.
		await session.Gate.WaitAsync(CancellationToken.None);

		try {
			await session.ResetConnectionAsync();
			return session.ToViewerConnectionSnapshot();
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> GetCurrentStateAsync(IViewerSessionState session, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	private static EntityId? GetInitialSelectedEntityId(ConnectionSettings settings, IReadOnlyList<EntityProperties> availableEntities)
	{
		if (string.IsNullOrWhiteSpace(settings.QueueOrTopicName))
			return null;

		EntityProperties? selectedEntity;

		if (!string.IsNullOrWhiteSpace(settings.SubscriptionName)) {
			selectedEntity = availableEntities.OfType<SubscriptionEntityProperties>()
				.FirstOrDefault(subscription =>
					subscription.Name.Equals(settings.SubscriptionName, StringComparison.OrdinalIgnoreCase)
					&& subscription.TopicName.Equals(settings.QueueOrTopicName, StringComparison.OrdinalIgnoreCase));
		}
		else {
			selectedEntity = availableEntities.FirstOrDefault(entity =>
				entity is QueueEntityProperties or TopicEntityProperties
				&& entity.Name.Equals(settings.QueueOrTopicName, StringComparison.OrdinalIgnoreCase));
		}

		return selectedEntity is not null
			? selectedEntity.ToEntityId()
			: throw new ArgumentException("The requested queue, topic, or subscription was not found in the connected Service Bus namespace.");
	}
}
