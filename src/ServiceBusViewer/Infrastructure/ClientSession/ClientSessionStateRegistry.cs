namespace ServiceBusViewer.Infrastructure.ClientSession;

using System.Collections.Concurrent;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;

/// <summary>Stores in-memory browser-session state buckets keyed by an opaque cookie value.</summary>
internal sealed class ClientSessionStateRegistry(IClientSessionSettings settings)
{
	/// <summary>The duration a browser session state bucket can remain idle before it is considered expired.</summary>
	private static readonly TimeSpan _idleTimeoutValue = TimeSpan.FromHours(1);

	private readonly ConcurrentDictionary<string, ClientSessionState> _sessions = new(StringComparer.Ordinal);
	private readonly IClientSessionSettings _settings = settings;

	/// <summary>Gets an existing session state bucket or creates a new one if it does not exist.</summary>
	/// <param name="sessionId">The value identifying the browser session.</param>
	/// <returns>The session state bucket.</returns>
	public ClientSessionState GetOrCreate(string sessionId)
	{
		ClientSessionState state = _sessions.GetOrAdd(sessionId, static (_, settings) => new ClientSessionState(new ConnectionSettings(
			settings.ConnectionString,
			settings.EmulatorManagementConnectionString,
			settings.QueueOrTopicName,
			settings.SubscriptionName)), _settings);

		state.Touch();
		return state;
	}

	/// <summary>Removes expired session state buckets.</summary>
	/// <returns>The list of removed session state buckets.</returns>
	public IReadOnlyList<ClientSessionState> RemoveExpiredSessions()
	{
		DateTimeOffset cutoff = DateTimeOffset.UtcNow - _idleTimeoutValue;
		List<ClientSessionState> removedStates = [];

		foreach (KeyValuePair<string, ClientSessionState> session in _sessions) {
			if (session.Value.LastAccessUtc > cutoff)
				continue;

			if (_sessions.TryRemove(session.Key, out ClientSessionState? removedState))
				removedStates.Add(removedState);
		}

		return removedStates;
	}
}
