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
				: await connection.PeekMessagesAsync(entityId, session.PeekMessageCount, operationCancellationToken);

			bool requiresSession = entityId is not null && connection.GetEntityProperties(entityId).RequiresSession;

			if (!requiresSession)
				session.ReceiveSessionId = null;

			session.SendResultMessage = null;

			await RefreshAllEntityMessageCountsAsync(connection, operationCancellationToken);

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<ViewerState> RefreshDeadLetterMessagesAsync(IViewerSessionState session, CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			CancellationToken operationCancellationToken = operationCancellationSource.Token;
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId? entityId = session.SelectedEntityId;

			session.CurrentMessages = entityId is not null
				? await connection.PeekDeadLetterMessagesAsync(entityId, session.PeekMessageCount, operationCancellationToken)
				: ReceivedMessageList.Empty;

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
	public async Task<DeadLetterOperationResult> RepublishDeadLetterMessagesAsync(
		IViewerSessionState session,
		string? messageId,
		RepublishMessageIdStrategy strategy,
		string? manualMessageId,
		int? maxMessages,
		CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");
			EntityProperties target = ResolveRepublishTarget(connection, entityId);
			bool requiresDuplicateDetection = target switch {
				QueueEntityProperties queue => queue.RequiresDuplicateDetection,
				TopicEntityProperties topic => topic.RequiresDuplicateDetection,
				_ => false
			};

			if (strategy is RepublishMessageIdStrategy.Manual && string.IsNullOrWhiteSpace(messageId))
				throw new ArgumentException("Republishing with a custom message id is only supported for a single message.");

			if (strategy is RepublishMessageIdStrategy.Manual && string.IsNullOrWhiteSpace(manualMessageId))
				throw new ArgumentException("A custom message id is required when republishing with the custom id strategy.");

			if (strategy is RepublishMessageIdStrategy.KeepOriginal && requiresDuplicateDetection)
				throw new ArgumentException("A new message id is required when republishing to a target with duplicate detection enabled.");

			DeadLetterOperationResult result = await connection.RepublishDeadLetterMessagesAsync(
				entityId,
				messageId is null ? null : [messageId],
				strategy,
				manualMessageId,
				maxMessages,
				operationCancellationSource.Token);

			await connection.RefreshEntityMessageCountsAsync(entityId, operationCancellationSource.Token);

			return result;
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<DeadLetterOperationResult> DropDeadLetterMessagesAsync(
		IViewerSessionState session,
		string? messageId,
		int? maxMessages,
		CancellationToken cancellationToken)
	{
		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId entityId = session.SelectedEntityId ?? throw new InvalidOperationException("No entity selected.");

			DeadLetterOperationResult result = await connection.DropDeadLetterMessagesAsync(
				entityId,
				messageId is null ? null : [messageId],
				maxMessages,
				operationCancellationSource.Token);

			await connection.RefreshEntityMessageCountsAsync(entityId, operationCancellationSource.Token);

			return result;
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <summary>
	/// Resolves the original entity that a dead-lettered message is republished to: the entity's own queue or the parent topic when the entity is a
	/// subscription. Topic-level dead-letter messages have no original entity and are not supported.
	/// </summary>
	/// <param name="connection">The active Service Bus connection used to read cached entity properties.</param>
	/// <param name="entityId">Identifier of the entity whose dead-letter queue is republished from.</param>
	/// <returns>The cached properties of the republish target.</returns>
	private static EntityProperties ResolveRepublishTarget(IServiceBusConnection connection, EntityId entityId)
		=> entityId switch {
			SubscriptionEntityId subscription => connection.GetEntityProperties(new TopicEntityId(subscription.TopicName)),
			QueueEntityId => connection.GetEntityProperties(entityId),
			_ => throw new InvalidOperationException("Republishing dead-letter messages is not supported for topics. Please select a queue or a subscription.")
		};

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
				session.CurrentMessages = await connection.PeekMessagesAsync(entityId, session.PeekMessageCount, operationCancellationToken);
			}
			catch (OperationCanceledException) when (operationCancellationToken.IsCancellationRequested) {
				throw;
			}
			catch (Exception exception) {
				_logger.LogWarning(
					"Message receive succeeded, but refreshing the peeked message list failed. Error type: {ErrorType}.",
					exception.GetType().FullName);
			}

			await connection.RefreshEntityMessageCountsAsync(entityId, operationCancellationToken);

			return session.ToViewerState(connection);
		}
		finally {
			session.Gate.Release();
		}
	}

	/// <inheritdoc/>
	public async Task<ViewerState> UpdatePeekMessageCountAsync(IViewerSessionState session, int peekMessageCount, CancellationToken cancellationToken)
	{
		if (peekMessageCount is < PeekMessageCountBounds.Min or > PeekMessageCountBounds.Max)
			throw new ArgumentOutOfRangeException(nameof(peekMessageCount), peekMessageCount, $"The peek message count must be a whole number from {PeekMessageCountBounds.Min} through {PeekMessageCountBounds.Max}.");

		await session.Gate.WaitAsync(cancellationToken);

		try {
			using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.ConnectionCancellationToken);
			CancellationToken operationCancellationToken = operationCancellationSource.Token;
			IServiceBusConnection connection = session.Connection ?? throw new ViewerNotConnectedException();
			EntityId? entityId = session.SelectedEntityId;

			session.PeekMessageCount = peekMessageCount;
			session.CurrentMessages = entityId is null
				? ReceivedMessageList.Empty
				: await connection.PeekMessagesAsync(entityId, peekMessageCount, operationCancellationToken);
			session.SendResultMessage = null;

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

			await connection.RefreshEntityMessageCountsAsync(entityId, operationCancellationToken);
		}
		finally {
			session.Gate.Release();
		}
	}

	// Refreshes the runtime counts of every discovered entity; subscriptions are covered through their parent topic. The entity list is
	// snapshotted because the connection replaces refreshed entries while iterating.
	private static async Task RefreshAllEntityMessageCountsAsync(IServiceBusConnection connection, CancellationToken cancellationToken)
	{
		foreach (EntityProperties entity in connection.AvailableEntities.ToList()) {
			EntityId? refreshId = entity switch {
				QueueEntityProperties queue => new QueueEntityId(queue.Name),
				TopicEntityProperties topic => new TopicEntityId(topic.Name),
				_ => null
			};

			if (refreshId is not null)
				await connection.RefreshEntityMessageCountsAsync(refreshId, cancellationToken);
		}
	}
}
