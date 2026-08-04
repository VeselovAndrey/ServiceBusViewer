namespace ServiceBusViewer.Api.Endpoints.Viewer.GetViewer;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class GetViewerRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToGetViewerResponse(await service.GetViewerAsync(context.GetBrowserSessionState())));
	}

	private static GetViewerResponse ToGetViewerResponse(Business.Contracts.Viewer.ViewerState state)
	{
		Models.ViewerState response = ResponseMapping.ToViewerStateResponse(state);

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
