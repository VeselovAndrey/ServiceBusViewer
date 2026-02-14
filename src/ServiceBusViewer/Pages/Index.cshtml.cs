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

	[BindProperty]
	public string? SendMessageBody { get; set; }

	[BindProperty]
	public MessageProperties SendMessageProperties { get; set; } = new();

	[BindProperty]
	public List<ApplicationProperty> SendMessageApplicationProperties { get; set; } = new List<ApplicationProperty>();

	[BindProperty]
	public string? ReceiveSessionId { get; set; }

	public string? EntityName { get; set; }

	public string? TopicName { get; set; }

	public bool RequiresSession { get; set; }

	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	public string ServiceBusHostName { get; set; } = string.Empty;

	public IReadOnlyList<EntityId> AvailableEntities { get; set; } = [];

	public IReadOnlyList<ReceivedMessage> Messages { get; set; } = [];

	public bool HasMoreMessages { get; set; }

	public ReceivedMessage? DisplayedMessage { get; set; }

	public string? SendResultMessage { get; set; }

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

	public async Task<IActionResult> OnPostDisconnect()
	{
		await _serviceBusService.DisconnectAsync();
		return RedirectToPage("/Connect");
	}

	public async Task<IActionResult> OnPostSelectEntity(string selectedEntityType, string selectedEntityName, string? selectedTopicName)
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		if (string.IsNullOrWhiteSpace(selectedEntityName)) {
			ModelState.AddModelError(string.Empty, "Entity name is required.");
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

	public async Task<IActionResult> OnPostSend()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		if (string.IsNullOrWhiteSpace(SendMessageBody)) {
			ModelState.AddModelError(string.Empty, "Message body cannot be empty.");

			AvailableEntities = ConvertToEntityIdList(_serviceBusService.AvailableEntities);

			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;

			return Page();
		}

		try {
			if (!ValidateSystemProperties()) {
				ReceivedMessageList invalidResult = await _serviceBusService.PeekMessagesAsync();
				Messages = invalidResult.Messages;
				HasMoreMessages = invalidResult.HasMore;

				FillPageModel();
				return Page();
			}


			await _serviceBusService.SendMessageAsync(SendMessageBody, SendMessageProperties, SendMessageApplicationProperties);
			SendResultMessage = "Message sent successfully!";
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
		EntityName = _serviceBusService.EntityName;
		TopicName = _serviceBusService.TopicName;
		AvailableEntities = ConvertToEntityIdList(_serviceBusService.AvailableEntities);
		RequiresSession = GetRequiresSession();
		if (!RequiresSession)
			ReceiveSessionId = string.Empty;
	}

	private bool GetRequiresSession()
	{
		if (string.IsNullOrWhiteSpace(_serviceBusService.EntityName))
			return false;

		if (!string.IsNullOrWhiteSpace(_serviceBusService.TopicName)) {
			SubscriptionEntityProperties? subscription = _serviceBusService.AvailableEntities
				.OfType<SubscriptionEntityProperties>()
				.FirstOrDefault(s => s.Name == _serviceBusService.EntityName && s.TopicName == _serviceBusService.TopicName);

			return subscription?.RequiresSession ?? false;
		}

		QueueEntityProperties? queue = _serviceBusService.AvailableEntities
			.OfType<QueueEntityProperties>()
			.FirstOrDefault(q => q.Name == _serviceBusService.EntityName);

		return queue?.RequiresSession ?? false;
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
