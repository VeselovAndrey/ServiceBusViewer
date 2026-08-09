namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Collections.Generic;

/// <summary>
/// Request parameters for retrieving entity details.
/// </summary>
internal sealed record DetailsRequest(
	string Type,
	string Name,
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

		if (request.Type is not null && !IsSupportedEntityType(request.Type.AsSpan().Trim()))
			local["type"] = [$"Unsupported entity type '{request.Type}'."];

		if (request.Type?.Equals("Subscription", StringComparison.OrdinalIgnoreCase) is true
			&& string.IsNullOrWhiteSpace(request.TopicName))
			local[nameof(request.TopicName)] = ["Topic name is required when selecting a subscription."];

		if (local.Count > 0) { errors = local; return false; }

		errors = null;
		return true;
	}

	private static bool IsSupportedEntityType(ReadOnlySpan<char> entityType)
		=> entityType.Equals("Queue", StringComparison.OrdinalIgnoreCase)
		   || entityType.Equals("Topic", StringComparison.OrdinalIgnoreCase)
		   || entityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase);
}
