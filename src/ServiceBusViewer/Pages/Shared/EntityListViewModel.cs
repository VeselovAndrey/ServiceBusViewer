namespace ServiceBusViewer.Pages.Shared;

using ServiceBusViewer.Infrastructure.ServiceBus.Models;

public sealed record EntityListViewModel(
	IReadOnlyList<EntityId> AvailableEntities,
	string? EntityName,
	string? TopicName, // The parent Topic name for Subscription
	bool DisplayPropertyLinks);
