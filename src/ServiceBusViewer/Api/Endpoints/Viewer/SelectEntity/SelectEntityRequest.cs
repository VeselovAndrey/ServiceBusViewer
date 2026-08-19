namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using System.Collections.Generic;

/// <summary>
/// Request to select an entity (queue/topic/subscription) for the viewer.
/// </summary>
/// <param name="SelectedEntityType">Type of entity to select ("Queue", "Topic", "Subscription").</param>
/// <param name="SelectedEntityName">Name of the entity to select.</param>
/// <param name="SelectedTopicName">Topic name when selecting a subscription.</param>
public sealed record SelectEntityRequest(
	string SelectedEntityType,
	string SelectedEntityName,
	string? SelectedTopicName);

/// <summary>Validates a <see cref="SelectEntityRequest"/> instance.</summary>
internal static class SelectEntityRequestValidator
{
	/// <summary>Validates a <see cref="SelectEntityRequest"/> instance.</summary>
	/// <param name="request">The <see cref="SelectEntityRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(SelectEntityRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.SelectedEntityType))
			local[nameof(request.SelectedEntityType)] = ["Selected entity type is required."];

		if (string.IsNullOrWhiteSpace(request.SelectedEntityName))
			local[nameof(request.SelectedEntityName)] = ["Entity name is required."];

		errors = local.Count > 0 ? local : null;

		return errors is null;
	}
}
