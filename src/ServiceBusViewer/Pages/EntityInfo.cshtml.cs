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

	[BindProperty]
	public string? SelectedEntityName { get; set; }

	[BindProperty]
	public string? SelectedEntityType { get; set; }

	[BindProperty]
	public string? SelectedTopicName { get; set; }

	public string ServiceBusHostName { get; set; } = string.Empty;

	public bool IsManagementApiAvailable => _serviceBusService.IsManagementApiAvailable;

	public EntityProperties? Properties { get; private set; }

	public string? ErrorMessage { get; private set; }

	public IReadOnlyList<EntityInfo> AvailableEntities { get; private set; } = Array.Empty<EntityInfo>();

	public string? EntityNameForSelection { get; private set; }

	public string? SubscriptionNameForSelection { get; private set; }

	public async Task<IActionResult> OnGet()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Index");

		AvailableEntities = _serviceBusService.AvailableEntities;
		SetSelectedEntityContext();

		if (string.IsNullOrWhiteSpace(Type) || string.IsNullOrWhiteSpace(Name)) {
			ErrorMessage = "Entity type and name are required.";
			return Page();
		}

		try {
			EntityInfo entity = Type.ToLowerInvariant() switch {
				"queue" => new QueueEntityInfo(Name!),
				"topic" => new TopicEntityInfo(Name!),
				"subscription" when !string.IsNullOrWhiteSpace(TopicName) => new SubscriptionEntityInfo(Name!, TopicName!),
				_ => throw new InvalidOperationException("Unsupported entity type or missing topic name for subscription.")
			};

			Properties = await _serviceBusService.GetEntityPropertiesAsync(entity);
		}
		catch (Exception ex) {
			ErrorMessage = ex.Message;
		}

		FillPageModel();

		return Page();
	}

	public IActionResult OnPostSelectEntity()
	{
		if (!_serviceBusService.Connected)
			return RedirectToPage("/Index");

		AvailableEntities = _serviceBusService.AvailableEntities;

		if (string.IsNullOrWhiteSpace(SelectedEntityName)) {
			ModelState.AddModelError(string.Empty, "Entity name is required.");
			Type = SelectedEntityType;
			Name = SelectedEntityName;
			TopicName = SelectedTopicName;
			SetSelectedEntityContext();
			return Page();
		}

		if (string.Equals(SelectedEntityType, "Subscription", StringComparison.OrdinalIgnoreCase)
			&& string.IsNullOrWhiteSpace(SelectedTopicName)) {
			ModelState.AddModelError(string.Empty, "Topic name is required for subscription selection.");
			Type = SelectedEntityType;
			Name = SelectedEntityName;
			TopicName = SelectedTopicName;
			SetSelectedEntityContext();
			return Page();
		}

		try {
			EntityInfo entityInfo = SelectedEntityType switch {
				"Subscription" => new SubscriptionEntityInfo(SelectedEntityName!, SelectedTopicName!),
				"Topic" => new TopicEntityInfo(SelectedEntityName!),
				_ => new QueueEntityInfo(SelectedEntityName!)
			};

			_serviceBusService.SwitchEntity(entityInfo);

			string targetType = SelectedEntityType ?? "Queue";
			string? targetTopicName = SelectedEntityType == "Subscription" ? SelectedTopicName : null;

			return RedirectToPage(new { type = targetType, name = SelectedEntityName, topicName = targetTopicName });
		}
		catch (Exception ex) {
			ModelState.AddModelError(string.Empty, ex.Message);
		}

		Type = SelectedEntityType;
		Name = SelectedEntityName;
		TopicName = SelectedTopicName;

		SetSelectedEntityContext();
		FillPageModel();

		return Page();
	}

	private void SetSelectedEntityContext()
	{
		switch (Type?.ToLowerInvariant()) {
			case "subscription" when !string.IsNullOrWhiteSpace(TopicName):
				EntityNameForSelection = TopicName;
				SubscriptionNameForSelection = Name;
				break;

			default:
				EntityNameForSelection = Name;
				SubscriptionNameForSelection = null;
				break;
		}
	}

	private void FillPageModel()
	{
		ServiceBusHostName = _serviceBusService.Host;
	}
}
