namespace ServiceBusViewer.Infrastructure.ServiceBus;

using System.Globalization;
using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Manages a single browser-session-scoped Service Bus connection and entity cache.</summary>
internal sealed class ServiceBusConnection : IServiceBusConnection
{
	private static readonly TimeSpan _emptyReceiveWaitTime = TimeSpan.FromSeconds(1);

	// ReceiveMessagesAsync requires a positive wait. Use the same bounded wait as ordinary receives so the initial dead-letter receive has enough time to return messages.
	private static readonly TimeSpan _deadLetterReceiveWaitTime = TimeSpan.FromSeconds(1);

	private readonly ServiceBusClient _client;
	private readonly ServiceBusAdministrationClient? _adminClient;
	private readonly bool _usesDevelopmentEmulator;
	private readonly List<EntityProperties> _availableEntities = [];
	private bool _disposed;

	public ServiceBusConnection(
		ServiceBusClient client,
		ServiceBusAdministrationClient? adminClient,
		string namespaceHost,
		bool usesDevelopmentEmulator = false,
		IEnumerable<EntityProperties>? availableEntities = null)
	{
		_client = client;
		_adminClient = adminClient;
		_usesDevelopmentEmulator = usesDevelopmentEmulator;
		NamespaceHost = namespaceHost;

		if (availableEntities is not null)
			_availableEntities.AddRange(availableEntities);
	}

	public string NamespaceHost { get; }

	public bool IsManagementApiAvailable => _adminClient is not null;

	public bool UsesDevelopmentEmulator => _usesDevelopmentEmulator;

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
		long? totalCount = await GetRuntimeMessageCountAsync(entity, deadLetter: false, cancellationToken);

		// Sanity fallback: a reported total below the number of messages actually peeked is unreliable (the emulator always reports zero counts).
		if (totalCount < messages.Count)
			totalCount = null;

