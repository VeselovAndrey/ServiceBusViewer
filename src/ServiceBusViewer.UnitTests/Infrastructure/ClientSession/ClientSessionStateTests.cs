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
	public void CancelActiveConnectionOperations_ActiveConnection_CancelsConnectionToken()
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
	public async Task ResetConnectionAsync_ConnectedState_ClearsStateAndDisposesConnection()
	{
		// Arrange
		var state = new ClientSessionState(new ConnectionSettings("Endpoint=sb://test/", null, null, null));
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		state.Connection = connection;
		state.SelectedEntityId = new QueueEntityId("orders");
		state.CurrentMessages = new ReceivedMessageList([], true);
		state.DisplayedMessage = new ReceivedMessage("body", new ReceivedMessageProperties("id", null, null, null, null, 1, DateTimeOffset.UtcNow, null, null, null, null, null), new Dictionary<string, object>());
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
	public async Task DisposeAsync_ConnectedState_CancelsOperationsAndDisposesConnection()
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
