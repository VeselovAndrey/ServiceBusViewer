namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using System.Collections.Generic;

/// <summary>
/// Request to select an entity (queue/topic/subscription) for the viewer.
/// </summary>
/// <param name="SelectedEntityType">Type of entity to select ("Queue", "Topic", "Subscription").</param>
/// <param name="SelectedEntityName">Name of the entity to select.</param>
/// <param name="SelectedTopicName">Topic name when selecting a subscription.</param>
internal sealed record SelectEntityRequest(
	string SelectedEntityType,
	string SelectedEntityName,
	string? SelectedTopicName);

internal static class SelectEntityRequestValidator
{
	public static bool Validate(SelectEntityRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.SelectedEntityType))
			local[nameof(request.SelectedEntityType)] = ["Selected entity type is required."];

		if (string.IsNullOrWhiteSpace(request.SelectedEntityName))
			local[nameof(request.SelectedEntityName)] = ["Entity name is required."];

		if (local.Count > 0) { errors = local; return false; }

		errors = null;
		return true;
	}
}
