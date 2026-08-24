namespace ServiceBusViewer.Business.Viewer.Services;

using Microsoft.Extensions.Logging;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Coordinates message operations for the selected entity in a single browser session.</summary>
internal sealed class ViewerMessageService(ILogger<ViewerMessageService> logger) : IViewerMessageService
{
	private readonly ILogger<ViewerMessageService> _logger = logger;

	/// <inheritdoc/>
	public async Task<ViewerState> RefreshAsync(IViewerSessionState session, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			CancellationToken operationCancellationToken = operationCancellationSource.Token;
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId? entityId = session.SelectedEntityId;

			session.CurrentMessages = entityId is null
				? ReceivedMessageList.Empty
				: await connection.PeekMessagesAsync(entityId, 50, operationCancellationToken);

			bool requiresSession = entityId is not null && connection.GetEntityProperties(entityId).RequiresSession;

			if (!requiresSession)
				session.ReceiveSessionId = null;

			session.SendResultMessage = null;

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<ViewerState> ReceiveAsync(IViewerSessionState session, string? sessionId, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			CancellationToken operationCancellationToken = operationCancellationSource.Token;
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();

			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");

			bool requiresSession = connection.GetEntityProperties(entityId).RequiresSession;

			session.ReceiveSessionId = requiresSession ? sessionId : null;
			session.DisplayedMessage = await connection.ReceiveMessageAsync(entityId, requiresSession ? sessionId : null, operationCancellationToken);
			session.SendResultMessage = null;

			try {
				session.CurrentMessages = await connection.PeekMessagesAsync(entityId, 50, operationCancellationToken);
			}
			catch (OperationCanceledException) when (operationCancellationToken.IsCancellationRequested) {
				throw;
			}
			catch (Exception exception) {
				_logger.LogWarning(
					"Message receive succeeded, but refreshing the peeked message list failed. Error type: {ErrorType}.",
					exception.GetType().FullName);
			}

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task SendAsync(IViewerSessionState session, SendCommand command, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			CancellationToken operationCancellationToken = operationCancellationSource.Token;
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");
			EntityProperties entity = connection.GetEntityProperties(entityId);

			if (entity.RequiresSession && string.IsNullOrWhiteSpace(command.MessageProperties.SessionId))
				throw new SendMessageValidationException($"A Session ID is required when sending to '{entity.Name}' because the selected entity requires sessions.");

			await connection.SendMessageAsync(entityId, command, operationCancellationToken);

			if (!entity.RequiresSession)
				session.ReceiveSessionId = null;
		}
		finally {
			session.Gate.Release();
		}
	}
}
