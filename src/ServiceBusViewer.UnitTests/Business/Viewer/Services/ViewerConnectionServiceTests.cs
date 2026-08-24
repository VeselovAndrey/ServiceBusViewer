namespace ServiceBusViewer.UnitTests.Business.Viewer.Services;

using NSubstitute;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Business.Viewer.Services;
using ServiceBusViewer.UnitTests.Assets;
using ServiceBusViewer.UnitTests.TestSupport;
using Xunit;

public sealed class ViewerConnectionServiceTests
{
	[Fact]
	public async Task ConnectAsync_ConfiguredQueueExists_SelectsQueueAndPeeksMessages()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName, requiresSession: false);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns([queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.PeekMessagesAsync(Arg.Any<EntityId>(), 50, Arg.Any<CancellationToken>()).Returns(ReceivedMessageList.Empty);

		IServiceBusConnectionFactory factory = Substitute.For<IServiceBusConnectionFactory>();

		var service = new ViewerConnectionService(factory);

		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;

		var settings = new ConnectionSettings("Endpoint=sb://test/", null, queueName, null);

		factory.OpenAsync(settings, Arg.Any<CancellationToken>()).Returns(connection);

		// Act
		ViewerState state = await service.ConnectAsync(session, settings, CancellationToken.None);

		// Assert
		Assert.Same(connection, session.Connection);
		Assert.Equal(new QueueEntityId(queueName), session.SelectedEntityId);
		await factory.Received(1).OpenAsync(settings, Arg.Any<CancellationToken>());
		await connection.Received(1).PeekMessagesAsync(new QueueEntityId(queueName), 50, Arg.Any<CancellationToken>());
		Assert.Equal(queueName, state.EntityName);
	}

	[Fact]
	public async Task GetCurrentStateAsync_DisconnectedSession_ThrowsViewerNotConnectedException()
	{
		// Arrange
		var service = new ViewerConnectionService(null!);
		using var sessionScope = ViewerSessionScope.CreateDisconnected();
		IViewerSessionState session = sessionScope.State;

		// Act
		Task action() => service.GetCurrentStateAsync(session, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ViewerNotConnectedException>(action);
	}

	[Fact]
	public async Task ConnectAsync_SessionIsAlreadyConnected_ThrowsViewerAlreadyConnectedException()
	{
		// Arrange
		IServiceBusConnectionFactory factory = Substitute.For<IServiceBusConnectionFactory>();
		var service = new ViewerConnectionService(factory);

		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.IsConnected.Returns(true);

		var settings = new ConnectionSettings("Endpoint=sb://test/", null, null, null);

		// Act
		Task action() => service.ConnectAsync(session, settings, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ViewerAlreadyConnectedException>(action);
		await factory.DidNotReceive().OpenAsync(Arg.Any<ConnectionSettings>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ConnectAsync_ConfiguredQueueIsMissing_DisposesConnectionAndThrowsArgumentException()
	{
		// Arrange
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns([]);

		IServiceBusConnectionFactory factory = Substitute.For<IServiceBusConnectionFactory>();
		factory.OpenAsync(Arg.Any<ConnectionSettings>(), Arg.Any<CancellationToken>()).Returns(connection);

		var service = new ViewerConnectionService(factory);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		var settings = new ConnectionSettings("Endpoint=sb://test/", null, "missing-queue", null);

		// Act
		Task action() => service.ConnectAsync(session, settings, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentException>(action);
		await connection.Received(1).DisposeAsync();
	}
}
