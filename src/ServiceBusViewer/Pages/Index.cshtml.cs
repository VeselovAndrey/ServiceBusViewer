namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

public enum MessageDisplayType
{
	None = 0,
	Peeked,
	Received
}

public record MessageProperty(string Key, string Value);

public class IndexModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;

	public string? EntityName { get; set; }

	public string? SubscriptionName { get; set; }

	[BindProperty]
	public string? SelectedEntityName { get; set; }

	[BindProperty]
	public string? SelectedEntityType { get; set; }

	[BindProperty]
	public string? SelectedTopicName { get; set; }

	[BindProperty]
	public string? SendMessageBody { get; set; }

	[BindProperty]
	public IList<MessageProperty> SendMessageProperties { get; set; } = new List<MessageProperty>();

	public bool IsConnected => _serviceBusService.Connected;

	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	public string ServiceBusHostName { get; set; } = string.Empty;

	public IReadOnlyList<EntityInfo> AvailableEntities { get; set; } = [];

	public IReadOnlyList<MessageDetails> Messages { get; set; } = [];

	public bool HasMoreMessages { get; set; }

	public MessageDisplayType DisplayType { get; set; } = MessageDisplayType.None;

	public MessageDetails? DisplayedMessage { get; set; }

	public string? SendResultMessage { get; set; }

	public async Task<IActionResult> OnGet()
	{
		if (!_serviceBusService.Connected) {
			return RedirectToPage("/Connect");
		}

		try {
			AvailableEntities = _serviceBusService.AvailableEntities;
			MessageList result = await _serviceBusService.PeekMessagesAsync();
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

			_serviceBusService.SwitchEntity(entityInfo);
			AvailableEntities = _serviceBusService.AvailableEntities;

			// Load messages for the selected entity
			MessageList result = await _serviceBusService.PeekMessagesAsync();
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
			MessageList result = await _serviceBusService.PeekMessagesAsync();
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
			DisplayType = MessageDisplayType.Received;

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
			Dictionary<string, object> properties = SendMessageProperties.ToDictionary(x => x.Key, x => (object)x.Value);

			await _serviceBusService.SendMessageAsync(SendMessageBody, "application/json", properties);
			SendResultMessage = "Message sent successfully!";
			SendMessageBody = string.Empty;
			SendMessageProperties.Clear();

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
}
