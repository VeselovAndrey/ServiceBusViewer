namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;

public class ConnectModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;

	[BindProperty(SupportsGet = true)]
	public string? ConnectionString { get; set; } = Environment.GetEnvironmentVariable("CONNECTION_STRING")
		?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

	[BindProperty(SupportsGet = true)]
	public string? RootConnectionString { get; set; } = Environment.GetEnvironmentVariable("ROOT_CONNECTION_STRING");

	[BindProperty(SupportsGet = true)]
	public string? EntityName { get; set; }

	[BindProperty(SupportsGet = true)]
	public string? SubscriptionName { get; set; }

	public IActionResult OnGet()
	{
		if (_serviceBusService.Connected) {
			return RedirectToPage("/Index");
		}

		return Page();
	}

	public async Task<IActionResult> OnPostConnect()
	{
		if (string.IsNullOrWhiteSpace(ConnectionString)) {
			ModelState.AddModelError(string.Empty, "Connection string is required.");
			return Page();
		}

		if (string.IsNullOrWhiteSpace(RootConnectionString) && string.IsNullOrWhiteSpace(EntityName)) {
			ModelState.AddModelError(string.Empty, "Queue/Topic name is required when not using root connection.");
			return Page();
		}

		try {
			await _serviceBusService.ConnectToAsync(ConnectionString, RootConnectionString, EntityName, SubscriptionName);
			return RedirectToPage("/Index");
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, $"Connection failed: {ex.Message}");
		}

		return Page();
	}
}
