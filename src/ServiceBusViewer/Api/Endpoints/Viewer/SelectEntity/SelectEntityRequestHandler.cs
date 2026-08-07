namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class SelectEntityRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, SelectEntityRequest request, IViewerEntityService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];

			if (!SelectEntityRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? selectErrors) && selectErrors is not null) {
				foreach (KeyValuePair<string, string[]> kv in selectErrors)
					errors[kv.Key] = kv.Value;
			}

			string? selectedEntityType = RequestValidation.NormalizeOptional(request.SelectedEntityType);
			string? selectedEntityName = RequestValidation.NormalizeOptional(request.SelectedEntityName);
			string? selectedTopicName = RequestValidation.NormalizeOptional(request.SelectedTopicName);

			if (errors.Count > 0)
				throw ApiProblemException.Validation(errors);

			Business.Viewer.Contracts.ServiceBus.EntityId entityId = EntityRequestMapping.CreateEntityId(
				selectedEntityType!,
				selectedEntityName!,
				selectedTopicName,
				nameof(request.SelectedTopicName));

			ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.SelectEntityAsync(
				context.GetClientSessionState(),
				entityId);

			return ToSelectEntityResponse(result);
		});
	}

	private static SelectEntityResponse ToSelectEntityResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
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
