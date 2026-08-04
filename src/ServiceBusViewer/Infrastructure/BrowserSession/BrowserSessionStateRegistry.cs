namespace ServiceBusViewer.Infrastructure.BrowserSession;

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;

/// <summary>Stores in-memory browser-session state buckets keyed by an opaque cookie value.</summary>
internal sealed class BrowserSessionStateRegistry
{
	/// <summary>The duration a browser session state bucket can remain idle before it is considered expired.</summary>
	private static readonly TimeSpan _idleTimeoutValue = TimeSpan.FromHours(1);

	private readonly ConcurrentDictionary<string, BrowserSessionState> _sessions = new(StringComparer.Ordinal);

	public TimeSpan IdleTimeout => _idleTimeoutValue;

	public BrowserSessionState GetOrCreate(string sessionId)
	{
		BrowserSessionState state = _sessions.GetOrAdd(sessionId, static _ => new BrowserSessionState());
		state.Touch();
		return state;
	}

	public IReadOnlyList<BrowserSessionState> RemoveExpiredSessions()
	{
		DateTimeOffset cutoff = DateTimeOffset.UtcNow - _idleTimeoutValue;
		List<BrowserSessionState> removedStates = [];

		foreach (KeyValuePair<string, BrowserSessionState> session in _sessions) {
			if (session.Value.LastAccessUtc > cutoff)
				continue;

			if (_sessions.TryRemove(session.Key, out BrowserSessionState? removedState))
				removedStates.Add(removedState);
		}

		return removedStates;
	}
}

/// <summary>Periodically evicts and disposes expired browser session state buckets.</summary>
internal sealed class BrowserSessionStateCleanupService(
	BrowserSessionStateRegistry registry,
	ILogger<BrowserSessionStateCleanupService> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using PeriodicTimer timer = new(TimeSpan.FromMinutes(15));

		while (await timer.WaitForNextTickAsync(stoppingToken)) {
			IReadOnlyList<BrowserSessionState> expiredSessions = registry.RemoveExpiredSessions();

			foreach (BrowserSessionState expiredSession in expiredSessions) {
				try {
					await expiredSession.DisposeAsync();
				}
				catch (Exception ex) {
					logger.LogWarning(ex, "Failed to dispose expired browser session state.");
				}
			}
		}
	}
}
