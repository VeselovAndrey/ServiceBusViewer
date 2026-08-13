namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Infrastructure.ClientSession;
using ViewerConnectionSettings = ServiceBusViewer.Business.Viewer.Contracts.ConnectionSettings;

internal static class ConnectRequestHandler
{
	public static async Task<IResult> HandleAsync(
		HttpContext context,
		ConnectRequest request,
		ServiceBusViewer.Business.Viewer.Contracts.IViewerConnectionService service,
		CancellationToken cancellationToken)
	{
		Dictionary<string, string[]> errors = [];

		if (!ConnectRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? connectErrors) && connectErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in connectErrors)
				errors[kv.Key] = kv.Value;
		}

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		var settings = new ViewerConnectionSettings(
			request.ConnectionString,
			request.EmulatorManagementConnectionString,
			request.QueueOrTopicName,
			request.SubscriptionName);

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ConnectAsync(
			context.GetClientSessionState(),
			settings,
			cancellationToken);

		return TypedResults.Ok(ToConnectResponse(result));
	}

	private static ConnectResponse ToConnectResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		ViewerState viewerState = state.ToApiModel();

		return new ConnectResponse(
			viewerState.ServiceBusHostName,
			viewerState.EntityName,
			viewerState.TopicName,
			viewerState.RequiresSession,
			viewerState.IsManagementApiAvailable,
			viewerState.AvailableEntities,
			viewerState.Messages,
			viewerState.HasMoreMessages,
			viewerState.DisplayedMessage,
			viewerState.SendResultMessage,
			viewerState.ReceiveSessionId);
	}
}
