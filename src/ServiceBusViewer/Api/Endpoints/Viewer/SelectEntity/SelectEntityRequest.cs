namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using System.Collections.Generic;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Request to select an entity (queue/topic/subscription) for the viewer.
/// </summary>
/// <param name="SelectedEntityType">Type of entity to select ("Queue", "Topic", "Subscription").</param>
/// <param name="SelectedEntityName">Name of the entity to select.</param>
/// <param name="SelectedTopicName">Topic name when selecting a subscription.</param>
internal sealed record SelectEntityRequest(
	string? SelectedEntityType,
	string? SelectedEntityName,
	string? SelectedTopicName);

internal static class SelectEntityRequestValidator
{
	public static void Validate(SelectEntityRequest request, IDictionary<string, string[]> errors)
	{
		RequestValidation.Require(request.SelectedEntityType, nameof(request.SelectedEntityType), "Selected entity type is required.", errors);
		RequestValidation.Require(request.SelectedEntityName, nameof(request.SelectedEntityName), "Entity name is required.", errors);
	}
}
