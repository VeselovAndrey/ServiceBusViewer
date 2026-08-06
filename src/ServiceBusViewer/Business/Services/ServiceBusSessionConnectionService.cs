namespace ServiceBusViewer.Business.Services;

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Manages a single browser-session-scoped Service Bus connection and entity cache.</summary>
internal sealed class ServiceBusSessionConnectionService : IServiceBusSessionConnectionService
{
	private const int MaxMessagesToPeek = 50;

	private readonly List<EntityProperties> _availableEntities = [];
	private ServiceBusClient? _client;
	private ServiceBusAdministrationClient? _adminClient;

	public bool Connected { get; private set; }

	public string Host { get; private set; } = string.Empty;

	public EntityProperties? ActiveEntity { get; private set; }

	public bool IsManagementApiAvailable => _adminClient is not null;

	public IReadOnlyList<EntityProperties> AvailableEntities => _availableEntities;

	public async Task ConnectToAsync(string connectionString, string rootConnectionString)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		_client = new ServiceBusClient(connectionString);
		_adminClient = new ServiceBusAdministrationClient(rootConnectionString);
		Host = GetServiceBusHost(connectionString);
		ActiveEntity = null;
		Connected = true;

		await UpdateAvailableEntitiesListAsync();
	}

	public void ConnectTo(string connectionString, string queueOrTopicName, string? subscriptionName)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		if (string.IsNullOrWhiteSpace(queueOrTopicName))
			throw new InvalidOperationException("Queue or topic name is required.");

		_client = new ServiceBusClient(connectionString);
		_adminClient = null;
		Host = GetServiceBusHost(connectionString);
		Connected = true;

		if (!string.IsNullOrWhiteSpace(subscriptionName)) {
			_availableEntities.Add(new TopicEntityProperties(
				queueOrTopicName,
				TimeSpan.MaxValue,
				false,
				TimeSpan.FromMinutes(10),
				true,
				false,
				TimeSpan.MaxValue));

			SubscriptionEntityProperties subscriptionProperties = new(
				subscriptionName,
				queueOrTopicName,
				TimeSpan.FromMinutes(1),
				10,
				TimeSpan.MaxValue,
				false,
				false,
				true,
				TimeSpan.MaxValue,
				[]);

			_availableEntities.Add(subscriptionProperties);
			ActiveEntity = subscriptionProperties;
			return;
		}

		QueueEntityProperties queueProperties = new(
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
			TimeSpan.MaxValue);

		_availableEntities.Add(queueProperties);
		ActiveEntity = queueProperties;
	}

	public EntityProperties GetEntityProperties(EntityId entityId)
	{
		EntityProperties? properties = entityId switch {
			QueueEntityId queueId => _availableEntities.FirstOrDefault(p => p is QueueEntityProperties && p.Name == queueId.Name),
			TopicEntityId topicId => _availableEntities.FirstOrDefault(p => p is TopicEntityProperties && p.Name == topicId.Name),
			SubscriptionEntityId subscriptionId => _availableEntities.FirstOrDefault(p => p is SubscriptionEntityProperties subscription && subscription.Name == subscriptionId.Name && subscription.TopicName == subscriptionId.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}", nameof(entityId))
		};

		return properties
			?? throw new InvalidOperationException($"Entity properties for '{entityId.Name}' not found in cache. Please refresh the entity list.");
	}

	public async Task DisconnectAsync()
	{
		if (_client is not null)
			await _client.DisposeAsync();

		_client = null;
		_adminClient = null;
		_availableEntities.Clear();
		Host = string.Empty;
		ActiveEntity = null;
		Connected = false;
	}

	public async Task<ReceivedMessageList> PeekMessagesAsync(int maxMessages = MaxMessagesToPeek)
	{
		if (ActiveEntity is null || ActiveEntity is TopicEntityProperties)
			return ReceivedMessageList.Empty;

		await using ServiceBusReceiver receiver = GetReceiver();
		IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(maxMessages + 1);
		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;
		return new ReceivedMessageList(messagesToReturn.Select(ConvertToReceivedMessage).ToList(), hasMore);
	}

	public async Task<ReceivedMessage?> ReceiveMessageAsync()
	{
		await using ServiceBusReceiver receiver = GetReceiver();
		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message);
		return ConvertToReceivedMessage(message);
	}

	public async Task<ReceivedMessage?> ReceiveSessionMessageAsync(string? sessionId)
	{
		await using ServiceBusSessionReceiver receiver = await GetSessionReceiver(sessionId);
		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message);
		return ConvertToReceivedMessage(message);
	}

	public async Task SendMessageAsync(string content, MessageProperties? messageProperties, IReadOnlyList<ApplicationProperty> applicationProperties)
	{
		await using ServiceBusSender sender = GetSender();
		ServiceBusMessage message = new(content);

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
			if (!string.IsNullOrWhiteSpace(property.Key))
				message.ApplicationProperties[property.Key] = ConvertApplicationPropertyValue(property.Value, property.Type);
		}

		await sender.SendMessageAsync(message);
	}

	public async Task UpdateAvailableEntitiesListAsync()
	{
		if (_adminClient is null)
			throw new InvalidOperationException("Entity list refresh is only available when connected with a root connection string.");

		_availableEntities.Clear();

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
				List<SubscriptionFilterRule> rules = [];

				await foreach (RuleProperties rule in _adminClient.GetRulesAsync(topic.Name, subscription.SubscriptionName)) {
					rules.Add(ConvertToSubscriptionRule(rule));
				}

				_availableEntities.Add(new SubscriptionEntityProperties(
					subscription.SubscriptionName,
					subscription.TopicName,
					subscription.LockDuration,
					subscription.MaxDeliveryCount,
					subscription.DefaultMessageTimeToLive,
					subscription.DeadLetteringOnMessageExpiration,
					subscription.RequiresSession,
					subscription.EnableBatchedOperations,
					subscription.AutoDeleteOnIdle,
					rules));
			}
		}
	}

	public void SwitchActiveEntity(EntityId entity)
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Not connected to any Service Bus instance.");

		ActiveEntity = entity switch {
			QueueEntityId queueId => _availableEntities.FirstOrDefault(p => p is QueueEntityProperties && p.Name == queueId.Name),
			TopicEntityId topicId => _availableEntities.FirstOrDefault(p => p is TopicEntityProperties && p.Name == topicId.Name),
			SubscriptionEntityId subscriptionId => _availableEntities.FirstOrDefault(p => p is SubscriptionEntityProperties subscription && subscription.Name == subscriptionId.Name && subscription.TopicName == subscriptionId.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
		};

		if (ActiveEntity is null)
			throw new InvalidOperationException($"Entity '{entity.Name}' not found in available entities.");
	}

	public ValueTask DisposeAsync() => new(DisconnectAsync());

	private ServiceBusReceiver GetReceiver()
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (ActiveEntity is null)
			throw new InvalidOperationException("No active entity selected.");

		if (ActiveEntity is SubscriptionEntityProperties subscription) {
			return _client.CreateReceiver(subscription.TopicName, subscription.Name, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		if (ActiveEntity is QueueEntityProperties queue) {
			return _client.CreateReceiver(queue.Name, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.");
	}

	private async Task<ServiceBusSessionReceiver> GetSessionReceiver(string? sessionId)
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (ActiveEntity is null)
			throw new InvalidOperationException("No active entity selected.");

		if (ActiveEntity is SubscriptionEntityProperties subscription) {
			return string.IsNullOrWhiteSpace(sessionId)
				? await _client.AcceptNextSessionAsync(subscription.TopicName, subscription.Name)
				: await _client.AcceptSessionAsync(subscription.TopicName, subscription.Name, sessionId);
		}

		if (ActiveEntity is QueueEntityProperties queue) {
			return string.IsNullOrWhiteSpace(sessionId)
				? await _client.AcceptNextSessionAsync(queue.Name)
				: await _client.AcceptSessionAsync(queue.Name, sessionId);
		}

		throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.");
	}

	private ServiceBusSender GetSender()
	{
		if (!Connected || _client is null)
			throw new InvalidOperationException("Service Bus is not connected.");

		if (ActiveEntity is null)
			throw new InvalidOperationException("No active entity selected.");

		return ActiveEntity switch {
			QueueEntityProperties queue => _client.CreateSender(queue.Name),
			SubscriptionEntityProperties subscription => _client.CreateSender(subscription.TopicName),
			TopicEntityProperties topic => _client.CreateSender(topic.Name),
			_ => throw new InvalidOperationException($"Cannot create sender for entity type '{ActiveEntity.GetType().Name}'.")
		};
	}

	private static string GetServiceBusHost(string connectionString)
	{
		const string connectionStringPrefix = "Endpoint=sb://";

		if (!connectionString.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Invalid Service Bus connection string.", nameof(connectionString));

		int endIndex = connectionString.IndexOf(';', connectionStringPrefix.Length);
		if (endIndex < 0)
			endIndex = connectionString.Length;

		ReadOnlySpan<char> host = connectionString.AsSpan(connectionStringPrefix.Length, endIndex - connectionStringPrefix.Length)
			.TrimEnd('/');

		return host.ToString();
	}

	private static ReceivedMessage ConvertToReceivedMessage(ServiceBusReceivedMessage message)
	{
		ReceivedMessageProperties properties = new(
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
			message.ApplicationProperties.ToDictionary(static pair => pair.Key, static pair => pair.Value));
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

	private static SubscriptionFilterRule ConvertToSubscriptionRule(RuleProperties rule)
	{
		if (rule.Filter is SqlRuleFilter sqlFilter) {
			string? actionExpression = rule.Action is SqlRuleAction sqlAction
				? sqlAction.SqlExpression
				: null;

			return new SqlSubscriptionFilterRule(rule.Name, sqlFilter.SqlExpression, actionExpression);
		}

		if (rule.Filter is CorrelationRuleFilter correlationFilter) {
			string? actionExpression = rule.Action is SqlRuleAction sqlAction
				? sqlAction.SqlExpression
				: null;

			IReadOnlyDictionary<string, object> applicationProperties = correlationFilter.ApplicationProperties
				.ToDictionary(static pair => pair.Key, static pair => pair.Value);

			return new CorrelationSubscriptionFilterRule(
				rule.Name,
				correlationFilter.CorrelationId,
				correlationFilter.MessageId,
				correlationFilter.To,
				correlationFilter.ReplyTo,
				correlationFilter.Subject,
				correlationFilter.SessionId,
				correlationFilter.ReplyToSessionId,
				correlationFilter.ContentType,
				applicationProperties,
				actionExpression);
		}

		return new UnknownSubscriptionFilterRule(
			rule.Name,
			rule.Filter?.GetType().Name ?? "null",
			"Unknown filter type");
	}
}
