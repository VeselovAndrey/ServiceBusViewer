namespace ServiceBusViewer.Infrastructure.ServiceBus;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Provides methods to interact with Azure Service Bus, including connecting, sending, receiving, and peeking messages.</summary>
public class ServiceBusService
{
	private const int _maxMessagesToPeek = 50;

	private ServiceBusClient? _client;
	private ServiceBusAdministrationClient? _adminClient;
	private readonly List<EntityInfo> _availableEntities = new List<EntityInfo>();

	/// <summary>Gets a value indicating whether the service is connected to a Service Bus instance.</summary>
	public bool Connected { get; private set; }

	/// <summary>Gets the host name of the connected Service Bus instance.</summary>
	public string Host { get; private set; } = string.Empty;

	/// <summary>Gets the entity name (queue or topic) currently connected to.</summary>
	public string EntityName { get; private set; } = string.Empty;

	/// <summary>Gets the subscription name if connected to a topic subscription; otherwise, null.</summary>
	public string? SubscriptionName { get; private set; }

	/// <summary>Gets a value indicating whether connected using a root connection string with admin privileges.</summary>
	public bool IsRootConnection => _adminClient is not null;

	/// <summary>Gets the list of available entities when connected in root mode.</summary>
	public IReadOnlyList<EntityInfo> AvailableEntities => _availableEntities;

	/// <summary>Connects to the specified Service Bus entity.</summary>
	/// <param name="connectionString">The Service Bus connection string.</param>
	/// <param name="entityName">The queue or topic name.</param>
	/// <param name="subscriptionName">The subscription name, or null for queues.</param>
	/// <param name="rootConnectionString">Optional root connection string for admin operations.</param>
	/// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
	public async Task ConnectToAsync(string connectionString, string? rootConnectionString, string? entityName, string? subscriptionName)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		_client = new ServiceBusClient(connectionString);

		if (!string.IsNullOrWhiteSpace(rootConnectionString))
			_adminClient = new ServiceBusAdministrationClient(rootConnectionString);

		Host = GetServiceBusHost(connectionString);
		EntityName = entityName ?? string.Empty;
		SubscriptionName = subscriptionName;
		Connected = true;

		// Populate available entities list
		if (!IsRootConnection) {
			// Prepopulate entities list
			if (string.IsNullOrWhiteSpace(entityName))
				throw new InvalidOperationException("The topic or subscription name required.");

			if (!string.IsNullOrWhiteSpace(subscriptionName)) {
				_availableEntities.Add(new TopicEntityInfo(entityName));
				_availableEntities.Add(new SubscriptionEntityInfo(subscriptionName, entityName));
			}
			else {
				_availableEntities.Add(new QueueEntityInfo(entityName));
			}
		}

