namespace ServiceBusViewer.UnitTests.Business.Viewer.Services;

using NSubstitute;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Business.Viewer.Services;
using ServiceBusViewer.UnitTests.Assets;
using ServiceBusViewer.UnitTests.TestSupport;
using Xunit;

public sealed class ViewerEntityServiceTests
{
	[Fact]
	public async Task SelectEntityAsync_ConnectedSession_UpdatesSelectionAndClearsTransientState()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);
		var messages = new ReceivedMessageList([], true);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.PeekMessagesAsync(new QueueEntityId(queueName), 50, Arg.Any<CancellationToken>()).Returns(messages);

		var service = new ViewerEntityService();

		using var sessionScope = new ViewerSessionScope();

		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId("previous");
		session.DisplayedMessage = CreateMessage();
		session.ReceiveSessionId = "old-session";
		session.SendResultMessage = "Sent";

		var entityId = new QueueEntityId(queueName);

		// Act
		ViewerState state = await service.SelectEntityAsync(session, entityId, CancellationToken.None);

		// Assert
		Assert.Equal(entityId, session.SelectedEntityId);
		Assert.Equal(messages, session.CurrentMessages);
		Assert.Null(session.DisplayedMessage);
		Assert.Null(session.ReceiveSessionId);
		Assert.Null(session.SendResultMessage);
		Assert.Equal(queueName, state.EntityName);
	}

	[Fact]
	public async Task GetDetailsAsync_DisconnectedSession_ThrowsViewerNotConnectedException()
	{
		// Arrange
		const string queueName = "default-queue";
		var service = new ViewerEntityService();
		using var sessionScope = ViewerSessionScope.CreateDisconnected();

		IViewerSessionState session = sessionScope.State;

		// Act
		Task action() => service.GetDetailsAsync(session, new QueueEntityId(queueName), CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ViewerNotConnectedException>(action);
	}

	[Fact]
	public async Task SelectEntityAsync_DisconnectedSession_ThrowsViewerNotConnectedException()
	{
		// Arrange
		var service = new ViewerEntityService();
		using var sessionScope = ViewerSessionScope.CreateDisconnected();
		IViewerSessionState session = sessionScope.State;

		// Act
		Task action() => service.SelectEntityAsync(session, new QueueEntityId("orders"), CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ViewerNotConnectedException>(action);
	}

	[Fact]
	public async Task GetDetailsAsync_SubscriptionIsSelected_ReturnsSubscriptionDetails()
	{
		// Arrange
		var entityId = new SubscriptionEntityId("processed", "orders");
		var subscription = new SubscriptionEntityProperties(
			"processed",
			"orders",
			false,
			TimeSpan.FromMinutes(1),
			10,
			TimeSpan.MaxValue,
			false,
			true,
			TimeSpan.MaxValue,
			[]);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.NamespaceHost.Returns("test.servicebus.windows.net");
		connection.IsManagementApiAvailable.Returns(true);
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[subscription]);
		connection.GetEntityProperties(entityId).Returns(subscription);

		var service = new ViewerEntityService();
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;

		// Act
		EntityDetailsResult details = await service.GetDetailsAsync(session, entityId, CancellationToken.None);

		// Assert
		Assert.Equal("test.servicebus.windows.net", details.ServiceBusHostName);
		Assert.True(details.IsManagementApiAvailable);
		Assert.Equal([entityId], details.AvailableEntities);
		Assert.Equal("Subscription", details.Type);
		Assert.Equal("processed", details.Name);
		Assert.Equal("orders", details.TopicName);
		Assert.Same(subscription, details.Properties);
	}

	private static ReceivedMessage CreateMessage()
		=> new ReceivedMessage(
			Body: "body",
			Properties: new ReceivedMessageProperties(
				MessageId: "message-id",
				PartitionKey: null,
				SessionId: null,
				CorrelationId: null,
				ContentType: null,
				DeliveryCount: 1,
				EnqueuedTimeUtc: DateTimeOffset.UtcNow,
				ScheduledEnqueueTime: null,
				TimeToLive: null,
				To: null,
				ReplyTo: null,
				Subject: null),
			ApplicationProperties: new Dictionary<string, object>());
}
