namespace ServiceBusViewer.Api.Endpoints.Entities;

using ServiceBusViewer.Api.Endpoints.Entities.Details;

internal static class EntitiesGroupBuilder
{
	public static IEndpointRouteBuilder MapEntitiesEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder entitiesGroup = endpoints.MapGroup("/entities");

		entitiesGroup.MapGet("/details", DetailsRequestHandler.HandleAsync);

		return endpoints;
	}
}
