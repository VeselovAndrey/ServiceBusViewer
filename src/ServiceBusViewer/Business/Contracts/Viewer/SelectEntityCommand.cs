namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record SelectEntityCommand(
	string SelectedEntityType,
	string SelectedEntityName,
	string? SelectedTopicName);
