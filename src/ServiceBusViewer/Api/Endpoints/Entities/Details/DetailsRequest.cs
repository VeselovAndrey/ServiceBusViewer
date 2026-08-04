namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using System.Collections.Generic;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Request parameters for retrieving entity details.
/// </summary>
internal sealed record DetailsRequest(
	string? Type,
	string? Name,
	string? TopicName);

internal static class DetailsRequestValidator
{
	public static void Validate(DetailsRequest request, IDictionary<string, string[]> errors)
	{
		RequestValidation.Require(request.Type, "type", "Entity type is required.", errors);
		RequestValidation.Require(request.Name, "name", "Entity name is required.", errors);
	}
}
