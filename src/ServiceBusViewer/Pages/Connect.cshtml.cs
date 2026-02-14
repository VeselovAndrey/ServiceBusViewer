namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;

[ValidateAntiForgeryToken]
public class ConnectModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;

	/// <summary>Gets or sets the connection string for the Service Bus namespace.</summary>
	[BindProperty]
	public string? ConnectionString { get; set; } = Environment.GetEnvironmentVariable("CONNECTION_STRING")
		?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

	/// <summary>Gets or sets the root connection string with management API access for entity discovery.</summary>
	[BindProperty]
	public string? RootConnectionString { get; set; } = Environment.GetEnvironmentVariable("ROOT_CONNECTION_STRING");

	/// <summary>Gets or sets the name of the queue or topic to connect to.</summary>
	[BindProperty]
	public string? QueueOrTopicName { get; set; }

	/// <summary>Gets or sets the subscription name for topic subscriptions.</summary>
	[BindProperty]
	public string? SubscriptionName { get; set; }

	/// <summary>Handles the GET request to display the connection page.</summary>
	/// <returns>The connection page if not connected; otherwise redirects to the Index page.</returns>
	public IActionResult OnGet()
	{
		if (_serviceBusService.Connected) {
			return RedirectToPage("/Index");
		}

		return Page();
	}

	/// <summary>Handles the connect action to establish a Service Bus connection.</summary>
	/// <returns>The Index page if connection succeeds; otherwise the connection page with error details.</returns>
	public async Task<IActionResult> OnPostConnect()
	{
		if (string.IsNullOrWhiteSpace(ConnectionString)) {
			ModelState.AddModelError(string.Empty, "Connection string is required.");
			return Page();
		}

		if (string.IsNullOrWhiteSpace(RootConnectionString) && string.IsNullOrWhiteSpace(QueueOrTopicName)) {
			ModelState.AddModelError(string.Empty, "Queue/Topic name is required when not using root connection.");
			return Page();
		}

		try {
			if (!string.IsNullOrWhiteSpace(RootConnectionString))
				await _serviceBusService.ConnectToAsync(ConnectionString, RootConnectionString);
			else
				_serviceBusService.ConnectTo(ConnectionString, QueueOrTopicName!, SubscriptionName);

			return RedirectToPage("/Index");
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Connection failed: {ex.Message}");
		}

		return Page();
	}
}
