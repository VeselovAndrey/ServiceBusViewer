namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Collections.Generic;

/// <summary>
/// Request parameters for retrieving entity details.
/// </summary>
internal sealed record DetailsRequest(
	string? Type,
	string? Name,
	string? TopicName);

internal static class DetailsRequestValidator
{
	public static bool Validate(DetailsRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.Type))
			local["type"] = ["Entity type is required."];

		if (string.IsNullOrWhiteSpace(request.Name))
			local["name"] = ["Entity name is required."];

		if (local.Count > 0) { errors = local; return false; }

		errors = null;
		return true;
	}
}
