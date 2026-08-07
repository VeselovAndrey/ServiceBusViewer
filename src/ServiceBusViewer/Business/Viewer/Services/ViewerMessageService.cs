namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates message operations for the selected entity in a single browser session.</summary>
internal sealed class ViewerMessageService : IViewerMessageService
{
	public async Task<ViewerState> RefreshAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = ViewerSessionStateMapper.EnsureConnected(session);

			session.DisplayedMessage = null;
			session.SendResultMessage = null;
			session.CurrentMessages = session.SelectedEntityId is null
				? ReceivedMessageList.Empty
				: await connection.PeekMessagesAsync(session.SelectedEntityId);

			if (session.SelectedEntityId is null || !ViewerSessionStateMapper.SelectedEntityRequiresSession(session, connection))
				session.ReceiveSessionId = null;

			return ViewerSessionStateMapper.ToViewerState(session, connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = ViewerSessionStateMapper.EnsureConnected(session);
			EntityId entityId = ViewerSessionStateMapper.EnsureSelectedEntity(session);
			bool requiresSession = ViewerSessionStateMapper.SelectedEntityRequiresSession(session, connection);

			session.ReceiveSessionId = requiresSession ? sessionId : null;
			session.DisplayedMessage = await connection.ReceiveMessageAsync(entityId, requiresSession ? sessionId : null);
			session.SendResultMessage = null;
			session.CurrentMessages = await connection.PeekMessagesAsync(entityId);

			return ViewerSessionStateMapper.ToViewerState(session, connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	public async Task<ViewerState> SendAsync(IViewerSessionState session, SendCommand command)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = ViewerSessionStateMapper.EnsureConnected(session);
			EntityId entityId = ViewerSessionStateMapper.EnsureSelectedEntity(session);

			await connection.SendMessageAsync(entityId, command);

			session.DisplayedMessage = null;
			session.SendResultMessage = ViewerSessionStateMapper.BuildSendResultMessage(command.MessageProperties.ContentType, command.MessageProperties.MessageId);
			session.CurrentMessages = await connection.PeekMessagesAsync(entityId);
			if (!ViewerSessionStateMapper.SelectedEntityRequiresSession(session, connection))
				session.ReceiveSessionId = null;

			return ViewerSessionStateMapper.ToViewerState(session, connection);
		}
		finally {
			session.Gate.Release();
		}
	}
}