		return new ReceivedMessageList([.. messagesToReturn.Select(ConvertToReceivedMessage)], hasMore, totalCount);
	}

	public async Task<ReceivedMessageList> PeekDeadLetterMessagesAsync(EntityId entityId, int maxMessages, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		EntityProperties entity = GetEntityProperties(entityId);

		if (entity is TopicEntityProperties)
			return ReceivedMessageList.Empty;

		await using ServiceBusReceiver receiver = GetDeadLetterReceiver(entity);
		IReadOnlyList<ServiceBusReceivedMessage> messages = await receiver.PeekMessagesAsync(maxMessages + 1, cancellationToken: cancellationToken);
		bool hasMore = messages.Count > maxMessages;
		IEnumerable<ServiceBusReceivedMessage> messagesToReturn = hasMore ? messages.Take(maxMessages) : messages;
		long? totalCount = await GetRuntimeMessageCountAsync(entity, deadLetter: true, cancellationToken);

		// Sanity fallback: a reported total below the number of messages actually peeked is unreliable (the emulator always reports zero counts).
		if (totalCount < messages.Count)
			totalCount = null;

		return new ReceivedMessageList([.. messagesToReturn.Select(ConvertToReceivedMessage)], hasMore, totalCount);
	}

	// The message count comes from the management runtime properties when management access is available; any failure falls back to an unknown count.
	private async Task<long?> GetRuntimeMessageCountAsync(EntityProperties entity, bool deadLetter, CancellationToken cancellationToken)
	{
		if (_adminClient is null)
			return null;

		try {
			long? count = entity switch {
				QueueEntityProperties queue => await GetQueueCountAsync(queue.Name),
				SubscriptionEntityProperties subscription => await GetSubscriptionCountAsync(subscription.TopicName, subscription.Name),
				_ => null
			};

			return count;

			async Task<long> GetQueueCountAsync(string queueName)
			{
				QueueRuntimeProperties properties = await _adminClient.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);

				return deadLetter ? properties.DeadLetterMessageCount : properties.ActiveMessageCount;
			}

			async Task<long> GetSubscriptionCountAsync(string topicName, string subscriptionName)
			{
				SubscriptionRuntimeProperties properties = await _adminClient.GetSubscriptionRuntimePropertiesAsync(topicName, subscriptionName, cancellationToken);

				return deadLetter ? properties.DeadLetterMessageCount : properties.ActiveMessageCount;
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception) {
			return null;
		}
	}

	// Best-effort batched runtime count retrieval: any failure leaves the counts unknown (null) instead of failing entity discovery.
	// The emulator reports zero counts even for non-empty entities, so its all-zero results are treated as unavailable.
	private async Task<Dictionary<string, EntityMessageCounts>> GetRuntimeQueueCountsAsync(CancellationToken cancellationToken)
	{
		Dictionary<string, EntityMessageCounts> counts = new(StringComparer.Ordinal);

		try {
			await foreach (QueueRuntimeProperties properties in _adminClient!.GetQueuesRuntimePropertiesAsync(cancellationToken)) {
				EntityMessageCounts entityCounts = new(properties.ActiveMessageCount, properties.ScheduledMessageCount, properties.DeadLetterMessageCount);

				if (_usesDevelopmentEmulator && entityCounts is { ActiveMessageCount: 0, ScheduledMessageCount: 0, DeadLetterMessageCount: 0 })
					continue;

				counts[properties.Name] = entityCounts;
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception) {
			counts.Clear();
		}

		return counts;
	}

	// Best-effort batched runtime count retrieval: any failure leaves the counts unknown (null) instead of failing entity discovery.
	// The emulator reports zero counts even for non-empty entities, so its all-zero results are treated as unavailable.
	private async Task<Dictionary<string, EntityMessageCounts>> GetRuntimeSubscriptionCountsAsync(string topicName, CancellationToken cancellationToken)
	{
		Dictionary<string, EntityMessageCounts> counts = new(StringComparer.Ordinal);

		try {
			await foreach (SubscriptionRuntimeProperties properties in _adminClient!.GetSubscriptionsRuntimePropertiesAsync(topicName, cancellationToken)) {
				// Subscriptions expose no scheduled count: scheduled messages reside at the topic level until their enqueue time.
				EntityMessageCounts entityCounts = new(properties.ActiveMessageCount, 0, properties.DeadLetterMessageCount);

				if (_usesDevelopmentEmulator && entityCounts is { ActiveMessageCount: 0, DeadLetterMessageCount: 0 })
					continue;

				counts[properties.SubscriptionName] = entityCounts;
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception) {
			counts.Clear();
		}

		return counts;
	}

	public async Task RefreshEntityMessageCountsAsync(EntityId entityId, CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		EntityProperties entity = GetEntityProperties(entityId);

		switch (entity) {
			case QueueEntityProperties queue:
				UpdateMessageCounts(entity, await GetQueueRuntimeCountsAsync(queue.Name, cancellationToken));
				break;
			case SubscriptionEntityProperties subscription:
				UpdateMessageCounts(entity, await GetSubscriptionRuntimeCountsAsync(subscription.TopicName, subscription.Name, cancellationToken));
				break;
			case TopicEntityProperties topic:
				// A topic holds no directly consumable messages: refresh the counts of its subscriptions instead.
				Dictionary<string, EntityMessageCounts> subscriptionCounts = await GetRuntimeSubscriptionCountsAsync(topic.Name, cancellationToken);

				foreach (SubscriptionEntityProperties subscription in _availableEntities.OfType<SubscriptionEntityProperties>().Where(s => s.TopicName == topic.Name).ToList())
					UpdateMessageCounts(subscription, subscriptionCounts.TryGetValue(subscription.Name, out EntityMessageCounts counts) ? counts : null);
				break;
		}
	}

	// Best-effort single-entity runtime count retrieval: any failure leaves the counts unknown (null).
	// The emulator reports zero counts even for non-empty entities, so its all-zero results are treated as unavailable.
	private async Task<EntityMessageCounts?> GetQueueRuntimeCountsAsync(string queueName, CancellationToken cancellationToken)
	{
		if (_adminClient is null)
			return null;

		try {
			QueueRuntimeProperties properties = await _adminClient.GetQueueRuntimePropertiesAsync(queueName, cancellationToken);
			EntityMessageCounts counts = new(properties.ActiveMessageCount, properties.ScheduledMessageCount, properties.DeadLetterMessageCount);

			if (_usesDevelopmentEmulator && counts is { ActiveMessageCount: 0, ScheduledMessageCount: 0, DeadLetterMessageCount: 0 })
				return null;

			return counts;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception) {
			return null;
		}
	}

	// Best-effort single-entity runtime count retrieval: any failure leaves the counts unknown (null).
	// Subscriptions expose no scheduled count: scheduled messages reside at the topic level until their enqueue time.
	// The emulator reports zero counts even for non-empty entities, so its all-zero results are treated as unavailable.
	private async Task<EntityMessageCounts?> GetSubscriptionRuntimeCountsAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
	{
		if (_adminClient is null)
			return null;

		try {
			SubscriptionRuntimeProperties properties = await _adminClient.GetSubscriptionRuntimePropertiesAsync(topicName, subscriptionName, cancellationToken);
			EntityMessageCounts counts = new(properties.ActiveMessageCount, 0, properties.DeadLetterMessageCount);

			if (_usesDevelopmentEmulator && counts is { ActiveMessageCount: 0, DeadLetterMessageCount: 0 })
				return null;

			return counts;
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception) {
			return null;
		}
	}

	// Records are immutable: replace the cached entry with a copy carrying the refreshed counts.
	private void UpdateMessageCounts(EntityProperties entity, EntityMessageCounts? counts)
	{
		int index = _availableEntities.IndexOf(entity);

		if (index >= 0)
			_availableEntities[index] = entity with { MessageCounts = counts };
	}

	public async Task<DeadLetterOperationResult> RepublishDeadLetterMessagesAsync(
		EntityId entityId,
		IReadOnlyCollection<string>? messageIds,
		RepublishMessageIdStrategy strategy,
		string? manualMessageId,
		int? maxMessages,
		CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (maxMessages <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxMessages), maxMessages, "The message bound must be a positive integer when set.");

		EntityProperties entity = GetEntityProperties(entityId);
		bool isBulk = messageIds is null;
		HashSet<string> requestedMessageIds = [];

		if (messageIds is not null)
			requestedMessageIds = [.. messageIds.Distinct(StringComparer.Ordinal)];
		HashSet<string> seenMessageIds = [];
		List<string> failureDetails = [];
		int affectedCount = 0;
		int failedCount = 0;

		await using ServiceBusReceiver receiver = GetDeadLetterReceiver(entity);
		await using ServiceBusSender sender = GetSender(entity);

		while (true) {
			IReadOnlyList<ServiceBusReceivedMessage> batch = await receiver.ReceiveMessagesAsync(10, maxWaitTime: _deadLetterReceiveWaitTime, cancellationToken: cancellationToken);
			int unseenRequestedCount = 0;

			foreach (ServiceBusReceivedMessage message in batch) {
				bool isUnseen = seenMessageIds.Add(message.MessageId);
				bool isRequested = isBulk || requestedMessageIds.Contains(message.MessageId);

				if (isRequested && isUnseen && !IsBulkBoundReached(isBulk, maxMessages, affectedCount, failedCount)) {
					unseenRequestedCount++;
					(bool succeeded, string? failureDetail) = await RepublishMessageAsync(receiver, sender, message, strategy, manualMessageId, cancellationToken);

					if (succeeded) {
						affectedCount++;
					}
					else {
						failedCount++;

						if (failureDetail is not null)
							failureDetails.Add(failureDetail);
					}
				}
				else {
					// A message this operation does not republish, one already attempted in this operation, or one beyond the bound: return it to visible state in the dead-letter queue.
					await receiver.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);
				}
			}

			if (batch.Count == 0 || unseenRequestedCount == 0 || IsBulkBoundReached(isBulk, maxMessages, affectedCount, failedCount))
				break;
		}

		DeadLetterOperationResult result = BuildDeadLetterOperationResult(isBulk, "republished", requestedMessageIds, affectedCount, failedCount, failureDetails);

		return result;
	}

	public async Task<DeadLetterOperationResult> DropDeadLetterMessagesAsync(
		EntityId entityId,
		IReadOnlyCollection<string>? messageIds,
		int? maxMessages,
		CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if (maxMessages <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxMessages), maxMessages, "The message bound must be a positive integer when set.");

		EntityProperties entity = GetEntityProperties(entityId);
		bool isBulk = messageIds is null;
		HashSet<string> requestedMessageIds = [];

		if (messageIds is not null)
			requestedMessageIds = [.. messageIds.Distinct(StringComparer.Ordinal)];
		HashSet<string> seenMessageIds = [];
		List<string> failureDetails = [];
		int affectedCount = 0;
		int failedCount = 0;

		await using ServiceBusReceiver receiver = GetDeadLetterReceiver(entity);

		while (true) {
			IReadOnlyList<ServiceBusReceivedMessage> batch = await receiver.ReceiveMessagesAsync(10, maxWaitTime: _deadLetterReceiveWaitTime, cancellationToken: cancellationToken);
			int unseenRequestedCount = 0;

			foreach (ServiceBusReceivedMessage message in batch) {
				bool isUnseen = seenMessageIds.Add(message.MessageId);
				bool isRequested = isBulk || requestedMessageIds.Contains(message.MessageId);

				if (isRequested && isUnseen && !IsBulkBoundReached(isBulk, maxMessages, affectedCount, failedCount)) {
					unseenRequestedCount++;
					(bool succeeded, string? failureDetail) = await DropDeadLetterMessageAsync(receiver, message, cancellationToken);

					if (succeeded) {
						affectedCount++;
					}
					else {
						failedCount++;

						if (failureDetail is not null)
							failureDetails.Add(failureDetail);
					}
				}
				else {
					// A message this operation does not drop, one already attempted in this operation, or one beyond the bound: return it to visible state in the dead-letter queue.
					await receiver.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);
				}
			}

			if (batch.Count == 0 || unseenRequestedCount == 0 || IsBulkBoundReached(isBulk, maxMessages, affectedCount, failedCount))
				break;
		}

		DeadLetterOperationResult result = BuildDeadLetterOperationResult(isBulk, "dropped", requestedMessageIds, affectedCount, failedCount, failureDetails);

		return result;
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
		MessageProperties messageProperties = command.MessageProperties;

		if (!string.IsNullOrWhiteSpace(messageProperties.MessageId))
			message.MessageId = messageProperties.MessageId;

		if (!string.IsNullOrWhiteSpace(messageProperties.ContentType))
			message.ContentType = messageProperties.ContentType;

		if (!string.IsNullOrWhiteSpace(messageProperties.SessionId))
			message.SessionId = messageProperties.SessionId;

		if (!string.IsNullOrWhiteSpace(messageProperties.PartitionKey))
			message.PartitionKey = messageProperties.PartitionKey;

		if (!string.IsNullOrWhiteSpace(messageProperties.CorrelationId))
			message.CorrelationId = messageProperties.CorrelationId;

		if (messageProperties.ScheduledEnqueueTime is not null)
			message.ScheduledEnqueueTime = messageProperties.ScheduledEnqueueTime.Value;

		if (messageProperties.TimeToLive is not null)
			message.TimeToLive = messageProperties.TimeToLive.Value;

		if (!string.IsNullOrWhiteSpace(messageProperties.To))
			message.To = messageProperties.To;

		if (!string.IsNullOrWhiteSpace(messageProperties.ReplyTo))
			message.ReplyTo = messageProperties.ReplyTo;

		if (!string.IsNullOrWhiteSpace(messageProperties.Subject))
			message.Subject = messageProperties.Subject;

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

		Dictionary<string, EntityMessageCounts> queueCounts = await GetRuntimeQueueCountsAsync(cancellationToken);

		await foreach (QueueProperties queue in _adminClient.GetQueuesAsync(cancellationToken)) {
			_availableEntities.Add(new QueueEntityProperties(
				queue.Name,
				queue.RequiresSession,
				queue.LockDuration,
				queue.MaxDeliveryCount, queue.DefaultMessageTimeToLive, queue.RequiresDuplicateDetection, queue.DuplicateDetectionHistoryTimeWindow, queue.DeadLetteringOnMessageExpiration, queue.EnableBatchedOperations, queue.EnablePartitioning, queue.AutoDeleteOnIdle) {
				MessageCounts = queueCounts.TryGetValue(queue.Name, out EntityMessageCounts counts) ? counts : null
			});
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

			Dictionary<string, EntityMessageCounts> subscriptionCounts = await GetRuntimeSubscriptionCountsAsync(topic.Name, cancellationToken);

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
					subscription.DefaultMessageTimeToLive, subscription.DeadLetteringOnMessageExpiration, subscription.EnableBatchedOperations, subscription.AutoDeleteOnIdle, rules) {
					MessageCounts = subscriptionCounts.TryGetValue(subscription.SubscriptionName, out EntityMessageCounts counts) ? counts : null
				});
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

	private ServiceBusReceiver GetDeadLetterReceiver(EntityProperties entity)
		=> entity switch {
			SubscriptionEntityProperties subscription => _client.CreateReceiver(subscription.TopicName, subscription.Name, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock, SubQueue = SubQueue.DeadLetter }),
			QueueEntityProperties queue => _client.CreateReceiver(queue.Name, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock, SubQueue = SubQueue.DeadLetter }),
			_ => throw new InvalidOperationException("Topics do not support dead-letter peeks. Please select a queue or a subscription.")
		};

	private static async Task<(bool Succeeded, string? FailureDetail)> RepublishMessageAsync(
		ServiceBusReceiver receiver,
		ServiceBusSender sender,
		ServiceBusReceivedMessage source,
		RepublishMessageIdStrategy strategy,
		string? manualMessageId,
		CancellationToken cancellationToken)
	{
		string messageIdLabel = source.MessageId ?? "(no message id)";

		try {
			// The copy is built inside the per-message guard so a construction failure fails only this message and keeps bulk operations going.
			ServiceBusMessage copy = BuildRepublishedMessage(source, strategy, manualMessageId);
			await sender.SendMessageAsync(copy, cancellationToken);
			await receiver.CompleteMessageAsync(source, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception exception) {
			// The copy was not delivered and completed: return the message to visible state in the dead-letter queue.
			await receiver.AbandonMessageAsync(source, propertiesToModify: null, cancellationToken);
			return (false, $"Message '{messageIdLabel}': {exception.Message}");
		}

		return (true, null);
	}

	private static async Task<(bool Succeeded, string? FailureDetail)> DropDeadLetterMessageAsync(
		ServiceBusReceiver receiver,
		ServiceBusReceivedMessage message,
		CancellationToken cancellationToken)
	{
		string messageIdLabel = message.MessageId ?? "(no message id)";

		try {
			// Completing a message on the dead-letter receiver permanently deletes it from the $DeadLetterQueue without re-delivering it.
			await receiver.CompleteMessageAsync(message, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		}
		catch (Exception exception) {
			// The permanent delete failed: return the message to visible state in the dead-letter queue.
			await receiver.AbandonMessageAsync(message, propertiesToModify: null, cancellationToken);
			return (false, $"Message '{messageIdLabel}': {exception.Message}");
		}

		return (true, null);
	}

	// The bound applies to bulk operations only and counts every processed message, whether it succeeded or failed.
	private static bool IsBulkBoundReached(bool isBulk, int? maxMessages, int affectedCount, int failedCount)
		=> isBulk && maxMessages is not null && affectedCount + failedCount >= maxMessages.Value;

	private static DeadLetterOperationResult BuildDeadLetterOperationResult(
		bool isBulk,
		string operationVerb,
		HashSet<string> requestedMessageIds,
		int affectedCount,
		int failedCount,
		List<string> failureDetails)
	{
		if (failedCount is 0 && (isBulk || affectedCount == requestedMessageIds.Count))
			return new DeadLetterOperationResult(true, affectedCount, failedCount, null);

		StringBuilder detail = new();

		if (failedCount > 0) {
			detail.Append(CultureInfo.InvariantCulture, $"{failedCount} of the requested message(s) could not be {operationVerb} and remain in the dead-letter queue.");

			if (failureDetails.Count > 0)
				detail.Append(CultureInfo.InvariantCulture, $" First failure: {failureDetails[0]}");
		}

		if (!isBulk && affectedCount < requestedMessageIds.Count) {
			if (detail.Length > 0)
				detail.Append(' ');

			detail.Append(CultureInfo.InvariantCulture, $"Message id(s) were not found in the dead-letter queue: {string.Join(", ", requestedMessageIds)}.");
		}

		return new DeadLetterOperationResult(false, affectedCount, failedCount, detail.Length > 0 ? detail.ToString() : null);
	}

	private static ServiceBusMessage BuildRepublishedMessage(
		ServiceBusReceivedMessage source,
		RepublishMessageIdStrategy strategy,
		string? manualMessageId)
	{
		ServiceBusMessage copy = new(source.Body.ToArray());

		// AutoGenerate leaves the message id unset: the Service Bus client rejects a null or empty id and assigns a fresh one at send time.
		if (strategy is not RepublishMessageIdStrategy.AutoGenerate) {
			copy.MessageId = strategy switch {
				RepublishMessageIdStrategy.KeepOriginal => source.MessageId,
				RepublishMessageIdStrategy.Guid => Guid.NewGuid().ToString(),
				RepublishMessageIdStrategy.GuidV7 => Guid.CreateVersion7().ToString(),
				_ => manualMessageId
			};
		}

		copy.ContentType = source.ContentType;
		copy.Subject = source.Subject;
		copy.To = source.To;
		copy.ReplyTo = source.ReplyTo;
		copy.ReplyToSessionId = source.ReplyToSessionId;
		copy.SessionId = source.SessionId;
		copy.PartitionKey = source.PartitionKey;
		copy.CorrelationId = source.CorrelationId;

		if (source.TimeToLive != TimeSpan.MaxValue)
			copy.TimeToLive = source.TimeToLive;

		if (source.ScheduledEnqueueTime != DateTimeOffset.MinValue)
			copy.ScheduledEnqueueTime = source.ScheduledEnqueueTime;

		foreach (KeyValuePair<string, object> applicationProperty in source.ApplicationProperties)
			copy.ApplicationProperties[applicationProperty.Key] = applicationProperty.Value;

		return copy;
	}

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
			message.DeliveryCount,
			message.EnqueuedTime,
			message.ScheduledEnqueueTime != DateTimeOffset.MinValue ? message.ScheduledEnqueueTime : null,
			message.TimeToLive != TimeSpan.MaxValue ? message.TimeToLive : null,
			message.To,
			message.ReplyTo,
			message.Subject,
			message.DeadLetterReason,
			message.DeadLetterSource,
			message.DeadLetterErrorDescription);

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
				ApplicationPropertyType.ByteArray => throw new FormatException("ByteArray application properties are not supported for sending."),
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
				ApplicationPropertyType.Null => throw new FormatException("Null application properties are not supported for sending."),
				ApplicationPropertyType.Unknown => throw new FormatException("Unknown application property types are not supported for sending."),
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
