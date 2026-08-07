namespace ServiceBusViewer.Api.Endpoints.Viewer.Refresh;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class RefreshRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, IViewerMessageService service)
	{
		return EndpointExecution.ExecuteAsync(async () =>
			ToRefreshResponse(await service.RefreshAsync(context.GetClientSessionState())));
	}

	private static RefreshResponse ToRefreshResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

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
