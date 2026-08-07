namespace ServiceBusViewer.Infrastructure.ClientSession;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Holds per-browser-session backend state for the viewer API.</summary>
internal sealed class ClientSessionState : IViewerSessionState
{
	private long _lastAccessUnixTimeSeconds;

	public ClientSessionState()
	{
		ConnectionSettings = CreateDefaultConnectionSettings();
		Touch();
	}

	public SemaphoreSlim Gate { get; } = new(1, 1);

	public ConnectionSettings ConnectionSettings { get; set; }

	public IServiceBusConnection? Connection { get; set; }

	public EntityId? SelectedEntityId { get; set; }

	public ReceivedMessageList CurrentMessages { get; set; } = ReceivedMessageList.Empty;

	public ReceivedMessage? DisplayedMessage { get; set; }

	public string? ReceiveSessionId { get; set; }

	public string? SendResultMessage { get; set; }

	public bool IsConnected => Connection is not null;

	public DateTimeOffset LastAccessUtc => DateTimeOffset.FromUnixTimeSeconds(Interlocked.Read(ref _lastAccessUnixTimeSeconds));

	public void Touch()
		=> Interlocked.Exchange(ref _lastAccessUnixTimeSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

	public async Task ResetConnectionAsync()
	{
		if (Connection is not null) {
			await Connection.DisposeAsync();
			Connection = null;
		}

		SelectedEntityId = null;
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

	private static ConnectionSettings CreateDefaultConnectionSettings()
	{
		string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
			?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

		string? rootConnectionString = Environment.GetEnvironmentVariable("ROOT_CONNECTION_STRING");

		string queueOrTopicName = Environment.GetEnvironmentVariable("QUEUE_OR_TOPIC_NAME") ?? "default";

		string? subscriptionName = Environment.GetEnvironmentVariable("SUBSCRIPTION_NAME");

		return !string.IsNullOrWhiteSpace(rootConnectionString)
			? new ConnectionSettings(connectionString, rootConnectionString, queueOrTopicName, subscriptionName)
			: new ConnectionSettings(connectionString, queueOrTopicName, subscriptionName);
	}
}
