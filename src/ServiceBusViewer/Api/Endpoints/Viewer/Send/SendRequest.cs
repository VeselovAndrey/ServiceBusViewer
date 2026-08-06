namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using System;
using System.Collections.Generic;
using System.Globalization;

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
	public static bool Validate(SendRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.SendMessageBody))
			local[nameof(request.SendMessageBody)] = ["Message body cannot be empty."];

		if (local.Count > 0) {
			errors = local;
			return false;
		}
		errors = null;
		return true;
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
	private const int _maxSystemPropertyLength = 128;

	public static bool Validate(SendMessagePropertiesRequest? request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (request is null) {
			errors = null;
			return true;
		}

		if (!string.IsNullOrWhiteSpace(request.MessageId) && request.MessageId.Length > _maxSystemPropertyLength)
			local[nameof(SendMessagePropertiesRequest.MessageId)] = [$"The Message ID must be {_maxSystemPropertyLength} characters or fewer."];

		if (!string.IsNullOrWhiteSpace(request.SessionId) && request.SessionId!.Length > _maxSystemPropertyLength)
			local[nameof(SendMessagePropertiesRequest.SessionId)] = [$"The Session ID must be {_maxSystemPropertyLength} characters or fewer."];

		if (!string.IsNullOrWhiteSpace(request.CorrelationId) && request.CorrelationId!.Length > _maxSystemPropertyLength)
			local[nameof(SendMessagePropertiesRequest.CorrelationId)] = [$"The Correlation ID must be {_maxSystemPropertyLength} characters or fewer."];

		if (!string.IsNullOrWhiteSpace(request.ScheduledEnqueueTime) && !DateTimeOffset.TryParse(request.ScheduledEnqueueTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
			local[nameof(SendMessagePropertiesRequest.ScheduledEnqueueTime)] = ["Scheduled enqueue time must be a valid date/time value."];

		if (!string.IsNullOrWhiteSpace(request.TimeToLive) && !TimeSpan.TryParse(request.TimeToLive, CultureInfo.InvariantCulture, out _))
			local[nameof(SendMessagePropertiesRequest.TimeToLive)] = ["Time to live must be a valid time span value."];

		if (local.Count > 0) {
			errors = local;
			return false;
		}

		errors = null;
		return true;
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
	public static bool Validate(SendMessageApplicationPropertyRequest request, string fieldName, out IReadOnlyDictionary<string, string[]>? errors)
	{
		var local = new Dictionary<string, string[]>();
		if (string.IsNullOrWhiteSpace(request.Type))
			local[fieldName] = ["Application property type is required."];
		// enum parsing and type-specific validation remains in the handler helper where the ApplicationProperty instance is constructed.
		if (local.Count > 0) { errors = local; return false; }
		errors = null;
		return true;
	}
}