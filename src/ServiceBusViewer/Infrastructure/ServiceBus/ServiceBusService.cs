namespace ServiceBusViewer.Infrastructure.ServiceBus;

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Provides methods to interact with Azure Service Bus, including connecting, sending, receiving, and peeking messages.</summary>
public class ServiceBusService
{
	private const int _maxMessagesToPeek = 50;

	private ServiceBusClient? _client;
	private ServiceBusAdministrationClient? _adminClient;
	private readonly List<EntityProperties> _availableEntities = new List<EntityProperties>();

	/// <summary>Gets a value indicating whether the service is connected to a Service Bus instance.</summary>
	public bool Connected { get; private set; }

	/// <summary>Gets the host name of the connected Service Bus instance.</summary>
	public string Host { get; private set; } = string.Empty;

	/// <summary>Gets the entity name (queue or topic) currently connected to.</summary>
	public string EntityName { get; private set; } = string.Empty;

	/// <summary>Gets the parent topic if connected to a topic subscription; otherwise, null.</summary>
	public string? TopicName { get; private set; }

	/// <summary>Gets a value indicating whether connected using a root connection string with admin privileges.</summary>
	public bool IsManagementApiAvailable => _adminClient is not null;

	/// <summary>Gets the list of available entity properties with full details.</summary>
	public IReadOnlyList<EntityProperties> AvailableEntities => _availableEntities;

	/// <summary>Connects to the specified Service Bus entity.</summary>
	/// <param name="connectionString">The Service Bus connection string.</param>
	/// <param name="rootConnectionString">Optional root connection string for admin operations.</param>
	/// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
	public async Task ConnectToAsync(string connectionString, string rootConnectionString)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		_client = new ServiceBusClient(connectionString);
		_adminClient = new ServiceBusAdministrationClient(rootConnectionString);

		Host = GetServiceBusHost(connectionString);
		EntityName = string.Empty;
		TopicName = null;
		Connected = true;

