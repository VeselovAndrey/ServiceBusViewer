namespace ServiceBusViewer.Api.Endpoints.Viewer.Receive;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class ReceiveRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, ReceiveRequest request, IViewerMessageService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];
			if (!ReceiveRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? receiveErrors) && receiveErrors is not null) {
				foreach (KeyValuePair<string, string[]> kv in receiveErrors)
					errors[kv.Key] = kv.Value;
			}

			if (errors.Count > 0)
				throw ApiProblemException.Validation(errors);

			ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ReceiveAsync(
				context.GetClientSessionState(),
				RequestValidation.NormalizeOptional(request.ReceiveSessionId));

			return ToReceiveResponse(result);
		});
	}

	private static ReceiveResponse ToReceiveResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

		return new ReceiveResponse(
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
