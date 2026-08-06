namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class SelectEntityRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, SelectEntityRequest request, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];

			if (!SelectEntityRequestValidator.Validate(request, out var selectErrors) && selectErrors is not null) {
				foreach (var kv in selectErrors)
					errors[kv.Key] = kv.Value;
			}

			string? selectedEntityType = RequestValidation.NormalizeOptional(request.SelectedEntityType);
			string? selectedEntityName = RequestValidation.NormalizeOptional(request.SelectedEntityName);
			string? selectedTopicName = RequestValidation.NormalizeOptional(request.SelectedTopicName);

			if (errors.Count > 0)
				throw ApiProblemException.Validation(errors);

			Business.Contracts.Viewer.ViewerState result = await service.SelectEntityAsync(
				context.GetClientSessionState(),
				new SelectEntityCommand(selectedEntityType!, selectedEntityName!, selectedTopicName));

			return ToSelectEntityResponse(result);
		});
	}

	private static SelectEntityResponse ToSelectEntityResponse(Business.Contracts.Viewer.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

		return new SelectEntityResponse(
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
