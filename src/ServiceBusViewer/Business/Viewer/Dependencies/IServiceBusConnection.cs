namespace ServiceBusViewer.Business.Viewer.Dependencies;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>
/// Represents an open Service Bus connection scoped to a single browser session. The connection exposes a management-capable view of available entities and operations for peeking,
/// receiving and sending messages against an explicitly selected entity.
/// </summary>
public interface IServiceBusConnection : IAsyncDisposable
{
	/// <summary>
	/// The host name of the Service Bus namespace (e.g. "{namespace}.servicebus.windows.net").
	/// </summary>
	string NamespaceHost { get; }

	/// <summary>Indicates whether the Service Bus management API is available for this connection (used for entity discovery, management operations, etc.).</summary>
	bool IsManagementApiAvailable { get; }

	/// <summary>
	/// A read-only list of discovered entities in the connected namespace. Empty when discovery is unavailable or none found.
	/// </summary>
	IReadOnlyList<EntityProperties> AvailableEntities { get; }

	/// <summary>Refreshes the catalog of available entities from the management API.</summary>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes when the refresh finishes.</returns>
	Task RefreshEntitiesAsync(CancellationToken cancellationToken);

	/// <summary>Retrieves the properties for a specific entity previously discovered.</summary>
	/// <param name="entityId">The identity of the entity whose properties are requested.</param>
	/// <returns>The <see cref="EntityProperties"/> for the requested entity.</returns>
	EntityProperties GetEntityProperties(EntityId entityId);

	/// <summary>Peeks up to <paramref name="maxMessages"/> messages from the requested entity without removing them from the queue/topic subscription.</summary>
	/// <param name="entityId">Identifier of the entity to peek.</param>
	/// <param name="maxMessages">Maximum number of messages to peek. Defaults to 50.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with the list of peeked messages.</returns>
	Task<ReceivedMessageList> PeekMessagesAsync(EntityId entityId, int maxMessages, CancellationToken cancellationToken);

	/// <summary>Peeks up to <paramref name="maxMessages"/> messages from the requested entity's $DeadLetterQueue without removing them.</summary>
	/// <param name="entityId">Identifier of the entity whose dead-letter queue is peeked (queue, or subscription with its parent topic).</param>
	/// <param name="maxMessages">Maximum number of messages to peek. Defaults to 50.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with the list of dead-lettered messages.</returns>
	Task<ReceivedMessageList> PeekDeadLetterMessagesAsync(EntityId entityId, int maxMessages, CancellationToken cancellationToken);

	/// <summary>
	/// Republishes dead-lettered messages from the requested entity's $DeadLetterQueue to its original entity - the entity's own queue, or the parent topic when the
	/// entity is a subscription. Each message is removed from the dead-letter queue only after its content-unchanged copy (message id adjusted per the strategy) has
	/// been delivered to the original entity; messages that fail remain in the dead-letter queue.
	/// </summary>
	/// <param name="entityId">Identifier of the entity whose dead-letter queue is republished from.</param>
	/// <param name="messageIds">Message ids of the dead-lettered messages to republish, or <c>null</c> to republish every message currently in the dead-letter queue.</param>
	/// <param name="strategy">Message id strategy applied to each republished copy.</param>
	/// <param name="manualMessageId">The message id assigned to the republished copy when the strategy is Manual; required for a single-message manual republish and not
	/// supported for bulk republish.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with the outcome summary reporting how many messages were moved and how many failed and remain in the dead-letter queue.</returns>
	Task<DeadLetterOperationResult> RepublishDeadLetterMessagesAsync(EntityId entityId, IReadOnlyCollection<string>? messageIds, RepublishMessageIdStrategy strategy, string? manualMessageId, CancellationToken cancellationToken);

	/// <summary>
	/// Permanently deletes dead-lettered messages from the requested entity's $DeadLetterQueue without re-delivering them.
	/// </summary>
	/// <param name="entityId">Identifier of the entity whose dead-letter queue is dropped from.</param>
	/// <param name="messageIds">Message ids of the dead-lettered messages to drop, or <c>null</c> to drop every message currently in the dead-letter queue.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with the outcome summary reporting how many messages were dropped and how many failed and remain in the dead-letter queue.</returns>
	Task<DeadLetterOperationResult> DropDeadLetterMessagesAsync(EntityId entityId, IReadOnlyCollection<string>? messageIds, CancellationToken cancellationToken);

	/// <summary>Receives the next available message from the requested entity.</summary>
	/// <param name="entityId">Identifier of the entity to receive from.</param>
	/// <param name="sessionId">Session id to receive from, or <c>null</c> for non-session receive or next available session.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes with the received message or <c>null</c> if none available.</returns>
	Task<ReceivedMessage?> ReceiveMessageAsync(EntityId entityId, string? sessionId, CancellationToken cancellationToken);

	/// <summary>Sends a message to the requested entity.</summary>
	/// <param name="entityId">Identifier of the entity to send to.</param>
	/// <param name="command">Command containing the message body and associated properties.</param>
	/// <param name="cancellationToken">Optional <seealso cref="CancellationToken"/> to propagate notifications that the operation should be cancelled.</param>
	/// <returns>A task that completes when the send operation completes.</returns>
	Task SendMessageAsync(EntityId entityId, SendCommand command, CancellationToken cancellationToken);
}
