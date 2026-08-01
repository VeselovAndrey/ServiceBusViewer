namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

[ValidateAntiForgeryToken]
public class IndexModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;
	private const int MaxSystemPropertyLength = 128;

	/// <summary>Gets or sets the message body to send.</summary>
	[BindProperty]
	public string? SendMessageBody { get; set; }

	/// <summary>Gets or sets the system properties for the message being sent.</summary>
	[BindProperty]
	public MessageProperties SendMessageProperties { get; set; } = new();

	/// <summary>Gets or sets the application properties for the message being sent.</summary>
	[BindProperty]
	public List<ApplicationProperty> SendMessageApplicationProperties { get; set; } = new List<ApplicationProperty>();

	/// <summary>Gets or sets the session ID for receiving session-enabled messages.</summary>
	[BindProperty]
	public string? ReceiveSessionId { get; set; }

	/// <summary>Gets or sets the name of the currently selected entity (queue or subscription).</summary>
	public string? EntityName { get; set; }

	/// <summary>Gets or sets the name of the topic for subscription entities.</summary>
	public string? TopicName { get; set; }

	/// <summary>Gets a value indicating whether the currently selected entity requires sessions.</summary>
	public bool RequiresSession { get; set; }

	/// <summary>Gets a value indicating whether the Service Bus Management API is available.</summary>
	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	/// <summary>Gets or sets the hostname of the Service Bus namespace.</summary>
	public string ServiceBusHostName { get; set; } = string.Empty;

	/// <summary>Gets or sets the list of available Service Bus entities (queues, topics, subscriptions).</summary>
	public IReadOnlyList<EntityId> AvailableEntities { get; set; } = [];

	/// <summary>Gets or sets the list of messages from the currently selected entity.</summary>
	public IReadOnlyList<ReceivedMessage> Messages { get; set; } = [];

	/// <summary>Gets or sets a value indicating whether there are more messages available to retrieve.</summary>
	public bool HasMoreMessages { get; set; }

	/// <summary>Gets or sets the currently displayed message.</summary>
	public ReceivedMessage? DisplayedMessage { get; set; }

	/// <summary>Gets or sets the result message from the last send operation.</summary>
	public string? SendResultMessage { get; set; }

	/// <summary>Handles the GET request to load initial data and messages.</summary>
	/// <returns>The page if connected; otherwise redirects to the Connect page.</returns>
	public async Task<IActionResult> OnGet()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		try {
			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Refresh failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	/// <summary>Handles the disconnect action, clearing the current Service Bus connection.</summary>
	/// <returns>Redirects to the Connect page.</returns>
	public async Task<IActionResult> OnPostDisconnect()
	{
		await _serviceBusService.DisconnectAsync();
		return RedirectToPage("/Connect");
	}

	/// <summary>Handles the selection of a different Service Bus entity.</summary>
	/// <param name="selectedEntityType">The type of entity (Queue, Topic, or Subscription).</param>
	/// <param name="selectedEntityName">The name of the selected entity.</param>
	/// <param name="selectedTopicName">The topic name for subscription entities.</param>
	/// <returns>The page with updated messages from the selected entity.</returns>
	public async Task<IActionResult> OnPostSelectEntity(string selectedEntityType, string selectedEntityName, string? selectedTopicName)
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		if (string.IsNullOrWhiteSpace(selectedEntityName)) {
			ModelState.AddModelError(string.Empty, "Entity name is required.");
			FillPageModel();
			return Page();
		}

		try {
			EntityId entityInfo = selectedEntityType switch {
				"Subscription" => new SubscriptionEntityId(selectedEntityName, selectedTopicName!),
				"Topic" => new TopicEntityId(selectedEntityName),
				_ => new QueueEntityId(selectedEntityName)
			};

			_serviceBusService.SwitchActiveEntity(entityInfo);

			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Entity selection failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	/// <summary>Handles the refresh action to reload messages from the current entity.</summary>
	/// <returns>The page with refreshed messages.</returns>
	public async Task<IActionResult> OnPostRefresh()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		try {
			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Refresh failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	/// <summary>Handles the receive action to retrieve and display a message from the current entity.</summary>
	/// <returns>The page with the received message displayed.</returns>
	public async Task<IActionResult> OnPostReceive()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		try {
			bool requiresSession = GetRequiresSession();
			DisplayedMessage = requiresSession
				? await _serviceBusService.ReceiveSessionMessageAsync(ReceiveSessionId)
				: await _serviceBusService.ReceiveMessageAsync();

			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Receive failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	/// <summary>Handles the send action to send a message to the current entity.</summary>
	/// <returns>The page with a result message indicating success or failure.</returns>
	public async Task<IActionResult> OnPostSend()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		if (string.IsNullOrWhiteSpace(SendMessageBody)) {
			ModelState.AddModelError(string.Empty, "Message body cannot be empty.");
			FillPageModel();
			return Page();
		}

		try {
			if (!ValidateSystemProperties()) {
				FillPageModel();
				return Page();
			}

			string? sentContentType = SendMessageProperties.ContentType;
			string? sentMessageId = SendMessageProperties.MessageId;
			await _serviceBusService.SendMessageAsync(SendMessageBody, SendMessageProperties, SendMessageApplicationProperties);
			SendResultMessage = BuildSendResultMessage(sentContentType, sentMessageId);
			SendMessageBody = string.Empty;
			SendMessageApplicationProperties.Clear();
			SendMessageProperties = new MessageProperties();

			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Send failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	private void FillPageModel()
	{
		ServiceBusHostName = _serviceBusService.Host;
		EntityName = _serviceBusService.ActiveEntity?.Name;
		TopicName = (_serviceBusService.ActiveEntity as SubscriptionEntityProperties)?.TopicName;
		AvailableEntities = ConvertToEntityIdList(_serviceBusService.AvailableEntities);
		RequiresSession = GetRequiresSession();
		if (!RequiresSession)
			ReceiveSessionId = string.Empty;
	}

	private bool GetRequiresSession()
	{
		return _serviceBusService.ActiveEntity switch {
			SubscriptionEntityProperties subscription => subscription.RequiresSession,
			QueueEntityProperties queue => queue.RequiresSession,
			_ => false
		};
	}

	private bool ValidateSystemProperties()
	{
		bool isValid = true;
		isValid &= ValidateSystemPropertyLength(SendMessageProperties.MessageId, nameof(SendMessageProperties.MessageId), "Message ID");
		isValid &= ValidateSystemPropertyLength(SendMessageProperties.SessionId, nameof(SendMessageProperties.SessionId), "Session ID");
		isValid &= ValidateSystemPropertyLength(SendMessageProperties.CorrelationId, nameof(SendMessageProperties.CorrelationId), "Correlation ID");
		return isValid;
	}

	private bool ValidateSystemPropertyLength(string? value, string propertyName, string displayName)
	{
		if (string.IsNullOrWhiteSpace(value) || value.Length <= MaxSystemPropertyLength)
			return true;

		ModelState.AddModelError($"{nameof(SendMessageProperties)}.{propertyName}", $"The {displayName} must be {MaxSystemPropertyLength} characters or fewer.");
		return false;
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

	private static IReadOnlyList<EntityId> ConvertToEntityIdList(IReadOnlyList<EntityProperties> properties)
	{
		return properties.Select(p => (EntityId)(p switch {
			QueueEntityProperties queue => new QueueEntityId(queue.Name),
			TopicEntityProperties topic => new TopicEntityId(topic.Name),
			SubscriptionEntityProperties subscription => new SubscriptionEntityId(subscription.Name, subscription.TopicName),
			_ => throw new ArgumentException($"Unknown entity properties type: {p.GetType().FullName}")
		})).ToList();
	}
}
