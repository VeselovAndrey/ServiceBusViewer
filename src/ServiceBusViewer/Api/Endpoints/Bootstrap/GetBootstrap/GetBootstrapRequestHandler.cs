namespace ServiceBusViewer.Api.Endpoints.Bootstrap.GetBootstrap;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class GetBootstrapRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToBootstrapResponse(await service.GetBootstrapAsync(context.GetBrowserSessionState())));
	}

	private static BootstrapResponse ToBootstrapResponse(BootstrapResult result)
		=> new BootstrapResponse(
			result.ApplicationVersion,
			ResponseMapping.ToConnectionSettingsResponse(result.Connection),
			result.Viewer is not null
				? ResponseMapping.ToViewerStateResponse(result.Viewer)
				: null,
			result.IsConnected);
}
