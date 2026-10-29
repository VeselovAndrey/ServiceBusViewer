namespace ServiceBusViewer.UnitTests.Business.Viewer.Services;

using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Business.Viewer.Services;
using ServiceBusViewer.UnitTests.Assets;
using ServiceBusViewer.UnitTests.TestSupport;
using Xunit;

public sealed class ViewerMessageServiceTests
{
	[Fact]
	public async Task ViewerMessageService_RefreshAsync_NonSessionEntity_ClearsReceiveSessionAndReplacesMessages()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);
		var refreshedMessages = new ReceivedMessageList([], true);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.PeekMessagesAsync(new QueueEntityId(queueName), 50, Arg.Any<CancellationToken>()).Returns(refreshedMessages);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);

		using var sessionScope = new ViewerSessionScope();

		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);
		session.CurrentMessages = ReceivedMessageList.Empty;
		session.ReceiveSessionId = "old-session";
		session.SendResultMessage = "Sent";

		// Act
		ViewerState state = await service.RefreshAsync(session, CancellationToken.None);

		// Assert
		Assert.Equal(refreshedMessages, session.CurrentMessages);
		Assert.Null(session.ReceiveSessionId);
		Assert.Null(session.SendResultMessage);
		Assert.True(state.HasMoreMessages);
	}

	[Fact]
	public async Task ViewerMessageService_UpdatePeekMessageCountAsync_ValidCount_UpdatesSessionAndRePeeksMessages()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);
		var refreshedMessages = new ReceivedMessageList([], true);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.PeekMessagesAsync(new QueueEntityId(queueName), 200, Arg.Any<CancellationToken>()).Returns(refreshedMessages);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);

		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);
		session.CurrentMessages = ReceivedMessageList.Empty;
		session.SendResultMessage = "Sent";

		// Act
		ViewerState state = await service.UpdatePeekMessageCountAsync(session, 200, CancellationToken.None);

		// Assert
		Assert.Equal(200, session.PeekMessageCount);
		Assert.Equal(refreshedMessages, session.CurrentMessages);
		Assert.Null(session.SendResultMessage);
		Assert.True(state.HasMoreMessages);
		await connection.Received(1).PeekMessagesAsync(new QueueEntityId(queueName), 200, Arg.Any<CancellationToken>());
	}

	[Theory]
	[InlineData(0)]
	[InlineData(501)]
	public async Task ViewerMessageService_UpdatePeekMessageCountAsync_InvalidCount_ThrowsAndLeavesPreviousCountApplied(int invalidCount)
	{
		// Arrange
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);

		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId("default-queue");

		// Act
		Task action() => service.UpdatePeekMessageCountAsync(session, invalidCount, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(action);
		Assert.Equal(50, session.PeekMessageCount);
		await connection.DidNotReceive().PeekMessagesAsync(Arg.Any<EntityId>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_SendAsync_SessionEntityHasNoSessionId_ThrowsSendMessageValidationException()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName, requiresSession: true);
		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);

		using var sessionScope = new ViewerSessionScope();

		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		var command = new SendCommand("body", new MessageProperties(), []);

		// Act
		Task action() => service.SendAsync(session, command, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<SendMessageValidationException>(action);
		await connection.DidNotReceive().SendMessageAsync(Arg.Any<EntityId>(), Arg.Any<SendCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_ReceiveAsync_SessionEntityWithSessionId_ReceivesFromSpecifiedSessionAndRefreshesMessages()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName, requiresSession: true);
		var refreshedMessages = new ReceivedMessageList([], true);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.ReceiveMessageAsync(new QueueEntityId(queueName), "session-42", Arg.Any<CancellationToken>()).Returns((ReceivedMessage?)null);
		connection.PeekMessagesAsync(new QueueEntityId(queueName), 50, Arg.Any<CancellationToken>()).Returns(refreshedMessages);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		// Act
		ViewerState state = await service.ReceiveAsync(session, "session-42", CancellationToken.None);

		// Assert
		Assert.Equal("session-42", session.ReceiveSessionId);
		Assert.Equal(refreshedMessages, session.CurrentMessages);
		Assert.True(state.HasMoreMessages);
		await connection.Received(1).ReceiveMessageAsync(new QueueEntityId(queueName), "session-42", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_ClearDisplayedMessageAsync_ConnectedSession_ClearsDisplayedMessageWithoutMessageRetrieval()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);
		session.CurrentMessages = ReceivedMessageList.Empty;
		session.DisplayedMessage = CreateDisplayedMessage();

		// Act
		ViewerState state = await service.ClearDisplayedMessageAsync(session, CancellationToken.None);

		// Assert
		Assert.Null(session.DisplayedMessage);
		Assert.Null(state.DisplayedMessage);
		await connection.DidNotReceive().ReceiveMessageAsync(Arg.Any<EntityId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
		await connection.DidNotReceive().PeekMessagesAsync(Arg.Any<EntityId>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_ClearDisplayedMessageAsync_DisconnectedSession_ThrowsViewerNotConnectedException()
	{
		// Arrange
		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = ViewerSessionScope.CreateDisconnected();
		IViewerSessionState session = sessionScope.State;
		session.DisplayedMessage = CreateDisplayedMessage();

		// Act
		Task action() => service.ClearDisplayedMessageAsync(session, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ViewerNotConnectedException>(action);
		Assert.NotNull(session.DisplayedMessage);
	}

	private static ReceivedMessage CreateDisplayedMessage()
		=> new ReceivedMessage(
			Body: "body",
			Properties: new ReceivedMessageProperties(
				MessageId: "message-id",
				SequenceNumber: 1,
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
				Subject: null,
				DeadLetterReason: null,
				DeadLetterSource: null,
				DeadLetterErrorDescription: null),
			ApplicationProperties: new Dictionary<string, object>());

	[Fact]
	public async Task ViewerMessageService_SendAsync_NonSessionEntity_SendsCommandAndClearsReceiveSession()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName, requiresSession: false);
		var command = new SendCommand("body", new MessageProperties(), []);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);
		session.ReceiveSessionId = "old-session";

		// Act
		await service.SendAsync(session, command, CancellationToken.None);

		// Assert
		Assert.Null(session.ReceiveSessionId);
		await connection.Received(1).SendMessageAsync(new QueueEntityId(queueName), command, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_RefreshAsync_ConnectedSession_RefreshesCountsOfQueuesAndTopicsOnly()
	{
		// Arrange
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties("default-queue");
		TopicEntityProperties topic = new("events", TimeSpan.FromDays(1), false, TimeSpan.FromMinutes(10), true, false, TimeSpan.FromDays(30));
		SubscriptionEntityProperties subscription = new("sub-a", "events", false, TimeSpan.FromMinutes(1), 10, TimeSpan.FromDays(1), false, true, TimeSpan.FromDays(30), []);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue, topic, subscription]);
		connection.PeekMessagesAsync(Arg.Any<EntityId>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ReceivedMessageList.Empty);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);

		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;

		// Act
		await service.RefreshAsync(session, CancellationToken.None);

		// Assert
		await connection.Received(1).RefreshEntityMessageCountsAsync(new QueueEntityId("default-queue"), Arg.Any<CancellationToken>());
		await connection.Received(1).RefreshEntityMessageCountsAsync(new TopicEntityId("events"), Arg.Any<CancellationToken>());
		await connection.DidNotReceive().RefreshEntityMessageCountsAsync(Arg.Is<SubscriptionEntityId>(id => id.Name == "sub-a"), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_SendAsync_SendCompleted_RefreshesAffectedEntityCounts()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName, requiresSession: false);
		var command = new SendCommand("body", new MessageProperties(), []);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		// Act
		await service.SendAsync(session, command, CancellationToken.None);

		// Assert
		await connection.Received(1).RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_ReceiveAsync_ReceiveCompleted_RefreshesAffectedEntityCounts()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.AvailableEntities.Returns((IReadOnlyList<EntityProperties>)[queue]);
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.ReceiveMessageAsync(new QueueEntityId(queueName), null, Arg.Any<CancellationToken>()).Returns((ReceivedMessage?)null);
		connection.PeekMessagesAsync(new QueueEntityId(queueName), 50, Arg.Any<CancellationToken>()).Returns(ReceivedMessageList.Empty);

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		// Act
		await service.ReceiveAsync(session, null, CancellationToken.None);

		// Assert
		await connection.Received(1).RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_DropDeadLetterMessagesAsync_OperationCompleted_RefreshesAffectedEntityCounts()
	{
		// Arrange
		const string queueName = "default-queue";

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.DropDeadLetterMessagesAsync(new QueueEntityId(queueName), Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<long?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
			.Returns(new DeadLetterOperationResult(true, 1, 0, null));

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		// Act
		await service.DropDeadLetterMessagesAsync(session, null, null, null, CancellationToken.None);

		// Assert
		await connection.Received(1).RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ViewerMessageService_RepublishDeadLetterMessagesAsync_OperationCompleted_RefreshesAffectedEntityCounts()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);

		IServiceBusConnection connection = Substitute.For<IServiceBusConnection>();
		connection.GetEntityProperties(new QueueEntityId(queueName)).Returns(queue);
		connection.RepublishDeadLetterMessagesAsync(new QueueEntityId(queueName), Arg.Any<IReadOnlyCollection<string>?>(), Arg.Any<long?>(), Arg.Any<RepublishMessageIdStrategy>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
			.Returns(new DeadLetterOperationResult(true, 1, 0, null));

		var service = new ViewerMessageService(NullLogger<ViewerMessageService>.Instance);
		using var sessionScope = new ViewerSessionScope();
		IViewerSessionState session = sessionScope.State;
		session.Connection = connection;
		session.SelectedEntityId = new QueueEntityId(queueName);

		// Act
		await service.RepublishDeadLetterMessagesAsync(session, null, null, RepublishMessageIdStrategy.Guid, null, null, CancellationToken.None);

		// Assert
		await connection.Received(1).RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), Arg.Any<CancellationToken>());
	}
}
