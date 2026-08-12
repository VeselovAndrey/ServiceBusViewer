namespace ServiceBusViewer.Infrastructure.ClientSession;

using System.Collections.Concurrent;

/// <summary>Stores in-memory browser-session state buckets keyed by an opaque cookie value.</summary>
internal sealed class ClientSessionStateRegistry
{
	/// <summary>The duration a browser session state bucket can remain idle before it is considered expired.</summary>
	private static readonly TimeSpan _idleTimeoutValue = TimeSpan.FromHours(1);

	private readonly ConcurrentDictionary<string, ClientSessionState> _sessions = new(StringComparer.Ordinal);

	public ClientSessionState GetOrCreate(string sessionId)
	{
		ClientSessionState state = _sessions.GetOrAdd(sessionId, static _ => new ClientSessionState());
		state.Touch();
		return state;
	}

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