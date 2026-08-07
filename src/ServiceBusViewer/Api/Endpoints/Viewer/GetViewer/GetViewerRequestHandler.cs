namespace ServiceBusViewer.Api.Endpoints.Viewer.GetViewer;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class GetViewerRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerConnectionService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToGetViewerResponse(await service.GetCurrentAsync(context.GetClientSessionState())));
	}

	private static GetViewerResponse ToGetViewerResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

		return new GetViewerResponse(
			response.ServiceBusHostName,
			response.EntityName,
			response.TopicName,
			response.RequiresSession,
			response.IsManagementApiAvailable,
			response.AvailableEntities,
			response.Messages,
			response.HasMoreMessages,
			response.DisplayedMessage,
			response.SendResultMessage,
			response.ReceiveSessionId);
	}
}
