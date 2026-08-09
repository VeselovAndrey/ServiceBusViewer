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
	/// <returns>A task that completes when the refresh finishes.</returns>
	Task RefreshEntitiesAsync();

	/// <summary>Retrieves the properties for a specific entity previously discovered.</summary>
	/// <param name="entityId">The identity of the entity whose properties are requested.</param>
	/// <returns>The <see cref="EntityProperties"/> for the requested entity.</returns>
	EntityProperties GetEntityProperties(EntityId entityId);

	/// <summary>Peeks up to <paramref name="maxMessages"/> messages from the requested entity without removing them from the queue/topic subscription.</summary>
	/// <param name="entityId">Identifier of the entity to peek.</param>
	/// <param name="maxMessages">Maximum number of messages to peek. Defaults to 50.</param>
	/// <returns>A task that completes with the list of peeked messages.</returns>
	Task<ReceivedMessageList> PeekMessagesAsync(EntityId entityId, int maxMessages = 50);

	/// <summary>Receives the next available message from the requested entity.</summary>
	/// <param name="entityId">Identifier of the entity to receive from.</param>
	/// <param name="sessionId">Session id to receive from, or <c>null</c> for non-session receive or next available session.</param>
	/// <returns>A task that completes with the received message or <c>null</c> if none available.</returns>
	Task<ReceivedMessage?> ReceiveMessageAsync(EntityId entityId, string? sessionId = null);

	/// <summary>Sends a message to the requested entity.</summary>
	/// <param name="entityId">Identifier of the entity to send to.</param>
	/// <param name="command">Command containing the message body and associated properties.</param>
	/// <returns>A task that completes when the send operation completes.</returns>
	Task SendMessageAsync(EntityId entityId, SendCommand command);
}
