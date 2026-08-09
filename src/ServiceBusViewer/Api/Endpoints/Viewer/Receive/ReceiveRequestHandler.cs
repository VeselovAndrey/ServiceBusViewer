namespace ServiceBusViewer.Api.Endpoints.Viewer.Receive;

using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class ReceiveRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, ReceiveRequest request, IViewerMessageService service)
	{
		Dictionary<string, string[]> errors = [];
		if (!ReceiveRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? receiveErrors) && receiveErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in receiveErrors)
				errors[kv.Key] = kv.Value;
		}

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.ReceiveAsync(context.GetClientSessionState(), request.ReceiveSessionId);

		return TypedResults.Ok(ToReceiveResponse(result));
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
