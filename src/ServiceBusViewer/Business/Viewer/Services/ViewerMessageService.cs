namespace ServiceBusViewer.Business.Viewer.Services;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates message operations for the selected entity in a single browser session.</summary>
internal sealed class ViewerMessageService : IViewerMessageService
{
	/// <inheritdoc/>
	public async Task<ViewerState> RefreshAsync(IViewerSessionState session)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId? entityId = session.SelectedEntityId;

			session.DisplayedMessage = null;
			session.SendResultMessage = null;
			session.CurrentMessages = entityId is null
				? ReceivedMessageList.Empty
				: await connection.PeekMessagesAsync(entityId);

			bool requiresSession = entityId is not null
				&& connection.GetEntityProperties(entityId) is QueueEntityProperties { RequiresSession: true }
					or SubscriptionEntityProperties { RequiresSession: true };

			if (!requiresSession)
				session.ReceiveSessionId = null;

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");
			bool requiresSession = connection.GetEntityProperties(entityId) is QueueEntityProperties { RequiresSession: true }
				or SubscriptionEntityProperties { RequiresSession: true };

			session.ReceiveSessionId = requiresSession ? sessionId : null;
			session.DisplayedMessage = await connection.ReceiveMessageAsync(entityId, requiresSession ? sessionId : null);
			session.SendResultMessage = null;
			session.CurrentMessages = await connection.PeekMessagesAsync(entityId);

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<ViewerState> SendAsync(IViewerSessionState session, SendCommand command)
	{
		await session.Gate.WaitAsync();

		try {
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");

			await connection.SendMessageAsync(entityId, command);

			session.DisplayedMessage = null;
			session.SendResultMessage = BuildSendResultMessage(command.MessageProperties.ContentType, command.MessageProperties.MessageId);
			session.CurrentMessages = await connection.PeekMessagesAsync(entityId);
			bool requiresSession = connection.GetEntityProperties(entityId) is QueueEntityProperties { RequiresSession: true }
				or SubscriptionEntityProperties { RequiresSession: true };

			if (!requiresSession)
				session.ReceiveSessionId = null;

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	private static string BuildSendResultMessage(string? sentContentType, string? sentMessageId)
	{
		string contentTypeDescription = string.IsNullOrWhiteSpace(sentContentType)
			? "without a content type"
			: $"with content type '{sentContentType}'";

		return string.IsNullOrWhiteSpace(sentMessageId)
			? $"Message sent successfully {contentTypeDescription}."
			: $"Message '{sentMessageId}' sent successfully {contentTypeDescription}.";
	}
}
