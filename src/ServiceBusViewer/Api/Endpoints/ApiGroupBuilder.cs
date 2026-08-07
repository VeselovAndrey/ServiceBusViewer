namespace ServiceBusViewer.Api.Endpoints;

using ServiceBusViewer.Api.Endpoints.Connection;
using ServiceBusViewer.Api.Endpoints.Entities;
using ServiceBusViewer.Api.Endpoints.SessionState;
using ServiceBusViewer.Api.Endpoints.Viewer;

internal static class ApiGroupBuilder
{
	public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder api = endpoints.MapGroup("/api");

		api.MapSessionStateEndpoints();
		api.MapConnectionEndpoints();
		api.MapViewerEndpoints();
		api.MapEntitiesEndpoints();

		return endpoints;
	}
}
