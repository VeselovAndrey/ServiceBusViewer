namespace ServiceBusViewer.Business.Contracts.Viewer;

using ServiceBusViewer.Business.Contracts.ServiceBus;

internal sealed record EntityDetailsResult(
	string ServiceBusHostName,
	bool IsManagementApiAvailable,
	IReadOnlyList<EntityId> AvailableEntities,
	string Type,
	string Name,
	string? TopicName,
	EntityProperties? Properties);
