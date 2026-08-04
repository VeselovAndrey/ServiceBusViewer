namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Contracts.ServiceBus;
using ServiceBusViewer.Business.Contracts.Viewer;
using ServiceBusViewer.Infrastructure.BrowserSession;

internal static class SendRequestHandler
{
	public static Task<IResult> HandleAsync(HttpContext context, SendRequest request, IViewerBusinessService service)
	{
		return EndpointExecution.ExecuteAsync(async () => {
			Dictionary<string, string[]> errors = [];
			SendRequestValidator.Validate(request, errors);
			string? body = RequestValidation.NormalizeOptional(request.SendMessageBody);
			MessageProperties messageProperties = CreateMessageProperties(request.SendMessageProperties, errors);
			List<ApplicationProperty> applicationProperties = CreateApplicationProperties(request.SendMessageApplicationProperties, errors);
			RequestValidation.ThrowIfAny(errors);

			ViewerState result = await service.SendAsync(
				context.GetBrowserSessionState(),
				new SendCommand(body!, messageProperties, applicationProperties));

			return ToSendResponse(result);
		});
	}

	private static SendResponse ToSendResponse(Business.Contracts.Viewer.ViewerState state)
	{
		Models.ViewerState response = ResponseMapping.ToViewerStateResponse(state);

		return new SendResponse(
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

	private static MessageProperties CreateMessageProperties(SendMessagePropertiesRequest? request, IDictionary<string, string[]> errors)
	{
		string? messageId = RequestValidation.NormalizeOptional(request?.MessageId);
		string? sessionId = RequestValidation.NormalizeOptional(request?.SessionId);
		string? correlationId = RequestValidation.NormalizeOptional(request?.CorrelationId);
		string? contentType = RequestValidation.NormalizeOptional(request?.ContentType);
		string? scheduledEnqueueTime = RequestValidation.NormalizeOptional(request?.ScheduledEnqueueTime);
		string? timeToLive = RequestValidation.NormalizeOptional(request?.TimeToLive);

		RequestValidation.ValidateSystemPropertyLength(messageId, nameof(SendMessagePropertiesRequest.MessageId), "Message ID", errors);
		RequestValidation.ValidateSystemPropertyLength(sessionId, nameof(SendMessagePropertiesRequest.SessionId), "Session ID", errors);
		RequestValidation.ValidateSystemPropertyLength(correlationId, nameof(SendMessagePropertiesRequest.CorrelationId), "Correlation ID", errors);

		return new MessageProperties {
			MessageId = messageId,
			SessionId = sessionId,
			CorrelationId = correlationId,
			ContentType = contentType,
			ScheduledEnqueueTime = RequestValidation.ParseDateTimeOffset(scheduledEnqueueTime, nameof(SendMessagePropertiesRequest.ScheduledEnqueueTime), errors),
			TimeToLive = RequestValidation.ParseTimeSpan(timeToLive, nameof(SendMessagePropertiesRequest.TimeToLive), errors)
		};
	}

	private static List<ApplicationProperty> CreateApplicationProperties(IReadOnlyList<SendMessageApplicationPropertyRequest>? request, IDictionary<string, string[]> errors)
	{
		if (request is null || request.Count == 0)
			return [];

		List<ApplicationProperty> properties = [];

		for (int index = 0; index < request.Count; index++) {
			SendMessageApplicationPropertyRequest property = request[index];
			string fieldName = $"{nameof(SendRequest.SendMessageApplicationProperties)}[{index}].{nameof(SendMessageApplicationPropertyRequest.Type)}";
			string? typeText = RequestValidation.Require(property.Type, fieldName, "Application property type is required.", errors);

			if (typeText is null)
				continue;

			if (!Enum.TryParse(typeText, true, out ApplicationPropertyType propertyType)) {
				errors[fieldName] = [$"Unsupported application property type '{typeText}'."];
				continue;
			}

			properties.Add(new ApplicationProperty(
				RequestValidation.NormalizeOptional(property.Key) ?? string.Empty,
				property.Value ?? string.Empty,
				propertyType));
		}

		return properties;
	}
}
