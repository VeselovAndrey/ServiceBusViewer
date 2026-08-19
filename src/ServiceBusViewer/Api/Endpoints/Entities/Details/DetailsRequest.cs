namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Collections.Generic;

/// <summary>Request parameters for retrieving entity details.</summary>
/// <param name="Type">The type of the entity.</param>
/// <param name="Name">The name of the entity.</param>
/// <param name="TopicName">The name of the topic, if applicable.</param>
public sealed record DetailsRequest(
	string Type,
	string Name,
	string? TopicName);

/// <summary>Validator for <see cref="DetailsRequest"/>.</summary>
internal static class DetailsRequestValidator
{
	/// <summary>Validates a <see cref="DetailsRequest"/> instance.</summary>
	/// <param name="request">The <see cref="DetailsRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary to receive validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(DetailsRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.Type))
			local["type"] = ["Entity type is required."];

		if (string.IsNullOrWhiteSpace(request.Name))
			local["name"] = ["Entity name is required."];

		if (request.Type is not null && !IsSupportedEntityType(request.Type.AsSpan().Trim()))
			local["type"] = [$"Unsupported entity type '{request.Type}'."];

		if (request.Type?.Equals("Subscription", StringComparison.OrdinalIgnoreCase) is true && string.IsNullOrWhiteSpace(request.TopicName))
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
