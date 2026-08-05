namespace ServiceBusViewer.Business.Contracts.Viewer;

/// <summary>
/// Command to change the selected entity in the viewer UI.
/// </summary>
/// <param name="SelectedEntityType">Type of the selected entity (e.g. "Queue", "Topic", "Subscription").</param>
/// <param name="SelectedEntityName">Name of the entity to select.</param>
/// <param name="SelectedTopicName">Parent topic name when selecting a subscription; otherwise null.</param>
internal sealed record SelectEntityCommand(
	string SelectedEntityType,
	string SelectedEntityName,
	string? SelectedTopicName);
