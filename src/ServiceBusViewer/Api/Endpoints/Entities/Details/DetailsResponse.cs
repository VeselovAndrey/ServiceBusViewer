namespace ServiceBusViewer.Api.Endpoints.Entities.Details;

using ServiceBusViewer.Api.Models;

/// <summary>
/// Detailed information about an entity including its properties and rules.
/// </summary>
/// <param name="ServiceBusHostName">Host name of the connected Service Bus.</param>
/// <param name="IsManagementApiAvailable">Whether management API is available.</param>
/// <param name="AvailableEntities">List of available entities.</param>
/// <param name="Type">Entity type (e.g., "Queue", "Topic", "Subscription").</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions, otherwise null.</param>
/// <param name="Properties">Entity properties when available.</param>
internal sealed record DetailsResponse(
	string ServiceBusHostName,
	bool IsManagementApiAvailable,
	IReadOnlyList<EntityId> AvailableEntities,
	string Type,
	string Name,
	string? TopicName,
	EntityProperties? Properties);
