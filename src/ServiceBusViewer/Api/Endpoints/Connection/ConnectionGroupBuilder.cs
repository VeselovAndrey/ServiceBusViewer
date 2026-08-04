namespace ServiceBusViewer.Api.Endpoints.Connection;

using ServiceBusViewer.Api.Endpoints.Connection.Connect;
using ServiceBusViewer.Api.Endpoints.Connection.Disconnect;

internal static class ConnectionGroupBuilder
{
	public static IEndpointRouteBuilder MapConnectionEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder connectionGroup = endpoints.MapGroup("/connection");

		connectionGroup.MapPost("/connect", ConnectRequestHandler.HandleAsync);
		connectionGroup.MapPost("/disconnect", DisconnectRequestHandler.HandleAsync);

		return endpoints;
	}
}
