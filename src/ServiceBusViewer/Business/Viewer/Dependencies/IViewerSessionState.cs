namespace ServiceBusViewer.Business.Viewer.Dependencies;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>
/// Represents the business-facing state for a single browser session. Implementations are owned by infrastructure but
/// the interface lives with Business contracts so business logic depends on the abstraction rather than a concrete
/// implementation.
/// </summary>
public interface IViewerSessionState : IAsyncDisposable
{
	/// <summary>
	/// A session-wide gate to serialize access to mutable session state. Use <see cref="SemaphoreSlim.WaitAsync()"/> and
	/// <see cref="SemaphoreSlim.Release()"/> or a helper wrapper.
	/// </summary>
	SemaphoreSlim Gate { get; }

	/// <summary>
	/// Current connection settings used to open the Service Bus connection for this session.
	/// </summary>
	ConnectionSettings ConnectionSettings { get; set; }

	/// <summary>
	/// The currently opened Service Bus connection context for this session, or <c>null</c> when disconnected.
	/// </summary>
	IServiceBusConnection? Connection { get; set; }

	/// <summary>
	/// The currently selected entity for viewer operations, or <c>null</c> when no entity is selected.
	/// </summary>
	EntityId? SelectedEntityId { get; set; }

	/// <summary>
	/// Cached list of messages currently presented to the viewer (peeked/received results).
	/// </summary>
	ReceivedMessageList CurrentMessages { get; set; }

	/// <summary>
	/// The message currently displayed in the viewer details pane, or <c>null</c>.
	/// </summary>
	ReceivedMessage? DisplayedMessage { get; set; }

	/// <summary>
	/// The session id used for session-enabled receives. When <c>null</c>, no specific session is pinned.
	/// </summary>
	string? ReceiveSessionId { get; set; }

	/// <summary>
	/// A short human-readable result from the last send operation that can be shown to the viewer.
	/// </summary>
	string? SendResultMessage { get; set; }

	/// <summary>
	/// Indicates whether the session currently has an active Service Bus connection.
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Resets and clears any connection-related state for the session (invoked on explicit disconnect or errors).
	/// Implementations should dispose the underlying <see cref="IServiceBusConnection"/> if present and clear message
	/// caches.
	/// </summary>
	/// <returns>A task that completes when the reset operation finishes.</returns>
	Task ResetConnectionAsync();
}