using Azure.Messaging.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

namespace ServiceBusViewer.Infrastructure.ServiceBus;

/// <summary>Provides methods to interact with Azure Service Bus, including connecting, sending, receiving, and peeking messages.</summary>
public class ServiceBusService
{
	const int _maxMessagesToPeek = 100;

	private ServiceBusClient? _client;

	/// <summary>Gets a value indicating whether the service is connected to a Service Bus instance.</summary>
	public bool Connected { get; private set; }

	/// <summary>Gets the host name of the connected Service Bus instance.</summary>
	public string Host { get; private set; } = string.Empty;

	/// <summary>Gets the entity name (queue or topic) currently connected to.</summary>
	public string EntityName { get; private set; } = string.Empty;

	/// <summary>Gets the subscription name if connected to a topic subscription; otherwise, null.</summary>
	public string? SubscriptionName { get; private set; }

	/// <summary>Connects to the specified Service Bus entity.</summary>
	/// <param name="connectionString">The Service Bus connection string.</param>
	/// <param name="entityName">The queue or topic name.</param>
	/// <param name="subscriptionName">The subscription name, or null for queues.</param>
	/// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
	public void ConnectTo(string connectionString, string entityName, string? subscriptionName)
	{
		if (Connected)
			throw new InvalidOperationException("Already connected to a Service Bus instance.");

		_client = new ServiceBusClient(connectionString);

		Host = GetServiceBusHost(connectionString);
		EntityName = entityName;
		SubscriptionName = subscriptionName;
		Connected = true;
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
		Host = string.Empty;
		EntityName = string.Empty;
		SubscriptionName = null;
		Connected = false;
	}

	/// <summary>Peeks a batch of messages from the connected Service Bus entity.</summary>
	/// <param name="maxMessages">The maximum number of messages to peek.</param>
	/// <returns>A list of <see cref="PeekedMessageInfo"/> representing the peeked messages.</returns>
	public async Task<List<PeekedMessageInfo>> PeekMessagesAsync(int maxMessages = _maxMessagesToPeek)
	{
		await using var receiver = GetReceiver();
		var messages = await receiver.PeekMessagesAsync(maxMessages);

		return messages.Select(m => new PeekedMessageInfo(m.MessageId, m.EnqueuedTime))
			.ToList();
	}

	/// <summary>Peeks a single message by its message ID.</summary>
	/// <param name="messageId">The ID of the message to peek.</param>
	/// <returns>The <see cref="MessageDetails"/> if found; otherwise, null.</returns>
	public async Task<MessageDetails?> PeekMessageAsync(string messageId)
	{
		await using var receiver = GetReceiver();
		var messages = await receiver.PeekMessagesAsync(_maxMessagesToPeek);

		var targetMessage = messages.FirstOrDefault(m => m.MessageId == messageId);
		if (targetMessage is null)
			return null;

		return new MessageDetails(
			targetMessage.MessageId,
			targetMessage.Body.ToString(),
			targetMessage.ContentType ?? "text/plain",
			targetMessage.EnqueuedTime,
			targetMessage.ApplicationProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
	}

	/// <summary>Receives and completes a single message from the connected Service Bus entity.</summary>
	/// <returns>The <see cref="MessageDetails"/> of the received message, or null if no message is available.</returns>
	public async Task<MessageDetails?> ReceiveMessageAsync()
	{
		await using var receiver = GetReceiver();

		var message = await receiver.ReceiveMessageAsync(TimeSpan.FromMilliseconds(100));

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
		await using var sender = GetSender();

		var message = new ServiceBusMessage(content) {
			ContentType = contentType,
			MessageId = Guid.NewGuid().ToString()
		};

		foreach (var property in properties) {
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

		var endIndex = connectionString.IndexOf(';', connectionStringPrefix.Length);

		var span = connectionString.AsSpan();
		var host = span.Slice(connectionStringPrefix.Length, endIndex - connectionStringPrefix.Length);
		host = host.TrimEnd('/');

		return host.ToString();
	}
}
