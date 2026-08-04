namespace ServiceBusViewer.Api.Endpoints.Connection.Disconnect;

using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;
using ServiceBusViewer.Api.Endpoints;

internal static class DisconnectRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToDisconnectResponse(await service.DisconnectAsync(context.GetBrowserSessionState())));
	}

	private static DisconnectResponse ToDisconnectResponse(BootstrapResult result)
	{
		return new DisconnectResponse(
			result.ApplicationVersion,
			ResponseMapping.ToConnectionSettingsResponse(result.Connection),
			result.Viewer is null ? null : ResponseMapping.ToViewerStateResponse(result.Viewer),
			result.IsConnected);
	}
}
