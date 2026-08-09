namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Collections.Generic;
using ServiceBusViewer.Api.Converters;

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

		string? entityType = RequestMapping.NormalizeOptional(request.Type);
		if (entityType is not null && !IsSupportedEntityType(entityType))
			local["type"] = [$"Unsupported entity type '{entityType}'."];

		if (entityType?.Equals("Subscription", StringComparison.OrdinalIgnoreCase) is true
			&& string.IsNullOrWhiteSpace(request.TopicName))
			local[nameof(request.TopicName)] = ["Topic name is required when selecting a subscription."];

		if (local.Count > 0) { errors = local; return false; }

		errors = null;
		return true;
	}

	private static bool IsSupportedEntityType(string entityType)
		=> entityType.Equals("Queue", StringComparison.OrdinalIgnoreCase)
		   || entityType.Equals("Topic", StringComparison.OrdinalIgnoreCase)
		   || entityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase);
}
