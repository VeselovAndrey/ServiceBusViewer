namespace ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class GetSessionStateRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service, IApplicationInfoProvider applicationInfoProvider)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			ViewerConnectionSnapshot viewerConnectionSnapshot = await service.GetSnapshotAsync(context.GetClientSessionState());

			return new SessionStateResponse(
				applicationInfoProvider.ApplicationVersion,
				viewerConnectionSnapshot.Connection.ToApiModel(),
				viewerConnectionSnapshot.Viewer?.ToApiModel(),
				viewerConnectionSnapshot.IsConnected);
		});
	}
}
