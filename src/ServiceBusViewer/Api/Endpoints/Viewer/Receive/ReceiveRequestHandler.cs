namespace ServiceBusViewer.Api.Endpoints.Viewer.Receive;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class ReceiveRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, ReceiveRequest request, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];
			if (!ReceiveRequestValidator.Validate(request, out var receiveErrors) && receiveErrors is not null) {
				foreach (var kv in receiveErrors)
					errors[kv.Key] = kv.Value;
			}
			if (errors.Count > 0)
				throw ApiProblemException.Validation(errors);

			Business.Contracts.Viewer.ViewerState result = await service.ReceiveAsync(
				context.GetClientSessionState(),
				new ReceiveCommand(RequestValidation.NormalizeOptional(request.ReceiveSessionId)));

			return ToReceiveResponse(result);
		});
	}

	private static ReceiveResponse ToReceiveResponse(Business.Contracts.Viewer.ViewerState state)
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
