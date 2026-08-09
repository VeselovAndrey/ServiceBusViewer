namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Infrastructure.ClientSession;
using ViewerConnectionSettings = ServiceBusViewer.Business.Viewer.Contracts.ConnectionSettings;

internal static class ConnectRequestHandler
{
	public static async Task<IResult> HandleAsync(
		HttpContext context,
		ConnectRequest request,
		ServiceBusViewer.Business.Viewer.Contracts.IViewerConnectionService service)
	{
		Dictionary<string, string[]> errors = [];

		if (!ConnectRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? connectErrors) && connectErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in connectErrors)
				errors[kv.Key] = kv.Value;
		}

		string? connectionString = RequestMapping.NormalizeOptional(request.ConnectionString);
		string? rootConnectionString = RequestMapping.NormalizeOptional(request.RootConnectionString);
		string? queueOrTopicName = RequestMapping.NormalizeOptional(request.QueueOrTopicName);
		string? subscriptionName = RequestMapping.NormalizeOptional(request.SubscriptionName);

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		ViewerConnectionSettings settings = rootConnectionString is null
			? new ViewerConnectionSettings(connectionString!, queueOrTopicName!, subscriptionName)
			: new ViewerConnectionSettings(connectionString!, rootConnectionString, queueOrTopicName, subscriptionName);

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ConnectAsync(
			context.GetClientSessionState(),
			settings);

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
