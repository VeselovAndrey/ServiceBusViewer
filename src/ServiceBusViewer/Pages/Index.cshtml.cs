using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

namespace ServiceBusViewer.Pages;

public enum MessageDisplayType
{
	None = 0,
	Peeked,
	Received
}

public record MessageProperty(string Key, string Value);

public record ConnectionInfo(string Host, string Entity, string? Subscription);

public class IndexModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;

	[BindProperty(SupportsGet = true)]
	public string? ConnectionString { get; set; } = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"; // Use development connection string by default

	[BindProperty(SupportsGet = true)]
	public string? EntityName { get; set; }

	[BindProperty(SupportsGet = true)]
	public string? SubscriptionName { get; set; }

	public bool IsConnected => _serviceBusService.Connected;

	public ConnectionInfo? Connection { get; set; }

	[BindProperty]
	public string? MessageId { get; set; }

	public IList<PeekedMessageInfo> Messages { get; set; } = new List<PeekedMessageInfo>();

	public MessageDisplayType DisplayType { get; set; } = MessageDisplayType.None;
	public MessageDetails? DisplayedMessage { get; set; }

	[BindProperty]
	public string? SendMessageBody { get; set; }

	[BindProperty]
	public IList<MessageProperty> SendMessageProperties { get; set; } = new List<MessageProperty>();

	public string? SendResultMessage { get; set; }

	public void OnGet()
	{
		FillHeaderValue();
	}

	public async Task<IActionResult> OnPostConnect()
	{
		if (string.IsNullOrWhiteSpace(ConnectionString) || string.IsNullOrWhiteSpace(EntityName)) {
			ModelState.AddModelError(string.Empty, "Connection string and Queue/Topic name are required.");
			return Page();
		}

		try {
			_serviceBusService.ConnectTo(ConnectionString, EntityName, SubscriptionName);

			Messages = await _serviceBusService.PeekMessagesAsync();
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Connection failed: {ex.Message}");
		}

		FillHeaderValue();
		return Page();
	}

	public async Task<IActionResult> OnPostDisconnect()
	{
		await _serviceBusService.DisconnectAsync();

		FillHeaderValue();
		return Page();
	}

	public async Task<IActionResult> OnPostRefresh()
	{
		try {
			Messages = await _serviceBusService.PeekMessagesAsync();
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Refresh failed: {ex.Message}");
		}

		FillHeaderValue();
		return Page();
	}

	public async Task<IActionResult> OnPostPeek(string messageId)
	{
		if (string.IsNullOrEmpty(MessageId)) {
			ModelState.AddModelError(string.Empty, "Message ID is required to peek.");
			return Page();
		}

		try {
			DisplayedMessage = await _serviceBusService.PeekMessageAsync(messageId);
			DisplayType = MessageDisplayType.Peeked;

			Messages = await _serviceBusService.PeekMessagesAsync();
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Peek failed: {ex.Message}");
		}

		FillHeaderValue();
		return Page();
	}

	public async Task<IActionResult> OnPostReceive()
	{
		try {
			DisplayedMessage = await _serviceBusService.ReceiveMessageAsync();
			DisplayType = MessageDisplayType.Received;

			Messages = await _serviceBusService.PeekMessagesAsync();
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Receive failed: {ex.Message}");
		}

		FillHeaderValue();
		return Page();
	}

	public async Task<IActionResult> OnPostSend()
	{
		if (string.IsNullOrWhiteSpace(SendMessageBody)) {
			ModelState.AddModelError(string.Empty, "Message body cannot be empty.");
			Messages = await _serviceBusService.PeekMessagesAsync();

			return Page();
		}

		try {
			Dictionary<string, object> properties = SendMessageProperties.ToDictionary(x => x.Key, x => (object)x.Value);

			await _serviceBusService.SendMessageAsync(SendMessageBody, "application/json", properties);
			SendResultMessage = "Message sent successfully!";
			SendMessageBody = string.Empty;
			SendMessageProperties.Clear();

			Messages = await _serviceBusService.PeekMessagesAsync();
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Send failed: {ex.Message}");
		}

		FillHeaderValue();
		return Page();
	}

	private void FillHeaderValue()
	{
		Connection = _serviceBusService.Connected
			? new ConnectionInfo(_serviceBusService.Host, _serviceBusService.EntityName, _serviceBusService.SubscriptionName)
			: null;
	}
}
