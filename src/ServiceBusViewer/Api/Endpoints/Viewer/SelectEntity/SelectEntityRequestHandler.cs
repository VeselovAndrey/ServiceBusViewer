namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class SelectEntityRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, SelectEntityRequest request, IViewerEntityService service)
	{
		Dictionary<string, string[]> errors = [];

		if (!SelectEntityRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? selectErrors) && selectErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in selectErrors)
				errors[kv.Key] = kv.Value;
		}

		bool isSupportedEntityType = request.SelectedEntityType.Equals("Queue", StringComparison.OrdinalIgnoreCase)
									 || request.SelectedEntityType.Equals("Topic", StringComparison.OrdinalIgnoreCase)
									 || request.SelectedEntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase);

		if (!isSupportedEntityType)
			errors[nameof(request.SelectedEntityType)] = [$"Unsupported entity type '{request.SelectedEntityType}'."];

		if (request.SelectedEntityType.Equals("Subscription", StringComparison.OrdinalIgnoreCase) && request.SelectedTopicName is null)
			errors[nameof(request.SelectedTopicName)] = ["Topic name is required when selecting a subscription."];

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		Business.Viewer.Contracts.ServiceBus.EntityId entityId = EntityIdFactory.Create(
			request.SelectedEntityType,
			request.SelectedEntityName,
			request.SelectedTopicName,
			nameof(request.SelectedTopicName));

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.SelectEntityAsync(
			context.GetClientSessionState(),
			entityId);

		return TypedResults.Ok(ToSelectEntityResponse(result));
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
