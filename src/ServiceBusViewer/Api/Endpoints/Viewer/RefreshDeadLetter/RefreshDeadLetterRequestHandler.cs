namespace ServiceBusViewer.Api.Endpoints.Viewer.RefreshDeadLetter;

using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class RefreshDeadLetterRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, IViewerMessageService service, CancellationToken cancellationToken)
	{
		ViewerState businessState = await service.RefreshDeadLetterMessagesAsync(context.GetClientSessionState(), cancellationToken);

		return TypedResults.Ok(businessState.ToApiModel());
	}
}
