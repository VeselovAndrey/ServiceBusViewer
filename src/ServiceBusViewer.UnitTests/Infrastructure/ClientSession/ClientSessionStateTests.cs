namespace ServiceBusViewer.UnitTests.Infrastructure.ClientSession;

using NSubstitute;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Infrastructure.ClientSession;
using Xunit;

public sealed class ClientSessionStateTests
{
	[Fact]
	public void ClientSessionState_CancelActiveConnectionOperations_ActiveConnection_CancelsConnectionToken()
	{
		// Arrange
		var state = new ClientSessionState(new ConnectionSettings("Endpoint=sb://test/", null, null, null));
		CancellationToken cancellationToken = state.ConnectionCancellationToken;

		// Act
		state.CancelActiveConnectionOperations();

		// Assert
		Assert.True(cancellationToken.IsCancellationRequested);
	}

	[Fact]
	public void ClientSessionState_ChangesConnectionSettings_KeepsApplicationConfiguredConnectionSettings()
	{
		// Arrange
		var applicationSettings = new ConnectionSettings("Endpoint=sb://app/", "Endpoint=sb://emulator-admin/", "app-queue", "app-subscription");
		var state = new ClientSessionState(applicationSettings);
		var userSettings = new ConnectionSettings("Endpoint=sb://user/", null, "user-queue", null);

		// Act
		state.ConnectionSettings = userSettings;

		// Assert
		Assert.Equal(userSettings, state.ConnectionSettings);
		Assert.Equal(applicationSettings, state.ApplicationConfiguredConnectionSettings);
	}

	[Fact]
	public async Task ClientSessionState_ResetConnectionAsync_ConnectedState_ClearsStateAndDisposesConnection()
	{
		// Arrange
		var state = new ClientSessionState(new ConnectionSettings("Endpoint=sb://test/", null, null, null));
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		state.Connection = connection;
		state.SelectedEntityId = new QueueEntityId("orders");
		state.CurrentMessages = new ReceivedMessageList([], true);
		state.DisplayedMessage = new ReceivedMessage("body", new ReceivedMessageProperties("id", 1, null, null, null, null, 1, DateTimeOffset.UtcNow, null, null, null, null, null, null, null, null), new Dictionary<string, object>());
		state.ReceiveSessionId = "session";
		state.SendResultMessage = "Sent";
		CancellationToken previousCancellationToken = state.ConnectionCancellationToken;

		// Act
		await state.ResetConnectionAsync();

		// Assert
		Assert.Null(state.Connection);
		Assert.Null(state.SelectedEntityId);
		Assert.Equal(ReceivedMessageList.Empty, state.CurrentMessages);
		Assert.Null(state.DisplayedMessage);
		Assert.Null(state.ReceiveSessionId);
		Assert.Null(state.SendResultMessage);
		Assert.False(previousCancellationToken.IsCancellationRequested);
		Assert.NotEqual(previousCancellationToken, state.ConnectionCancellationToken);
		await connection.Received(1).DisposeAsync();
	}

	[Fact]
	public async Task ClientSessionState_DisposeAsync_ConnectedState_CancelsOperationsAndDisposesConnection()
	{
		// Arrange
		var state = new ClientSessionState(new ConnectionSettings("Endpoint=sb://test/", null, null, null));
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		state.Connection = connection;
		CancellationToken cancellationToken = state.ConnectionCancellationToken;

		// Act
		await state.DisposeAsync();

		// Assert
		Assert.True(cancellationToken.IsCancellationRequested);
		Assert.Null(state.Connection);
		await connection.Received(1).DisposeAsync();
	}
}
