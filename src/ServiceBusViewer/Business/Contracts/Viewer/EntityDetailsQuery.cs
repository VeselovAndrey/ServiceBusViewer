namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record EntityDetailsQuery(
	string Type,
	string Name,
	string? TopicName);
