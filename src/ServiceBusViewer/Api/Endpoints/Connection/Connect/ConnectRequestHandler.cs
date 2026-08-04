namespace ServiceBusViewer.Api.Endpoints.Connection.Connect;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class ConnectRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, ConnectRequest request, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];
			ConnectRequestValidator.Validate(request, errors);
			string? connectionString = RequestValidation.NormalizeOptional(request.ConnectionString);
			string? rootConnectionString = RequestValidation.NormalizeOptional(request.RootConnectionString);
			string? queueOrTopicName = RequestValidation.NormalizeOptional(request.QueueOrTopicName);
			string? subscriptionName = RequestValidation.NormalizeOptional(request.SubscriptionName);
			RequestValidation.ThrowIfAny(errors);

			ViewerState result = await service.ConnectAsync(
				context.GetBrowserSessionState(),
				new ConnectCommand(connectionString!, rootConnectionString, queueOrTopicName, subscriptionName));

			return ToConnectResponse(result);
		});
	}

	private static ConnectResponse ToConnectResponse(ViewerState state)
	{
		Models.ViewerState response = ResponseMapping.ToViewerStateResponse(state);

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
