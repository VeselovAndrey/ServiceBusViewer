namespace ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class GetSessionStateRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service, IApplicationInfoProvider applicationInfoProvider)
	{
		ViewerConnectionSnapshot viewerConnectionSnapshot = await service.GetSnapshotAsync(context.GetClientSessionState());

		return TypedResults.Ok(new SessionStateResponse(
				applicationInfoProvider.ApplicationVersion,
				applicationInfoProvider.IsRunningInContainer,
				viewerConnectionSnapshot.Connection.ToApiModel(),
				viewerConnectionSnapshot.Viewer?.ToApiModel(),
				viewerConnectionSnapshot.IsConnected));
	}
}
