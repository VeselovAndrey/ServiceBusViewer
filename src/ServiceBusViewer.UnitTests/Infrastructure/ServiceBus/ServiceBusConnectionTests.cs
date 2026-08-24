namespace ServiceBusViewer.UnitTests.Infrastructure.ServiceBus;

using Azure.Messaging.ServiceBus;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.UnitTests.Assets;
using Xunit;

public sealed class ServiceBusConnectionTests
{
	[Fact]
	public async Task ServiceBusConnection_GetEntityProperties_EntityExists_ReturnsCachedProperties()
	{
		// Arrange
		const string queueName = "default-queue";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName);
		await using var connection = CreateConnection(queue);

		// Act
		EntityProperties properties = connection.GetEntityProperties(new QueueEntityId(queueName));

		// Assert
		Assert.Same(queue, properties);
	}

	[Fact]
	public async Task ServiceBusConnection_PeekMessagesAsync_TopicEntity_ReturnsEmptyWithoutReceiving()
	{
		// Arrange
		var topic = new TopicEntityProperties("events", TimeSpan.FromDays(1), false, TimeSpan.FromMinutes(10), true, false, TimeSpan.FromDays(30));
		await using var connection = CreateConnection(topic);

		// Act
		ReceivedMessageList messages = await connection.PeekMessagesAsync(new TopicEntityId("events"), 50, CancellationToken.None);

		// Assert
		Assert.Equal(ReceivedMessageList.Empty, messages);
	}

	[Fact]
	public async Task ServiceBusConnection_GetEntityProperties_EntityIsMissing_ThrowsInvalidOperationException()
	{
		// Arrange
		const string queueName = "default-queue";
		await using var connection = CreateConnection(EntitiesData.CreateQueueProperties(queueName));

		// Act
		Action action = () => connection.GetEntityProperties(new QueueEntityId("missing"));

		// Assert
		Assert.Throws<InvalidOperationException>(action);
	}

	[Fact]
	public async Task ServiceBusConnection_DisposeAsync_ConnectionIsDisposed_RejectsFurtherOperations()
	{
		// Arrange
		const string queueName = "default-queue";
		var connection = CreateConnection(EntitiesData.CreateQueueProperties(queueName));

		// Act
		await connection.DisposeAsync();

		// Assert
		Assert.Throws<ObjectDisposedException>(() => connection.GetEntityProperties(new QueueEntityId(queueName)));
	}

	private static ServiceBusConnection CreateConnection(params EntityProperties[] entities)
		=> new(
			new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;"),
			null,
			"localhost",
			entities);
}
