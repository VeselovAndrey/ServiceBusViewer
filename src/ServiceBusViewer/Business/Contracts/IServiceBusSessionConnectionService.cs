namespace ServiceBusViewer.Business.Contracts;

using ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Defines the Service Bus connection operations used by the viewer business layer.</summary>
internal interface IServiceBusSessionConnectionService : IAsyncDisposable
{
	bool Connected { get; }

	string Host { get; }

	EntityProperties? ActiveEntity { get; }

	bool IsManagementApiAvailable { get; }

	IReadOnlyList<EntityProperties> AvailableEntities { get; }

	Task ConnectToAsync(string connectionString, string rootConnectionString);

	void ConnectTo(string connectionString, string queueOrTopicName, string? subscriptionName);

	EntityProperties GetEntityProperties(EntityId entityId);

	Task DisconnectAsync();

	Task<ReceivedMessageList> PeekMessagesAsync(int maxMessages = 50);

	Task<ReceivedMessage?> ReceiveMessageAsync();

	Task<ReceivedMessage?> ReceiveSessionMessageAsync(string? sessionId);

	Task SendMessageAsync(string content, MessageProperties? messageProperties, IReadOnlyList<ApplicationProperty> applicationProperties);

	Task UpdateAvailableEntitiesListAsync();

	void SwitchActiveEntity(EntityId entity);
}