		await UpdateAvailableEntitiesListAsync();
	}

	/// <summary>Connects to the specified Service Bus entity without using ServiceBusAdministrationClient to get the list of entities.</summary>
	/// <param name="connectionString">The Service Bus connection string.</param>
	/// <param name="queueOrTopicName">The queue or topic name.</param>
	/// <param name="subscriptionName">The subscription name, or <c>null</c> for queues.</param>
	/// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
	public void ConnectTo(string connectionString, string queueOrTopicName, string? subscriptionName)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		_client = new ServiceBusClient(connectionString);
		_adminClient = null;

		Host = GetServiceBusHost(connectionString);
		EntityName = subscriptionName ?? queueOrTopicName;
		TopicName = subscriptionName is not null ? queueOrTopicName : null;
		Connected = true;

		// Non-admin mode: create minimal property objects for the connected entity
		if (string.IsNullOrWhiteSpace(queueOrTopicName))
			throw new InvalidOperationException("The topic or subscription name required.");

		if (!string.IsNullOrWhiteSpace(subscriptionName)) {
			// For subscriptions, create both topic and subscription property placeholders
			_availableEntities.Add(new TopicEntityProperties(
				queueOrTopicName,
				TimeSpan.MaxValue,
				false,
				TimeSpan.FromMinutes(10),
				true,
				false,
				TimeSpan.MaxValue));
			_availableEntities.Add(new SubscriptionEntityProperties(
				subscriptionName,
				queueOrTopicName,
				TimeSpan.FromMinutes(1),
				10,
				TimeSpan.MaxValue,
				false,
				false,
				true,
				TimeSpan.MaxValue));
		}
		else {
			// For queues, create a queue property placeholder
			_availableEntities.Add(new QueueEntityProperties(
				queueOrTopicName,
				TimeSpan.FromMinutes(1),
				10,
				TimeSpan.MaxValue,
				false,
				TimeSpan.FromMinutes(10),
				false,
				true,
				false,
				false,
				TimeSpan.MaxValue));
		}
	}

	/// <summary>Returns properties for the specified entity from the cache.</summary>
	/// <param name="entityId">Entity to load properties for.</param>
	/// <returns>Entity properties with a type matching the requested entity.</returns>
	/// <exception cref="InvalidOperationException">Thrown if entity is not found in cache.</exception>
	public EntityProperties GetEntityPropertiesAsync(EntityId entityId)
	{
		EntityProperties? properties = entityId switch {
			QueueEntityId queueId => _availableEntities.FirstOrDefault(p => p is QueueEntityProperties && p.Name == queueId.Name),
			TopicEntityId topicId => _availableEntities.FirstOrDefault(p => p is TopicEntityProperties && p.Name == topicId.Name),
			SubscriptionEntityId subscriptionId => _availableEntities.FirstOrDefault(p => p is SubscriptionEntityProperties sp && sp.Name == subscriptionId.Name && sp.TopicName == subscriptionId.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}")
		};

		if (properties is null)
			throw new InvalidOperationException($"Entity properties for '{entityId.Name}' not found in cache. Please refresh the entity list.");

		return properties;
	}

	/// <summary>Disconnects asynchronously from the current Service Bus instance.</summary>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	public async Task DisconnectAsync()
	{
		if (!Connected)
			throw new InvalidOperationException("Not connected to any Service Bus instance.");

		if (_client is not null)
			await _client.DisposeAsync();

		_client = null;
		_adminClient = null;
		_availableEntities.Clear();

		Host = string.Empty;
		EntityName = string.Empty;
		TopicName = null;
		Connected = false;
	}

	/// <summary>Peeks a batch of messages from the connected Service Bus entity.</summary>
	/// <param name="maxMessages">The maximum number of messages to peek.</param>
	/// <returns>A <see cref="ReceivedMessageList"/> containing the peeked messages and a flag indicating if more messages exist.</returns>
	public async Task<ReceivedMessageList> PeekMessagesAsync(int maxMessages = _maxMessagesToPeek)
	{
		if (string.IsNullOrWhiteSpace(EntityName))
			return ReceivedMessageList.Empty;

		await using ServiceBusReceiver receiver = GetReceiver();

		// Load limit+1 messages to detect if there are more
		IReadOnlyList<ServiceBusReceivedMessage>? messages = await receiver.PeekMessagesAsync(maxMessages + 1);

		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;

		var messageDetails = messagesToReturn.Select(ConvertToMessageDetails).ToList();

		return new ReceivedMessageList(messageDetails, hasMore);
	}

	/// <summary>Receives and completes a single message from the connected Service Bus entity.</summary>
	/// <returns>The <see cref="ReceivedMessage"/> of the received message, or null if no message is available.</returns>
	public async Task<ReceivedMessage?> ReceiveMessageAsync()
	{
		await using ServiceBusReceiver receiver = GetReceiver();

		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message);

		return ConvertToMessageDetails(message);
	}

	/// <summary>Receives and completes a single message from a session-enabled Service Bus entity.</summary>
	/// <param name="sessionId">The session ID to accept. If null or empty, accepts the next available session.</param>
	/// <returns>The <see cref="ReceivedMessage"/> of the received message, or null if no message is available.</returns>
	public async Task<ReceivedMessage?> ReceiveSessionMessageAsync(string? sessionId)
	{
		await using ServiceBusSessionReceiver sessionReceiver = await GetSessionReceiver(sessionId);

		ServiceBusReceivedMessage? message = await sessionReceiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await sessionReceiver.CompleteMessageAsync(message);

		return ConvertToMessageDetails(message);
	}

	/// <summary>Sends a message to the connected Service Bus entity.</summary>
	/// <param name="content">The message content.</param>
	/// <param name="messageProperties">The optional system properties to include with the message.</param>
	/// <param name="applicationProperties">The application properties to include with the message.</param>
	public async Task SendMessageAsync(string content, MessageProperties? messageProperties, IReadOnlyList<ApplicationProperty> applicationProperties)
	{
		await using ServiceBusSender sender = GetSender();

		ServiceBusMessage message = new ServiceBusMessage(content);

		if (messageProperties is not null) {
			if (!string.IsNullOrWhiteSpace(messageProperties.MessageId))
				message.MessageId = messageProperties.MessageId;

			if (!string.IsNullOrWhiteSpace(messageProperties.ContentType))
				message.ContentType = messageProperties.ContentType;

			if (!string.IsNullOrWhiteSpace(messageProperties.SessionId))
				message.SessionId = messageProperties.SessionId;

			if (!string.IsNullOrWhiteSpace(messageProperties.CorrelationId))
				message.CorrelationId = messageProperties.CorrelationId;

			if (messageProperties.ScheduledEnqueueTime is not null)
				message.ScheduledEnqueueTime = messageProperties.ScheduledEnqueueTime.Value;

			if (messageProperties.TimeToLive is not null)
				message.TimeToLive = messageProperties.TimeToLive.Value;
		}

		foreach (ApplicationProperty property in applicationProperties) {
			if (!string.IsNullOrEmpty(property.Key))
				message.ApplicationProperties[property.Key] = ConvertApplicationPropertyValue(property.Value, property.Type);
		}

		await sender.SendMessageAsync(message);
	}

	/// <summary>Retrieves all available entities (queues, topics, and subscriptions) with their properties from the Service Bus namespace.</summary>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <exception cref="InvalidOperationException">Thrown if admin client is not available.</exception>
	public async Task UpdateAvailableEntitiesListAsync()
	{
		if (_adminClient is null)
			throw new InvalidOperationException("Entity list refresh is only available when connected with a root connection string.");

		_availableEntities.Clear();

		// Get all queues with properties
		await foreach (QueueProperties queue in _adminClient.GetQueuesAsync()) {
			_availableEntities.Add(new QueueEntityProperties(
				queue.Name,
				queue.LockDuration,
				queue.MaxDeliveryCount,
				queue.DefaultMessageTimeToLive,
				queue.RequiresDuplicateDetection,
				queue.DuplicateDetectionHistoryTimeWindow,
				queue.DeadLetteringOnMessageExpiration,
				queue.EnableBatchedOperations,
				queue.RequiresSession,
				queue.EnablePartitioning,
				queue.AutoDeleteOnIdle));
		}

		// Get all topics and their subscriptions with properties
		await foreach (TopicProperties topic in _adminClient.GetTopicsAsync()) {
			_availableEntities.Add(new TopicEntityProperties(
				topic.Name,
				topic.DefaultMessageTimeToLive,
				topic.RequiresDuplicateDetection,
				topic.DuplicateDetectionHistoryTimeWindow,
				topic.EnableBatchedOperations,
				topic.EnablePartitioning,
				topic.AutoDeleteOnIdle));

			await foreach (SubscriptionProperties subscription in _adminClient.GetSubscriptionsAsync(topic.Name)) {
				_availableEntities.Add(new SubscriptionEntityProperties(
					subscription.SubscriptionName,
					subscription.TopicName,
					subscription.LockDuration,
					subscription.MaxDeliveryCount,
					subscription.DefaultMessageTimeToLive,
					subscription.DeadLetteringOnMessageExpiration,
					subscription.RequiresSession,
					subscription.EnableBatchedOperations,
					subscription.AutoDeleteOnIdle));
			}
		}
	}


	/// <summary>Switches the active entity without disconnecting from the Service Bus.</summary>
	/// <param name="entity">The entity to switch to.</param>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	public void SwitchActiveEntity(EntityId entity)
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Not connected to any Service Bus instance.");

		// No need to create a new client, just update the entity information
		switch (entity) {
			case QueueEntityId queueId:
				EntityName = queueId.Name;
				TopicName = null;
				break;

			case SubscriptionEntityId subscriptionId:
				EntityName = subscriptionId.Name;
				TopicName = subscriptionId.TopicName;
				break;

			case TopicEntityId topicId:
				throw new InvalidOperationException($"Please select the subscription for the topic {topicId.Name}");

			default:
				throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}");
		}
	}

	/// <summary>Gets a <see cref="ServiceBusReceiver"/> for the current entity and subscription.</summary>
	/// <returns>A <see cref="ServiceBusReceiver"/> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	private ServiceBusReceiver GetReceiver()
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (!string.IsNullOrWhiteSpace(TopicName)) {
			return _client.CreateReceiver(TopicName, EntityName, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		return _client.CreateReceiver(EntityName, new ServiceBusReceiverOptions {
			ReceiveMode = ServiceBusReceiveMode.PeekLock
		});
	}

	/// <summary>Gets a <see cref="ServiceBusSessionReceiver"/> for the current entity.</summary>
	/// <param name="sessionId">The session ID to accept. If null or empty, accepts the next available session.</param>
	/// <returns>A <see cref="ServiceBusSessionReceiver"/> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	private async Task<ServiceBusSessionReceiver> GetSessionReceiver(string? sessionId)
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (!string.IsNullOrWhiteSpace(TopicName)) {
			return string.IsNullOrEmpty(sessionId)
				? await _client.AcceptNextSessionAsync(TopicName, EntityName)
				: await _client.AcceptSessionAsync(TopicName, EntityName, sessionId);
		}

		return string.IsNullOrEmpty(sessionId)
			? await _client.AcceptNextSessionAsync(EntityName)
			: await _client.AcceptSessionAsync(EntityName, sessionId);
	}

	/// <summary>Gets a <see cref="ServiceBusSender"/> for the current entity.</summary>
	/// <returns>A <see cref="ServiceBusSender"/> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	private ServiceBusSender GetSender()
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		return _client.CreateSender(EntityName);
	}

	/// <summary>Extracts the Service Bus host from the connection string.</summary>
	/// <param name="connectionString">The Service Bus connection string.</param>
	/// <returns>The host name and port.</returns>
	/// <exception cref="ArgumentException">Thrown if the connection string is invalid.</exception>
	private static string GetServiceBusHost(string connectionString)
	{
		const string connectionStringPrefix = "Endpoint=sb://";

		if (!connectionString.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Invalid Service Bus connection string.", nameof(connectionString));

		int endIndex = connectionString.IndexOf(';', connectionStringPrefix.Length);
		ReadOnlySpan<char> host = connectionString.AsSpan(connectionStringPrefix.Length, endIndex - connectionStringPrefix.Length)
			.TrimEnd('/');

		return host.ToString();
	}

	private static ReceivedMessage ConvertToMessageDetails(ServiceBusReceivedMessage message)
	{
		var properties = new ReceivedMessageProperties(
			message.MessageId,
			message.PartitionKey,
			message.SessionId,
			message.CorrelationId,
			message.ContentType,
			message.EnqueuedTime,
			message.ScheduledEnqueueTime != DateTimeOffset.MinValue ? message.ScheduledEnqueueTime : null,
			message.TimeToLive != TimeSpan.MaxValue ? message.TimeToLive : null);

		return new ReceivedMessage(
			message.Body.ToString(),
			properties,
			message.ApplicationProperties);
	}

	private static object ConvertApplicationPropertyValue(string value, ApplicationPropertyType type)
	{
		ArgumentNullException.ThrowIfNull(value);

		try {
			return type switch {
				ApplicationPropertyType.String => value,
				ApplicationPropertyType.Bool => bool.Parse(value),
				ApplicationPropertyType.Byte => byte.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.SByte => sbyte.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Short => short.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.UShort => ushort.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Int => int.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.UInt => uint.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Long => long.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.ULong => ulong.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Float => float.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Double => double.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Decimal => decimal.Parse(value, CultureInfo.InvariantCulture),
				ApplicationPropertyType.Char => value.Length == 1 ? value[0] : throw new FormatException("Char value must be a single character."),
				ApplicationPropertyType.Guid => Guid.Parse(value),
				ApplicationPropertyType.DateTime => DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
				ApplicationPropertyType.DateTimeOffset => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
				ApplicationPropertyType.TimeSpan => TimeSpan.Parse(value, CultureInfo.InvariantCulture),

				_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported application property type.")
			};
		}
		catch (FormatException ex) {
			throw new FormatException($"Cannot convert value '{value}' to type {type}. {ex.Message}", ex);
		}
		catch (OverflowException ex) {
			throw new FormatException($"Value '{value}' is out of range for type {type}. {ex.Message}", ex);
		}
	}
}
