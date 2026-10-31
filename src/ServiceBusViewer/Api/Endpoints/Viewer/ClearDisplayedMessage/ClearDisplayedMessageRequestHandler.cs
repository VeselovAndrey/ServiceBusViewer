namespace ServiceBusViewer.Api.Endpoints.Viewer.ClearDisplayedMessage;

using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class ClearDisplayedMessageRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, IViewerMessageService service, CancellationToken cancellationToken)
	{
		ServiceBusViewer.Business.Viewer.Contracts.ViewerState state = await service.ClearDisplayedMessageAsync(context.GetClientSessionState(), cancellationToken);

		return TypedResults.Ok(ToClearDisplayedMessageResponse(state));
	}

	private static ClearDisplayedMessageResponse ToClearDisplayedMessageResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

		return new ClearDisplayedMessageResponse(
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
			response.ReceiveSessionId,
			response.TotalMessageCount,
			response.UsesDevelopmentEmulator);
	}
}
