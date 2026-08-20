namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class SendRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, SendRequest request, IViewerMessageService service, CancellationToken cancellationToken)
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

		MessageProperties messageProperties = CreateMessageProperties(request.SendMessageProperties, errors);
		List<ApplicationProperty> applicationProperties = CreateApplicationProperties(request.SendMessageApplicationProperties, errors);
		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		await service.SendAsync(
			context.GetClientSessionState(),
			new SendCommand(request.SendMessageBody ?? string.Empty, messageProperties, applicationProperties),
			cancellationToken);

		return TypedResults.NoContent();
	}

	private static MessageProperties CreateMessageProperties(SendMessagePropertiesRequest? request, IDictionary<string, string[]> errors)
	{
		DateTimeOffset? scheduled = null;
		if (request?.ScheduledEnqueueTime is not null && DateTimeOffset.TryParse(request?.ScheduledEnqueueTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
			scheduled = dto;

		TimeSpan? ttl = null;
		if (request?.TimeToLive is not null && TimeSpan.TryParse(request?.TimeToLive, CultureInfo.InvariantCulture, out TimeSpan ts))
			ttl = ts;

		return new MessageProperties {
			MessageId = request?.MessageId,
			SessionId = request?.SessionId,
			CorrelationId = request?.CorrelationId,
			ContentType = request?.ContentType,
			ScheduledEnqueueTime = scheduled,
			TimeToLive = ttl
		};
	}

	private static List<ApplicationProperty> CreateApplicationProperties(IReadOnlyList<SendMessageApplicationPropertyRequest>? request, Dictionary<string, string[]> errors)
	{
		if (request is null || request.Count == 0)
			return [];

		List<ApplicationProperty> properties = [];

		for (int index = 0; index < request.Count; index++) {
			SendMessageApplicationPropertyRequest property = request[index];
			string fieldName = $"{nameof(SendRequest.SendMessageApplicationProperties)}[{index}].{nameof(SendMessageApplicationPropertyRequest.Type)}";

			if (property.Type is null)
				continue; // validator already recorded missing-type error

			if (!Enum.TryParse(property.Type, true, out ApplicationPropertyType propertyType)) {
				errors[fieldName] = [$"Unsupported application property type '{property.Type}'."];
				continue;
			}

			if (!IsSupportedSendApplicationPropertyType(propertyType)) {
				errors[fieldName] = [$"Application property type '{property.Type}' is not supported for sending."];
				continue;
			}

			properties.Add(new ApplicationProperty(
				property.Key ?? string.Empty,
				property.Value ?? string.Empty,
				propertyType));
		}

		return properties;
	}

	private static bool IsSupportedSendApplicationPropertyType(ApplicationPropertyType type)
		=> type is not ApplicationPropertyType.ByteArray
		   && type is not ApplicationPropertyType.Null
		   && type is not ApplicationPropertyType.Unknown;
}
