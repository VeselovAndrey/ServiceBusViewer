namespace ServiceBusViewer.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBusViewer.Infrastructure.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus.Models;

public class EntityInfoModel(ServiceBusService serviceBusService) : PageModel
{
	private readonly ServiceBusService _serviceBusService = serviceBusService;

	[BindProperty(SupportsGet = true)]
	public string? Type { get; set; }

	[BindProperty(SupportsGet = true)]
	public string? Name { get; set; }

	[BindProperty(SupportsGet = true)]
	public string? TopicName { get; set; }

	public string ServiceBusHostName { get; set; } = string.Empty;

	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	public EntityProperties? Properties { get; private set; }

	public string? ErrorMessage { get; private set; }

	public IReadOnlyList<EntityId> AvailableEntities { get; private set; } = [];

	public async Task<IActionResult> OnGet()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Connect");

		AvailableEntities = ConvertToEntityIdList(_serviceBusService.AvailableEntities);
		ServiceBusHostName = _serviceBusService.Host;

		if (string.IsNullOrWhiteSpace(Type) || string.IsNullOrWhiteSpace(Name)) {
			ErrorMessage = "Entity type and name are required.";
			return Page();
		}

		try {
			EntityId entity = Type.ToLowerInvariant() switch {
				"queue" => new QueueEntityId(Name!),
				"topic" => new TopicEntityId(Name!),
				"subscription" when !string.IsNullOrWhiteSpace(TopicName) => new SubscriptionEntityId(Name!, TopicName!),
				_ => throw new InvalidOperationException("Unsupported entity type or missing topic name for subscription.")
			};

			Properties = _serviceBusService.GetEntityPropertiesAsync(entity);
		}
		catch (Exception ex) {
			ErrorMessage = ex.Message;
		}

		return Page();
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
