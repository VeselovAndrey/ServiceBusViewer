namespace ServiceBusViewer.Infrastructure.ClientSession;

/// <summary>Periodically evicts and disposes expired client session state buckets.</summary>
internal sealed class ClientSessionStateCleanupService(
	ClientSessionStateRegistry registry,
	ILogger<ClientSessionStateCleanupService> logger) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using PeriodicTimer timer = new(TimeSpan.FromMinutes(15));

		while (await timer.WaitForNextTickAsync(stoppingToken)) {
			IReadOnlyList<ClientSessionState> expiredSessions = registry.RemoveExpiredSessions();

			foreach (ClientSessionState expiredSession in expiredSessions) {
				try {
					await expiredSession.DisposeAsync();
				}
				catch (Exception ex) {
					logger.LogWarning(ex, "Failed to dispose expired client session state.");
				}
			}
		}
	}
}