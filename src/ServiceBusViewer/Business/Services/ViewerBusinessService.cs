namespace ServiceBusViewer.Business.Services;

using System.Reflection;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.ServiceBus;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.ClientSession;

/// <summary>Coordinates business operations for a single browser session.</summary>
internal sealed class ViewerBusinessService : IViewerBusinessService
{
	private static readonly string _applicationVersion = GetApplicationVersion();

	public async Task<BootstrapResult> GetBootstrapAsync(ClientSessionState state)
	{
		await state.Gate.WaitAsync();

		try {
			return ToBootstrapResult(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> ConnectAsync(ClientSessionState state, ConnectCommand command)
	{
		await state.Gate.WaitAsync();

		try {
			if (state.IsConnected)
				throw new ApiProblemException(StatusCodes.Status409Conflict, "Already connected", "Already connected to a Service Bus instance.");

			IServiceBusSessionConnectionService connection = new ServiceBusSessionConnectionService();

			try {
				if (command.RootConnectionString is not null) {
					await connection.ConnectToAsync(command.ConnectionString, command.RootConnectionString);
				}
				else {
					connection.ConnectTo(
						command.ConnectionString,
						command.QueueOrTopicName ?? throw new InvalidOperationException("Queue or topic name is required."),
						command.SubscriptionName);
				}
			}
			catch {
				await connection.DisposeAsync();
				throw;
			}

			state.ServiceBusConnection = connection;
			state.Connection = new StoredConnectionSettings(
				command.ConnectionString,
				command.RootConnectionString,
				command.RootConnectionString is null ? command.QueueOrTopicName : null,
				command.RootConnectionString is null ? command.SubscriptionName : null);
			state.DisplayedMessage = null;
			state.SendResultMessage = null;
			state.ReceiveSessionId = null;
			state.CurrentMessages = await connection.PeekMessagesAsync();

			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<BootstrapResult> DisconnectAsync(ClientSessionState state)
	{
		await state.Gate.WaitAsync();

		try {
			await state.ResetConnectionAsync();
			return ToBootstrapResult(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> GetViewerAsync(ClientSessionState state)
	{
		await state.Gate.WaitAsync();

		try {
			EnsureConnected(state);
			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> RefreshAsync(ClientSessionState state)
	{
		await state.Gate.WaitAsync();

		try {
			IServiceBusSessionConnectionService connection = EnsureConnected(state);
			state.DisplayedMessage = null;
			state.SendResultMessage = null;
			state.CurrentMessages = await connection.PeekMessagesAsync();
			if (!RequiresSession(connection.ActiveEntity))
				state.ReceiveSessionId = null;

			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> SelectEntityAsync(ClientSessionState state, SelectEntityCommand command)
	{
		await state.Gate.WaitAsync();

		try {
			IServiceBusSessionConnectionService connection = EnsureConnected(state);
			EntityId entity = CreateEntityId(command.SelectedEntityType, command.SelectedEntityName, command.SelectedTopicName, "SelectedTopicName");
			connection.SwitchActiveEntity(entity);

			state.DisplayedMessage = null;
			state.SendResultMessage = null;
			state.ReceiveSessionId = null;
			state.CurrentMessages = await connection.PeekMessagesAsync();

			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> ReceiveAsync(ClientSessionState state, ReceiveCommand command)
	{
		await state.Gate.WaitAsync();

		try {
			IServiceBusSessionConnectionService connection = EnsureConnected(state);
			bool requiresSession = RequiresSession(connection.ActiveEntity);

			state.ReceiveSessionId = requiresSession ? command.ReceiveSessionId : null;
			state.DisplayedMessage = requiresSession
				? await connection.ReceiveSessionMessageAsync(command.ReceiveSessionId)
				: await connection.ReceiveMessageAsync();
			state.SendResultMessage = null;
			state.CurrentMessages = await connection.PeekMessagesAsync();

			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<ViewerState> SendAsync(ClientSessionState state, SendCommand command)
	{
		await state.Gate.WaitAsync();

		try {
			IServiceBusSessionConnectionService connection = EnsureConnected(state);

			await connection.SendMessageAsync(command.Body, command.MessageProperties, command.ApplicationProperties);

			state.DisplayedMessage = null;
			state.SendResultMessage = BuildSendResultMessage(command.MessageProperties.ContentType, command.MessageProperties.MessageId);
			state.CurrentMessages = await connection.PeekMessagesAsync();
			if (!RequiresSession(connection.ActiveEntity))
				state.ReceiveSessionId = null;

			return ToViewerState(state);
		}
		finally {
			state.Gate.Release();
		}
	}

	public async Task<EntityDetailsResult> GetEntityDetailsAsync(ClientSessionState state, EntityDetailsQuery query)
	{
		await state.Gate.WaitAsync();

		try {
			IServiceBusSessionConnectionService connection = EnsureConnected(state);
			EntityId entity = CreateEntityId(query.Type, query.Name, query.TopicName, "TopicName");
			EntityProperties properties = connection.GetEntityProperties(entity);

			return new EntityDetailsResult(
				connection.Host,
				connection.IsManagementApiAvailable,
				connection.AvailableEntities.Select(ToEntityId).ToList(),
				NormalizeEntityType(query.Type),
				query.Name,
				query.TopicName,
				properties);
		}
		finally {
			state.Gate.Release();
		}
	}

	private static BootstrapResult ToBootstrapResult(ClientSessionState state)
	{
		ConnectionSettings connection = new(
			state.Connection.ConnectionString,
			state.Connection.RootConnectionString,
			state.Connection.QueueOrTopicName,
			state.Connection.SubscriptionName);

		return new BootstrapResult(
			_applicationVersion,
			connection,
			state.IsConnected ? ToViewerState(state) : null,
			state.IsConnected);
	}

	private static ViewerState ToViewerState(ClientSessionState state)
	{
		IServiceBusSessionConnectionService connection = state.ServiceBusConnection
			?? throw new InvalidOperationException("Service Bus is not connected.");

		return new ViewerState(
			connection.Host,
			connection.ActiveEntity?.Name,
			(connection.ActiveEntity as SubscriptionEntityProperties)?.TopicName,
			RequiresSession(connection.ActiveEntity),
			connection.IsManagementApiAvailable,
			connection.AvailableEntities.Select(ToEntityId).ToList(),
			state.CurrentMessages.Messages,
			state.CurrentMessages.HasMore,
			state.DisplayedMessage,
			state.SendResultMessage,
			RequiresSession(connection.ActiveEntity) ? state.ReceiveSessionId : null);
	}

	private static EntityId ToEntityId(EntityProperties entity) => entity switch {
		QueueEntityProperties queue => new QueueEntityId(queue.Name),
		TopicEntityProperties topic => new TopicEntityId(topic.Name),
		SubscriptionEntityProperties subscription => new SubscriptionEntityId(subscription.Name, subscription.TopicName),
		_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
	};

	private static EntityId CreateEntityId(string selectedEntityType, string selectedEntityName, string? selectedTopicName, string topicNameField)
	{
		return NormalizeEntityType(selectedEntityType) switch {
			"Queue" => new QueueEntityId(selectedEntityName),
			"Topic" => new TopicEntityId(selectedEntityName),
			"Subscription" when selectedTopicName is not null => new SubscriptionEntityId(selectedEntityName, selectedTopicName),
			"Subscription" => throw ApiProblemException.Validation(new Dictionary<string, string[]> {
				[topicNameField] = ["Topic name is required when selecting a subscription."]
			}),
			_ => throw new ApiProblemException(StatusCodes.Status400BadRequest, "Invalid entity type", $"Unsupported entity type '{selectedEntityType}'.")
		};
	}

	private static string NormalizeEntityType(string selectedEntityType)
	{
		return selectedEntityType.Trim().ToLowerInvariant() switch {
			"queue" => "Queue",
			"topic" => "Topic",
			"subscription" => "Subscription",
			_ => throw new ApiProblemException(StatusCodes.Status400BadRequest, "Invalid entity type", $"Unsupported entity type '{selectedEntityType}'.")
		};
	}

	private static bool RequiresSession(EntityProperties? entity) => entity switch {
		QueueEntityProperties queue => queue.RequiresSession,
		SubscriptionEntityProperties subscription => subscription.RequiresSession,
		_ => false
	};

	private static IServiceBusSessionConnectionService EnsureConnected(ClientSessionState state)
	{
		return state.ServiceBusConnection
			?? throw new ApiProblemException(StatusCodes.Status409Conflict, "Not connected", "Not connected to any Service Bus instance.");
	}

	private static string BuildSendResultMessage(string? sentContentType, string? sentMessageId)
	{
		string contentTypeDescription = string.IsNullOrWhiteSpace(sentContentType)
			? "without a content type"
			: $"with content type '{sentContentType}'";

		return string.IsNullOrWhiteSpace(sentMessageId)
			? $"Message sent successfully {contentTypeDescription}."
			: $"Message '{sentMessageId}' sent successfully {contentTypeDescription}.";
	}

	private static string GetApplicationVersion()
	{
		Assembly assembly = typeof(ViewerBusinessService).Assembly;
		return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "unknown";
	}
}
