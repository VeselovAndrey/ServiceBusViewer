namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Infrastructure.ClientSession;
using ViewerConnectionSettings = ServiceBusViewer.Business.Viewer.Contracts.ConnectionSettings;

internal static class ConnectRequestHandler
{
	public static Task<IResult> HandleAsync(
		HttpContext context,
		ConnectRequest request,
		ServiceBusViewer.Business.Viewer.Contracts.IViewerConnectionService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];

			if (!ConnectRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? connectErrors) && connectErrors is not null) {
				foreach (KeyValuePair<string, string[]> kv in connectErrors)
					errors[kv.Key] = kv.Value;
			}

			string? connectionString = RequestValidation.NormalizeOptional(request.ConnectionString);
			string? rootConnectionString = RequestValidation.NormalizeOptional(request.RootConnectionString);
			string? queueOrTopicName = RequestValidation.NormalizeOptional(request.QueueOrTopicName);
			string? subscriptionName = RequestValidation.NormalizeOptional(request.SubscriptionName);

			if (errors.Count > 0)
				throw ApiProblemException.Validation(errors);

			ViewerConnectionSettings settings = rootConnectionString is null
				? new ViewerConnectionSettings(connectionString!, queueOrTopicName!, subscriptionName)
				: new ViewerConnectionSettings(connectionString!, rootConnectionString, queueOrTopicName, subscriptionName);

			ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ConnectAsync(
				context.GetClientSessionState(),
				settings);

			return ToConnectResponse(result);
		});
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
