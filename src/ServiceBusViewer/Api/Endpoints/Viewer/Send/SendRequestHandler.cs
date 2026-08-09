namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.Models;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class SendRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, SendRequest request, IViewerMessageService service)
	{
		Dictionary<string, string[]> errors = [];

		if (!SendRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? sendRequestErrors) && sendRequestErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in sendRequestErrors)
				errors[kv.Key] = kv.Value;
		}

		if (!SendMessagePropertiesRequestValidator.Validate(request.SendMessageProperties, out IReadOnlyDictionary<string, string[]>? propsErrors) && propsErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in propsErrors)
				errors[kv.Key] = kv.Value;
		}

		string? body = RequestValidation.NormalizeOptional(request.SendMessageBody);
		MessageProperties messageProperties = CreateMessageProperties(request.SendMessageProperties, errors);
		List<ApplicationProperty> applicationProperties = CreateApplicationProperties(request.SendMessageApplicationProperties, errors);

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.SendAsync(
			context.GetClientSessionState(),
			new SendCommand(body!, messageProperties, applicationProperties));

		return TypedResults.Ok(ToSendResponse(result));
	}

	private static SendResponse ToSendResponse(ServiceBusViewer.Business.Viewer.Contracts.ViewerState state)
	{
		Models.ViewerState response = state.ToApiModel();

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

		DateTimeOffset? scheduled = null;
		if (scheduledEnqueueTime is not null && DateTimeOffset.TryParse(scheduledEnqueueTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
			scheduled = dto;

		TimeSpan? ttl = null;
		if (timeToLive is not null && TimeSpan.TryParse(timeToLive, CultureInfo.InvariantCulture, out TimeSpan ts))
			ttl = ts;

		return new MessageProperties {
			MessageId = messageId,
			SessionId = sessionId,
			CorrelationId = correlationId,
			ContentType = contentType,
			ScheduledEnqueueTime = scheduled,
			TimeToLive = ttl
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
			string? typeText = RequestValidation.NormalizeOptional(property.Type);

			if (typeText is null)
				continue; // validator already recorded missing-type error

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
