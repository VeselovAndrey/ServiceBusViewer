namespace ServiceBusViewer.Api.Endpoints.Connection.Disconnect;

using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class DisconnectRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service, IApplicationInfoProvider applicationInfoProvider)
	{
		ViewerConnectionSnapshot viewerConnectionSnapshot = await service.DisconnectAsync(context.GetClientSessionState());

		return TypedResults.Ok(new DisconnectResponse(
				applicationInfoProvider.ApplicationVersion,
				viewerConnectionSnapshot.Connection.ToApiModel(),
				viewerConnectionSnapshot.Viewer?.ToApiModel(),
				viewerConnectionSnapshot.IsConnected));
	}
}
