namespace ServiceBusViewer.Pages.Shared;

using ServiceBusViewer.Infrastructure.ServiceBus.Models;

public sealed record EntityListViewModel(
	IReadOnlyList<EntityInfo> AvailableEntities,
	string? EntityName,
	string? SubscriptionName,
	bool ManagementApiEnabled);
