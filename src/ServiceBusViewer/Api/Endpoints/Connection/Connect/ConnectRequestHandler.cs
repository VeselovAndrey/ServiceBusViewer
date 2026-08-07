namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;
using ViewerConnectionSettings = ServiceBusViewer.Business.Viewer.Contracts.ConnectionSettings;

internal static class ConnectRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, ConnectRequest request, IViewerConnectionService service)
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

			ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ConnectAsync(
				context.GetClientSessionState(),
				new ViewerConnectionSettings(connectionString!, rootConnectionString, queueOrTopicName, subscriptionName));

			return ToConnectResponse(result);
		});
	}

	private static ConnectResponse ToConnectResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

		return new ConnectResponse(
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
