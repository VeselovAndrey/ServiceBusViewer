namespace ServiceBusViewer.Api.Endpoints.Connection.Disconnect;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class DisconnectRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToDisconnectResponse(await service.DisconnectAsync(context.GetClientSessionState())));
	}

	private static DisconnectResponse ToDisconnectResponse(BootstrapResult result)
		=> new DisconnectResponse(
			result.ApplicationVersion,
			result.Connection.ToApiModel(),
			result.Viewer?.ToApiModel(),
			result.IsConnected);
}
