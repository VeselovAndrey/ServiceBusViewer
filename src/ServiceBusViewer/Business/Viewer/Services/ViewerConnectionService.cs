namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates connection lifecycle operations for a single browser session.</summary>
internal sealed class ViewerConnectionService(IServiceBusConnectionFactory connectionFactory) : IViewerConnectionService
{
	public async Task<ViewerConnectionSnapshot> GetSnapshotAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			return session.ToViewerConnectionSnapshot();
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> ConnectAsync(IViewerSessionState session, ConnectionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		await session.Gate.WaitAsync();

		try {
			if (session.IsConnected)
				throw new ViewerAlreadyConnectedException();

			IServiceBusConnection connection = await connectionFactory.OpenAsync(settings);

			try {
				session.ConnectionSettings = settings;
				session.Connection = connection;
				session.SelectedEntityId = GetInitialSelectedEntityId(settings);
				session.CurrentMessages = session.SelectedEntityId is null
					? ReceivedMessageList.Empty
					: await connection.PeekMessagesAsync(session.SelectedEntityId);
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

	public async Task<ViewerConnectionSnapshot> DisconnectAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			await session.ResetConnectionAsync();
			return session.ToViewerConnectionSnapshot();
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> GetCurrentStateAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	private static EntityId? GetInitialSelectedEntityId(ConnectionSettings settings)
		=> string.IsNullOrWhiteSpace(settings.RootConnectionString) && !string.IsNullOrWhiteSpace(settings.QueueOrTopicName)
			? string.IsNullOrWhiteSpace(settings.SubscriptionName)
				? new QueueEntityId(settings.QueueOrTopicName)
				: new SubscriptionEntityId(settings.SubscriptionName, settings.QueueOrTopicName)
			: null;
}
