namespace ServiceBusViewer.UnitTests.Infrastructure.ServiceBus;

using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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

	[Fact]
	public async Task ServiceBusConnection_RefreshEntityMessageCountsAsync_QueueEntity_UpdatesAffectedCountsOnly()
	{
		// Arrange
		const string affectedName = "queue-a";
		QueueEntityProperties affected = EntitiesData.CreateQueueProperties(affectedName);
		QueueEntityProperties untouched = EntitiesData.CreateQueueProperties("queue-b") with { MessageCounts = new EntityMessageCounts(7, 0, 0) };
		ServiceBusAdministrationClient adminClient = Substitute.For<ServiceBusAdministrationClient>();
		QueueRuntimeProperties runtime = ServiceBusModelFactory.QueueRuntimeProperties(affectedName, activeMessageCount: 5, scheduledMessageCount: 2, deadLetterMessageCount: 1);
		adminClient.GetQueueRuntimePropertiesAsync(affectedName, Arg.Any<CancellationToken>())
			.Returns(Response.FromValue(runtime, Substitute.For<Response>()));
		await using var connection = CreateConnection([affected, untouched], adminClient);

		// Act
		await connection.RefreshEntityMessageCountsAsync(new QueueEntityId(affectedName), CancellationToken.None);

		// Assert
		Assert.Equal(new EntityMessageCounts(5, 2, 1), connection.GetEntityProperties(new QueueEntityId(affectedName)).MessageCounts);
		Assert.Equal(new EntityMessageCounts(7, 0, 0), connection.GetEntityProperties(new QueueEntityId("queue-b")).MessageCounts);
	}

	[Fact]
	public async Task ServiceBusConnection_RefreshEntityMessageCountsAsync_SubscriptionEntity_UpdatesCounts()
	{
		// Arrange
		const string topicName = "events";
		const string subscriptionName = "sub-a";
		TopicEntityProperties topic = CreateTopicProperties(topicName);
		SubscriptionEntityProperties subscription = CreateSubscriptionProperties(subscriptionName, topicName);
		ServiceBusAdministrationClient adminClient = Substitute.For<ServiceBusAdministrationClient>();
		SubscriptionRuntimeProperties runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(topicName, subscriptionName, activeMessageCount: 3, deadLetterMessageCount: 1);
		adminClient.GetSubscriptionRuntimePropertiesAsync(topicName, subscriptionName, Arg.Any<CancellationToken>())
			.Returns(Response.FromValue(runtime, Substitute.For<Response>()));
		await using var connection = CreateConnection([topic, subscription], adminClient);

		// Act
		await connection.RefreshEntityMessageCountsAsync(new SubscriptionEntityId(subscriptionName, topicName), CancellationToken.None);

		// Assert
		Assert.Equal(new EntityMessageCounts(3, 0, 1), connection.GetEntityProperties(new SubscriptionEntityId(subscriptionName, topicName)).MessageCounts);
	}

	[Fact]
	public async Task ServiceBusConnection_RefreshEntityMessageCountsAsync_TopicEntity_RefreshesItsSubscriptionsOnly()
	{
		// Arrange
		const string topicName = "events";
		TopicEntityProperties topic = CreateTopicProperties(topicName);
		SubscriptionEntityProperties affected = CreateSubscriptionProperties("sub-a", topicName);
		SubscriptionEntityProperties untouched = CreateSubscriptionProperties("sub-b", "other") with { MessageCounts = new EntityMessageCounts(9, 0, 0) };
		ServiceBusAdministrationClient adminClient = Substitute.For<ServiceBusAdministrationClient>();
		SubscriptionRuntimeProperties runtime = ServiceBusModelFactory.SubscriptionRuntimeProperties(topicName, "sub-a", activeMessageCount: 4, deadLetterMessageCount: 2);
		adminClient.GetSubscriptionsRuntimePropertiesAsync(topicName, Arg.Any<CancellationToken>())
			.Returns(new TestAsyncPageable<SubscriptionRuntimeProperties>([runtime]));
		await using var connection = CreateConnection([topic, affected, untouched], adminClient);

		// Act
		await connection.RefreshEntityMessageCountsAsync(new TopicEntityId(topicName), CancellationToken.None);

		// Assert
		Assert.Equal(new EntityMessageCounts(4, 0, 2), connection.GetEntityProperties(new SubscriptionEntityId("sub-a", topicName)).MessageCounts);
		Assert.Equal(new EntityMessageCounts(9, 0, 0), connection.GetEntityProperties(new SubscriptionEntityId("sub-b", "other")).MessageCounts);
		Assert.Null(connection.GetEntityProperties(new TopicEntityId(topicName)).MessageCounts);
	}

	[Fact]
	public async Task ServiceBusConnection_RefreshEntityMessageCountsAsync_RetrievalFails_SetsCountsToNull()
	{
		// Arrange
		const string queueName = "queue-a";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName) with { MessageCounts = new EntityMessageCounts(5, 0, 0) };
		ServiceBusAdministrationClient adminClient = Substitute.For<ServiceBusAdministrationClient>();
		adminClient.GetQueueRuntimePropertiesAsync(queueName, Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("Management endpoint unavailable."));
		await using var connection = CreateConnection([queue], adminClient);

		// Act
		await connection.RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), CancellationToken.None);

		// Assert
		Assert.Null(connection.GetEntityProperties(new QueueEntityId(queueName)).MessageCounts);
	}

	[Fact]
	public async Task ServiceBusConnection_RefreshEntityMessageCountsAsync_ManagementUnavailable_SetsCountsToNull()
	{
		// Arrange
		const string queueName = "queue-a";
		QueueEntityProperties queue = EntitiesData.CreateQueueProperties(queueName) with { MessageCounts = new EntityMessageCounts(5, 0, 0) };
		await using var connection = CreateConnection(queue);

		// Act
		await connection.RefreshEntityMessageCountsAsync(new QueueEntityId(queueName), CancellationToken.None);

		// Assert
		Assert.Null(connection.GetEntityProperties(new QueueEntityId(queueName)).MessageCounts);
	}

	private static TopicEntityProperties CreateTopicProperties(string name)
		=> new(name, TimeSpan.FromDays(1), false, TimeSpan.FromMinutes(10), true, false, TimeSpan.FromDays(30));

	private static SubscriptionEntityProperties CreateSubscriptionProperties(string name, string topicName)
		=> new(name, topicName, false, TimeSpan.FromMinutes(1), 10, TimeSpan.FromDays(1), false, true, TimeSpan.FromDays(30), []);

	private static ServiceBusConnection CreateConnection(EntityProperties[] entities, ServiceBusAdministrationClient adminClient)
		=> new(
			new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;"),
			adminClient,
			"localhost",
			availableEntities: entities);

	private sealed class TestAsyncPageable<T>(IReadOnlyList<T> items) : AsyncPageable<T> where T : notnull
	{
		private readonly IReadOnlyList<T> _items = items;

		public override async IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
		{
			await Task.CompletedTask;
			yield return Page<T>.FromValues(_items, null, Substitute.For<Response>());
		}
	}

	private static ServiceBusConnection CreateConnection(params EntityProperties[] entities)
		=> new(
			new ServiceBusClient("Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;"),
			null,
			"localhost",
			availableEntities: entities);
}
