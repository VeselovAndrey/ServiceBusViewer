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
}
