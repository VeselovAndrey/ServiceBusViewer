namespace ServiceBusViewer.Infrastructure.ServiceBus;

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.ServiceBus.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Manages a single browser-session-scoped Service Bus connection and entity cache.</summary>
internal sealed class ServiceBusConnection : IServiceBusConnection
{
	private const int _maxMessagesToPeek = 50;

	private readonly ServiceBusClient _client;
	private readonly ServiceBusAdministrationClient? _adminClient;
	private readonly List<EntityProperties> _availableEntities = [];
	private bool _disposed;

	public ServiceBusConnection(
		ServiceBusClient client,
		ServiceBusAdministrationClient? adminClient,
		string namespaceHost,
		IEnumerable<EntityProperties>? availableEntities = null)
	{
		_client = client;
		_adminClient = adminClient;
		NamespaceHost = namespaceHost;

		if (availableEntities is not null)
			_availableEntities.AddRange(availableEntities);
	}

	public string NamespaceHost { get; }

	public bool IsManagementApiAvailable => _adminClient is not null;

	public IReadOnlyList<EntityProperties> AvailableEntities => _availableEntities;

	public EntityProperties GetEntityProperties(EntityId entityId)
	{
		ThrowIfDisposed();

		EntityProperties? properties = FindEntity(entityId);

		return properties
			?? throw new InvalidOperationException($"Entity properties for '{entityId.Name}' not found in cache. Please refresh the entity list.");
	}

	public async Task<ReceivedMessageList> PeekMessagesAsync(EntityId entityId, int maxMessages = _maxMessagesToPeek)
	{
		ThrowIfDisposed();

		EntityProperties entity = GetEntityProperties(entityId);

		if (entity is TopicEntityProperties)
			return ReceivedMessageList.Empty;

		await using ServiceBusReceiver receiver = GetReceiver(entity);
		IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(maxMessages + 1);
		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;
		return new ReceivedMessageList(messagesToReturn.Select(ConvertToReceivedMessage).ToList(), hasMore);
	}

	public async Task<ReceivedMessage?> ReceiveMessageAsync(EntityId entityId, string? sessionId = null)
	{
		ThrowIfDisposed();

		EntityProperties entity = GetEntityProperties(entityId);
		bool requiresSession = entity switch {
			QueueEntityProperties queue => queue.RequiresSession,
			SubscriptionEntityProperties subscription => subscription.RequiresSession,
			_ => false
		};

		if (!requiresSession) {
			await using ServiceBusReceiver nonSessionReceiver = GetReceiver(entity);
			ServiceBusReceivedMessage? nonSessionMessage = await nonSessionReceiver.ReceiveMessageAsync();

			if (nonSessionMessage is null)
				return null;

			await nonSessionReceiver.CompleteMessageAsync(nonSessionMessage);
			return ConvertToReceivedMessage(nonSessionMessage);
		}

		await using ServiceBusSessionReceiver receiver = await GetSessionReceiver(entity, sessionId);
		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync();

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message);
		return ConvertToReceivedMessage(message);
	}

	public async Task SendMessageAsync(EntityId entityId, SendCommand command)
	{
		ThrowIfDisposed();

		EntityProperties entity = GetEntityProperties(entityId);
		await using ServiceBusSender sender = GetSender(entity);
		ServiceBusMessage message = new(command.Body);
		MessageProperties? messageProperties = command.MessageProperties;

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

		foreach (ApplicationProperty property in command.ApplicationProperties) {
			if (!string.IsNullOrWhiteSpace(property.Key))
				message.ApplicationProperties[property.Key] = ConvertApplicationPropertyValue(property.Value, property.Type);
		}

		await sender.SendMessageAsync(message);
	}

	public async Task RefreshEntitiesAsync()
	{
		ThrowIfDisposed();

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

	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;

		_disposed = true;
		_availableEntities.Clear();
		await _client.DisposeAsync();
	}

	private ServiceBusReceiver GetReceiver(EntityProperties entity)
	{
		if (entity is SubscriptionEntityProperties subscription) {
			return _client.CreateReceiver(subscription.TopicName, subscription.Name, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		if (entity is QueueEntityProperties queue) {
			return _client.CreateReceiver(queue.Name, new ServiceBusReceiverOptions {
				ReceiveMode = ServiceBusReceiveMode.PeekLock
			});
		}

		throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.");
	}

	private async Task<ServiceBusSessionReceiver> GetSessionReceiver(EntityProperties entity, string? sessionId)
	{
		if (entity is SubscriptionEntityProperties subscription) {
			return string.IsNullOrWhiteSpace(sessionId)
				? await _client.AcceptNextSessionAsync(subscription.TopicName, subscription.Name)
				: await _client.AcceptSessionAsync(subscription.TopicName, subscription.Name, sessionId);
		}

		if (entity is QueueEntityProperties queue) {
			return string.IsNullOrWhiteSpace(sessionId)
				? await _client.AcceptNextSessionAsync(queue.Name)
				: await _client.AcceptSessionAsync(queue.Name, sessionId);
		}

		throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.");
	}

	private ServiceBusSender GetSender(EntityProperties entity)
	{
		return entity switch {
			QueueEntityProperties queue => _client.CreateSender(queue.Name),
			SubscriptionEntityProperties subscription => _client.CreateSender(subscription.TopicName),
			TopicEntityProperties topic => _client.CreateSender(topic.Name),
			_ => throw new InvalidOperationException($"Cannot create sender for entity type '{entity.GetType().Name}'.")
		};
	}

	private EntityProperties? FindEntity(EntityId entityId)
	{
		return entityId switch {
			QueueEntityId queueId => _availableEntities.FirstOrDefault(p => p is QueueEntityProperties && p.Name == queueId.Name),
			TopicEntityId topicId => _availableEntities.FirstOrDefault(p => p is TopicEntityProperties && p.Name == topicId.Name),
			SubscriptionEntityId subscriptionId => _availableEntities.FirstOrDefault(p => p is SubscriptionEntityProperties subscription && subscription.Name == subscriptionId.Name && subscription.TopicName == subscriptionId.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}", nameof(entityId))
		};
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
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
