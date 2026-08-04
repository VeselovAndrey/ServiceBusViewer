namespace ServiceBusViewer.Api.Endpoints.Bootstrap;

using ServiceBusViewer.Api.Endpoints.Bootstrap.GetBootstrap;

internal static class BootstrapGroupBuilder
{
	public static IEndpointRouteBuilder MapBootstrapEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder bootstrapGroup = endpoints.MapGroup("/bootstrap");

		bootstrapGroup.MapGet(string.Empty, GetBootstrapRequestHandler.HandleAsync);

		return endpoints;
	}
}
