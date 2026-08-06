namespace ServiceBusViewer.Api.Endpoints.Viewer.GetViewer;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class GetViewerRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToGetViewerResponse(await service.GetViewerAsync(context.GetClientSessionState())));
	}

	private static GetViewerResponse ToGetViewerResponse(Business.Contracts.Viewer.ViewerState state)
	{
		ViewerState response = state.ToApiModel();

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
