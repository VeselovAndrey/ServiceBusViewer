namespace ServiceBusViewer.Infrastructure.ClientSession;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Holds per-browser-session backend state for the viewer API.</summary>
internal sealed class ClientSessionState : IViewerSessionState
{
	private long _lastAccessUnixTimeSeconds;
	private CancellationTokenSource _connectionCancellationSource = new();

	public ClientSessionState()
	{
		ConnectionSettings = CreateDefaultConnectionSettings();
		Touch();
	}

	public SemaphoreSlim Gate { get; } = new(1, 1);

	public CancellationToken ConnectionCancellationToken => _connectionCancellationSource.Token;

	public void CancelActiveConnectionOperations()
		=> _connectionCancellationSource.Cancel();

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
		IServiceBusConnection? connection = Connection;
		Connection = null;

		if (connection is not null)
			await connection.DisposeAsync();

		SelectedEntityId = null;
		CurrentMessages = ReceivedMessageList.Empty;
		DisplayedMessage = null;
		ReceiveSessionId = null;
		SendResultMessage = null;
		_connectionCancellationSource.Dispose();
		_connectionCancellationSource = new CancellationTokenSource();
	}

	public async ValueTask DisposeAsync()
	{
		CancelActiveConnectionOperations();
		await Gate.WaitAsync();

		try {
			await ResetConnectionAsync();
		}
		finally {
			Gate.Release();
			Gate.Dispose();
			_connectionCancellationSource.Dispose();
		}
	}

	private static ConnectionSettings CreateDefaultConnectionSettings()
	{
		string connectionString = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_CONNECTION_STRING")
			?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

		string? emulatorManagementConnectionString = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING");

		string queueOrTopicName = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME") ?? string.Empty;

		string? subscriptionName = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SUBSCRIPTION_NAME");

		return new ConnectionSettings(
			connectionString,
			emulatorManagementConnectionString,
			queueOrTopicName,
			subscriptionName);
	}
}