		await UpdateAvailableEntitiesListAsync();
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
		SubscriptionName = null;
		Connected = false;
	}

	/// <summary>Peeks a batch of messages from the connected Service Bus entity.</summary>
	/// <param name="maxMessages">The maximum number of messages to peek.</param>
	/// <returns>A <see cref="MessageList"/> containing the peeked messages and a flag indicating if more messages exist.</returns>
	public async Task<MessageList> PeekMessagesAsync(int maxMessages = _maxMessagesToPeek)
	{
		if (string.IsNullOrWhiteSpace(EntityName))
			return MessageList.Empty;

		await using ServiceBusReceiver receiver = GetReceiver();

		// Load limit+1 messages to detect if there are more
		IReadOnlyList<ServiceBusReceivedMessage>? messages = await receiver.PeekMessagesAsync(maxMessages + 1);

		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;

		var messageDetails = messagesToReturn.Select(m => new MessageDetails(
			m.MessageId,
			m.Body.ToString(),
			m.ContentType ?? "text/plain",
			m.EnqueuedTime,
			m.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)))
			.ToList();

		return new MessageList(messageDetails, hasMore);
	}

	/// <summary>Receives and completes a single message from the connected Service Bus entity.</summary>
	/// <returns>The <see cref="MessageDetails"/> of the received message, or null if no message is available.</returns>
	public async Task<MessageDetails?> ReceiveMessageAsync()
	{
		await using ServiceBusReceiver receiver = GetReceiver();

		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message);

		return new MessageDetails(
			message.MessageId,
			message.Body.ToString(),
			message.ContentType ?? "text/plain",
			message.EnqueuedTime,
			message.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
	}

	/// <summary>Sends a message to the connected Service Bus entity.</summary>
	/// <param name="content">The message content.</param>
	/// <param name="contentType">The content type of the message.</param>
	/// <param name="properties">The application properties to include with the message.</param>
	public async Task SendMessageAsync(string content, string contentType, Dictionary<string, object> properties)
	{
		await using ServiceBusSender sender = GetSender();

		ServiceBusMessage message = new ServiceBusMessage(content) {
			ContentType = contentType,
			MessageId = Guid.NewGuid().ToString()
		};

		foreach (KeyValuePair<string, object> property in properties) {
			if (!string.IsNullOrEmpty(property.Key))
				message.ApplicationProperties[property.Key] = property.Value;
		}

		await sender.SendMessageAsync(message);
	}

	/// <summary>Gets a <see cref="ServiceBusReceiver"/> for the current entity and subscription.</summary>
	/// <returns>A <see cref="ServiceBusReceiver"/> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	private ServiceBusReceiver GetReceiver()
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (!string.IsNullOrWhiteSpace(SubscriptionName)) {
			return _client.CreateReceiver(EntityName, SubscriptionName, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		return _client.CreateReceiver(EntityName, new ServiceBusReceiverOptions {
			ReceiveMode = ServiceBusReceiveMode.PeekLock
		});
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

		ReadOnlySpan<char> span = connectionString.AsSpan();
		ReadOnlySpan<char> host = span.Slice(connectionStringPrefix.Length, endIndex - connectionStringPrefix.Length);
		host = host.TrimEnd('/');

		return host.ToString();
	}

	/// <summary>Retrieves all available entities (queues, topics, and subscriptions) from the Service Bus namespace.</summary>
	/// <returns>A list of <see cref="EntityInfo" /> representing all entities.</returns>
	/// <exception cref="InvalidOperationException">Thrown if not connected in root mode.</exception>
	public async Task UpdateAvailableEntitiesListAsync()
	{
		if (_adminClient is null) // Keep pre-configured entity list if admin client was not configured
			return;

		_availableEntities.Clear();

		// Get all queues
		await foreach (QueueProperties? queue in _adminClient.GetQueuesAsync())
			_availableEntities.Add(new QueueEntityInfo(queue.Name));


		// Get all topics and their subscriptions
		await foreach (TopicProperties? topic in _adminClient.GetTopicsAsync()) {
			_availableEntities.Add(new TopicEntityInfo(topic.Name));

			await foreach (SubscriptionProperties? subscription in _adminClient.GetSubscriptionsAsync(topic.Name))
				_availableEntities.Add(new SubscriptionEntityInfo(subscription.SubscriptionName, topic.Name));
		}
	}

	/// <summary>Switches the active entity without disconnecting from the Service Bus.</summary>
	/// <param name="entity">The entity to switch to.</param>
	/// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
	public async void SwitchEntity(EntityInfo entity)
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Not connected to any Service Bus instance.");

		// No need to create a new client, just update the entity information
		switch (entity) {
			case QueueEntityInfo queue:
				EntityName = queue.Name;
				SubscriptionName = null;
				break;

			case SubscriptionEntityInfo subscription:
				EntityName = subscription.TopicName;
				SubscriptionName = subscription.Name;
				break;

			case TopicEntityInfo topic:
				throw new InvalidOperationException($"Please select the subscription for the topic {topic.Name}");

			default:
				throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}");
		}
	}
}
