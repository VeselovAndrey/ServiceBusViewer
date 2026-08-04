namespace ServiceBusViewer.Infrastructure.BrowserSession;

using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Holds per-browser-session backend state for the viewer API.</summary>
internal sealed class BrowserSessionState : IAsyncDisposable
{
	private long _lastAccessUnixTimeSeconds;

	public BrowserSessionState()
	{
		Connection = StoredConnectionSettings.CreateDefault();
		Touch();
	}

	public SemaphoreSlim Gate { get; } = new(1, 1);

	public StoredConnectionSettings Connection { get; set; }

	public IServiceBusSessionConnectionService? ServiceBusConnection { get; set; }

	public ReceivedMessageList CurrentMessages { get; set; } = ReceivedMessageList.Empty;

	public ReceivedMessage? DisplayedMessage { get; set; }

	public string? ReceiveSessionId { get; set; }

	public string? SendResultMessage { get; set; }

	public bool IsConnected => ServiceBusConnection?.Connected == true;

	public DateTimeOffset LastAccessUtc => DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref _lastAccessUnixTimeSeconds));

	public void Touch()
		=> Interlocked.Exchange(ref _lastAccessUnixTimeSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

	public async Task ResetConnectionAsync()
	{
		if (ServiceBusConnection is not null) {
			await ServiceBusConnection.DisposeAsync();
			ServiceBusConnection = null;
		}

		CurrentMessages = ReceivedMessageList.Empty;
		DisplayedMessage = null;
		ReceiveSessionId = null;
		SendResultMessage = null;
	}

	public async ValueTask DisposeAsync()
	{
		await ResetConnectionAsync();
		Gate.Dispose();
	}
}

internal sealed record StoredConnectionSettings(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName)
{
	public static StoredConnectionSettings CreateDefault()
	{
		return new StoredConnectionSettings(
			Environment.GetEnvironmentVariable("CONNECTION_STRING")
				?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			Environment.GetEnvironmentVariable("ROOT_CONNECTION_STRING"),
			null,
			null);
	}
}
