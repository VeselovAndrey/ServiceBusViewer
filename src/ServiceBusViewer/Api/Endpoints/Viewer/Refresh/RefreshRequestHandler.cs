namespace ServiceBusViewer.Api.Endpoints.Viewer.Refresh;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class RefreshRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToRefreshResponse(await service.RefreshAsync(context.GetBrowserSessionState())));
	}

	private static RefreshResponse ToRefreshResponse(Business.Contracts.Viewer.ViewerState state)
	{
		Models.ViewerState response = ResponseMapping.ToViewerStateResponse(state);

		return new RefreshResponse(
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
