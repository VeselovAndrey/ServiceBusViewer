namespace ServiceBusViewer.Infrastructure.ServiceBus;

using System.Globalization;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Manages a single browser-session-scoped Service Bus connection and entity cache.</summary>
internal sealed class ServiceBusConnection : IServiceBusConnection
{
	private static readonly TimeSpan _emptyReceiveWaitTime = TimeSpan.FromSeconds(1);

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
		ObjectDisposedException.ThrowIf(_disposed, this);

		EntityProperties? properties = FindEntity(entityId);

		return properties
			?? throw new InvalidOperationException($"Entity properties for '{entityId.Name}' not found in cache. Please refresh the entity list.");
	}

	public async Task<ReceivedMessageList> PeekMessagesAsync(EntityId entityId, int maxMessages, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		EntityProperties entity = GetEntityProperties(entityId);

		if (entity is TopicEntityProperties)
			return ReceivedMessageList.Empty;

		await using ServiceBusReceiver receiver = GetReceiver(entity);
		IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(maxMessages + 1, cancellationToken: cancellationToken);
		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;
		return new ReceivedMessageList([.. messagesToReturn.Select(ConvertToReceivedMessage)], hasMore);
	}

	public async Task<ReceivedMessage?> ReceiveMessageAsync(EntityId entityId, string? sessionId, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		EntityProperties entity = GetEntityProperties(entityId);

		return entity.RequiresSession
			? await ReceiveSessionMessageAsync(entity, sessionId, cancellationToken)
			: await ReceiveNonSessionMessageAsync(entity, cancellationToken);
	}

	public async Task SendMessageAsync(EntityId entityId, SendCommand command, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

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

		await sender.SendMessageAsync(message, cancellationToken);
	}

	public async Task RefreshEntitiesAsync(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (_adminClient is null)
			throw new InvalidOperationException("Entity list refresh is only available when management access is available.");

		_availableEntities.Clear();

		await foreach (QueueProperties queue in _adminClient.GetQueuesAsync(cancellationToken)) {
			_availableEntities.Add(new QueueEntityProperties(
				queue.Name,
				queue.RequiresSession,
				queue.LockDuration,
				queue.MaxDeliveryCount, queue.DefaultMessageTimeToLive, queue.RequiresDuplicateDetection, queue.DuplicateDetectionHistoryTimeWindow, queue.DeadLetteringOnMessageExpiration, queue.EnableBatchedOperations, queue.EnablePartitioning, queue.AutoDeleteOnIdle));
		}

		await foreach (TopicProperties topic in _adminClient.GetTopicsAsync(cancellationToken)) {
			_availableEntities.Add(new TopicEntityProperties(
				topic.Name,
				topic.DefaultMessageTimeToLive,
				topic.RequiresDuplicateDetection,
				topic.DuplicateDetectionHistoryTimeWindow,
				topic.EnableBatchedOperations,
				topic.EnablePartitioning,
				topic.AutoDeleteOnIdle));

			await foreach (SubscriptionProperties subscription in _adminClient.GetSubscriptionsAsync(topic.Name, cancellationToken)) {
				List<SubscriptionFilterRule> rules = [];

				await foreach (RuleProperties rule in _adminClient.GetRulesAsync(topic.Name, subscription.SubscriptionName, cancellationToken)) {
					rules.Add(ConvertToSubscriptionRule(rule));
				}

				_availableEntities.Add(new SubscriptionEntityProperties(
					subscription.SubscriptionName,
					subscription.TopicName,
					subscription.RequiresSession,
					subscription.LockDuration,
					subscription.MaxDeliveryCount,
					subscription.DefaultMessageTimeToLive, subscription.DeadLetteringOnMessageExpiration, subscription.EnableBatchedOperations, subscription.AutoDeleteOnIdle, rules));
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
		=> entity switch {
			SubscriptionEntityProperties subscription => _client.CreateReceiver(subscription.TopicName, subscription.Name, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock }),
			QueueEntityProperties queue => _client.CreateReceiver(queue.Name, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock }),
			_ => throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.")
		};

	private async Task<ReceivedMessage?> ReceiveNonSessionMessageAsync(EntityProperties entity, CancellationToken cancellationToken)
	{
		await using ServiceBusReceiver receiver = GetReceiver(entity);
		ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(_emptyReceiveWaitTime, cancellationToken);

		if (message is null)
			return null;

		await receiver.CompleteMessageAsync(message, cancellationToken);

		return ConvertToReceivedMessage(message);
	}

	private async Task<ReceivedMessage?> ReceiveSessionMessageAsync(EntityProperties entity, string? sessionId, CancellationToken cancellationToken)
	{
		ServiceBusSessionReceiver? receiver = await TryGetSessionReceiverAsync(entity, sessionId, cancellationToken);

		if (receiver is null)
			return null;

		await using (receiver) {
			ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(_emptyReceiveWaitTime, cancellationToken);

			if (message is null)
				return null;

			await receiver.CompleteMessageAsync(message, cancellationToken);

			return ConvertToReceivedMessage(message);
		}
	}

	private async Task<ServiceBusSessionReceiver?> TryGetSessionReceiverAsync(EntityProperties entity, string? sessionId, CancellationToken cancellationToken)
	{
		using CancellationTokenSource timeoutCancellationSource = new(_emptyReceiveWaitTime);
		using CancellationTokenSource linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationSource.Token);

		try {
			return entity switch {
				SubscriptionEntityProperties subscription => string.IsNullOrWhiteSpace(sessionId)
					? await _client.AcceptNextSessionAsync(subscription.TopicName, subscription.Name, cancellationToken: linkedCancellationSource.Token)
					: await _client.AcceptSessionAsync(subscription.TopicName, subscription.Name, sessionId, cancellationToken: linkedCancellationSource.Token),
				QueueEntityProperties queue => string.IsNullOrWhiteSpace(sessionId)
					? await _client.AcceptNextSessionAsync(queue.Name, cancellationToken: linkedCancellationSource.Token)
					: await _client.AcceptSessionAsync(queue.Name, sessionId, cancellationToken: linkedCancellationSource.Token),
				_ => throw new InvalidOperationException("Topics do not support receiving messages directly. Please select a subscription.")
			};
		}
		catch (OperationCanceledException) when (timeoutCancellationSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
			return null;
		}
		catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.ServiceTimeout && !cancellationToken.IsCancellationRequested) {
			return null;
		}
	}

	private ServiceBusSender GetSender(EntityProperties entity)
		=> entity switch {
			QueueEntityProperties queue => _client.CreateSender(queue.Name),
			SubscriptionEntityProperties subscription => _client.CreateSender(subscription.TopicName),
			TopicEntityProperties topic => _client.CreateSender(topic.Name),
			_ => throw new InvalidOperationException($"Cannot create sender for entity type '{entity.GetType().Name}'.")
		};

	private EntityProperties? FindEntity(EntityId entityId)
		=> entityId switch {
			QueueEntityId queueId => _availableEntities.FirstOrDefault(p => p is QueueEntityProperties && p.Name == queueId.Name),
			TopicEntityId topicId => _availableEntities.FirstOrDefault(p => p is TopicEntityProperties && p.Name == topicId.Name),
			SubscriptionEntityId subscriptionId => _availableEntities.FirstOrDefault(p => p is SubscriptionEntityProperties subscription && subscription.Name == subscriptionId.Name && subscription.TopicName == subscriptionId.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entityId.GetType().FullName}", nameof(entityId))
		};

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
		return rule.Filter switch {
			SqlRuleFilter sqlFilter => ConvertSqlRule(rule, sqlFilter),
			CorrelationRuleFilter correlationFilter => ConvertCorrelationRule(rule, correlationFilter),
			_ => ConvertUnknownRule(rule)
		};

		static SqlSubscriptionFilterRule ConvertSqlRule(RuleProperties source, SqlRuleFilter filter)
		{
			string? actionExpression = source.Action is SqlRuleAction sqlAction
				? sqlAction.SqlExpression
				: null;

			return new SqlSubscriptionFilterRule(source.Name, filter.SqlExpression, actionExpression);
		}

		static CorrelationSubscriptionFilterRule ConvertCorrelationRule(RuleProperties source, CorrelationRuleFilter filter)
		{
			string? actionExpression = source.Action is SqlRuleAction sqlAction
				? sqlAction.SqlExpression
				: null;

			IReadOnlyDictionary<string, object> applicationProperties = filter.ApplicationProperties
				.ToDictionary(static pair => pair.Key, static pair => pair.Value);

			return new CorrelationSubscriptionFilterRule(
				source.Name,
				filter.CorrelationId,
				filter.MessageId,
				filter.To,
				filter.ReplyTo,
				filter.Subject,
				filter.SessionId,
				filter.ReplyToSessionId,
				filter.ContentType,
				applicationProperties,
				actionExpression);
		}

		static UnknownSubscriptionFilterRule ConvertUnknownRule(RuleProperties source)
			=> new UnknownSubscriptionFilterRule(source.Name, source.Filter?.GetType().Name ?? "null", "Unknown filter type");
	}
}
