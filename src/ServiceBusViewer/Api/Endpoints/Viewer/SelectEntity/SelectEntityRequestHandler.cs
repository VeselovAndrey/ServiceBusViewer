namespace ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class SelectEntityRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, SelectEntityRequest request, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];
			SelectEntityRequestValidator.Validate(request, errors);
			string? selectedEntityType = RequestValidation.NormalizeOptional(request.SelectedEntityType);
			string? selectedEntityName = RequestValidation.NormalizeOptional(request.SelectedEntityName);
			string? selectedTopicName = RequestValidation.NormalizeOptional(request.SelectedTopicName);
			RequestValidation.ThrowIfAny(errors);

			ViewerState result = await service.SelectEntityAsync(
				context.GetBrowserSessionState(),
				new SelectEntityCommand(selectedEntityType!, selectedEntityName!, selectedTopicName));

			return ToSelectEntityResponse(result);
		});
	}

	private static SelectEntityResponse ToSelectEntityResponse(Business.Contracts.Viewer.ViewerState state)
	{
		Models.ViewerState response = ResponseMapping.ToViewerStateResponse(state);

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
