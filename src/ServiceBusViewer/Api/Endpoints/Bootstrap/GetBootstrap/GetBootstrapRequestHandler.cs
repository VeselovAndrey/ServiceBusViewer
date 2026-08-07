namespace ServiceBusViewer.Api.Endpoints.Bootstrap.GetBootstrap;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class GetBootstrapRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToBootstrapResponse(await service.GetBootstrapAsync(context.GetClientSessionState())));
	}

	private static BootstrapResponse ToBootstrapResponse(BootstrapResult result)
		=> new BootstrapResponse(
			result.ApplicationVersion,
			result.Connection.ToApiModel(),
			result.Viewer?.ToApiModel(),
			result.IsConnected);
}
