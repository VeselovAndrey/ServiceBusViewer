namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using System.Collections.Generic;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Request to send a message; includes body, system properties and application properties.
/// </summary>
/// <param name="SendMessageBody">Message body to send.</param>
/// <param name="SendMessageProperties">Optional system properties for the message.</param>
/// <param name="SendMessageApplicationProperties">Optional list of application properties.</param>
internal sealed record SendRequest(
	string? SendMessageBody,
	SendMessagePropertiesRequest? SendMessageProperties,
	IReadOnlyList<SendMessageApplicationPropertyRequest>? SendMessageApplicationProperties);

internal static class SendRequestValidator
{
	public static void Validate(SendRequest request, IDictionary<string, string[]> errors)
	{
		RequestValidation.Require(request.SendMessageBody, nameof(request.SendMessageBody), "Message body cannot be empty.", errors);
	}
}

/// <summary>
/// Optional system properties for the message to send.
/// </summary>
/// <param name="MessageId">Message id.</param>
/// <param name="SessionId">Session id.</param>
/// <param name="CorrelationId">Correlation id.</param>
/// <param name="ContentType">Content type.</param>
/// <param name="ScheduledEnqueueTime">Scheduled enqueue time (ISO-8601) or null.</param>
/// <param name="TimeToLive">Time-to-live string or null.</param>
internal sealed record SendMessagePropertiesRequest(
	string? MessageId,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	string? ScheduledEnqueueTime,
	string? TimeToLive);

internal static class SendMessagePropertiesRequestValidator
{
	public static void Validate(SendMessagePropertiesRequest? request, IDictionary<string, string[]> errors)
	{
		if (request is null)
			return;
		string? messageId = RequestValidation.NormalizeOptional(request.MessageId);		string? sessionId = RequestValidation.NormalizeOptional(request.SessionId);
		string? correlationId = RequestValidation.NormalizeOptional(request.CorrelationId);
		RequestValidation.ValidateSystemPropertyLength(messageId, nameof(SendMessagePropertiesRequest.MessageId), "Message ID", errors);
		RequestValidation.ValidateSystemPropertyLength(sessionId, nameof(SendMessagePropertiesRequest.SessionId), "Session ID", errors);
		RequestValidation.ValidateSystemPropertyLength(correlationId, nameof(SendMessagePropertiesRequest.CorrelationId), "Correlation ID", errors);
	}
}

/// <summary>
/// Single application property entry for a message to send.
/// </summary>
/// <param name="Key">Property key.</param>
/// <param name="Value">Property value.</param>
/// <param name="Type">Property type name.</param>
internal sealed record SendMessageApplicationPropertyRequest(
	string? Key,
	string? Value,
	string? Type);

internal static class SendMessageApplicationPropertyRequestValidator
{
	public static void Validate(SendMessageApplicationPropertyRequest request, string fieldName, IDictionary<string, string[]> errors)
	{
		RequestValidation.Require(request.Type, fieldName, "Application property type is required.", errors);
		// enum parsing and type-specific validation remains in the handler helper where the ApplicationProperty instance is constructed.
	}
}