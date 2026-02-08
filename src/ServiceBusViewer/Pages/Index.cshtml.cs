namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

public class IndexModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;
	private const int MaxSystemPropertyLength = 128;

	[BindProperty]
	public string? SelectedEntityName { get; set; }

	[BindProperty]
	public string? SelectedEntityType { get; set; }

	[BindProperty]
	public string? SelectedTopicName { get; set; }

	[BindProperty]
	public string? SendMessageBody { get; set; }

	[BindProperty]
	public MessageProperties SendMessageProperties { get; set; } = new();

	[BindProperty]
	public List<ApplicationProperty> SendMessageApplicationProperties { get; set; } = new List<ApplicationProperty>();

	public string? EntityName { get; set; }

	public string? SubscriptionName { get; set; }

	public bool IsConnected => _serviceBusService.Connected;

	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	public string ServiceBusHostName { get; set; } = string.Empty;

	public IReadOnlyList<EntityInfo> AvailableEntities { get; set; } = [];

	public IReadOnlyList<ReceivedMessage> Messages { get; set; } = [];

	public bool HasMoreMessages { get; set; }

	public ReceivedMessage? DisplayedMessage { get; set; }

	public string? SendResultMessage { get; set; }

	public async Task<IActionResult> OnGet()
	{
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		try {
			AvailableEntities = _serviceBusService.AvailableEntities;
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

	public async Task<IActionResult> OnPostSelectEntity()
	{
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		if (string.IsNullOrWhiteSpace(SelectedEntityName)) {
			ModelState.AddModelError(string.Empty, "Entity name is required.");
			return Page();
		}

		try {
			EntityInfo entityInfo = SelectedEntityType switch {
				"Subscription" => new SubscriptionEntityInfo(SelectedEntityName!, SelectedTopicName!),
				"Topic" => new TopicEntityInfo(SelectedEntityName!),
				_ => new QueueEntityInfo(SelectedEntityName!)
			};

			_serviceBusService.SwitchActiveEntity(entityInfo);
			AvailableEntities = _serviceBusService.AvailableEntities;

			// Load messages for the selected entity
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
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		try {
			ReceivedMessageList result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;

			// Maintain entity list
			AvailableEntities = _serviceBusService.AvailableEntities;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Refresh failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	public async Task<IActionResult> OnPostReceive()
	{
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		try {
			DisplayedMessage = await _serviceBusService.ReceiveMessageAsync();

			var result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;

			// Maintain entity list
			AvailableEntities = _serviceBusService.AvailableEntities;
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Receive failed: {ex.Message}");
		}

		FillPageModel();
		return Page();
	}

	public async Task<IActionResult> OnPostSend()
	{
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		if (string.IsNullOrWhiteSpace(SendMessageBody)) {
			ModelState.AddModelError(string.Empty, "Message body cannot be empty.");

			var result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;

			AvailableEntities = _serviceBusService.AvailableEntities;

			return Page();
		}

		try {
			if (!ValidateSystemProperties()) {
				var invalidResult = await _serviceBusService.PeekMessagesAsync();
				Messages = invalidResult.Messages;
				HasMoreMessages = invalidResult.HasMore;

				AvailableEntities = _serviceBusService.AvailableEntities;

				return Page();
			}


			await _serviceBusService.SendMessageAsync(SendMessageBody, SendMessageProperties, SendMessageApplicationProperties);
			SendResultMessage = "Message sent successfully!";
			SendMessageBody = string.Empty;
			SendMessageApplicationProperties.Clear();
			SendMessageProperties = new MessageProperties();

			var result = await _serviceBusService.PeekMessagesAsync();
			Messages = result.Messages;
			HasMoreMessages = result.HasMore;

			// Maintain entity list
			AvailableEntities = _serviceBusService.AvailableEntities;
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
		SubscriptionName = _serviceBusService.SubscriptionName;
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
}
