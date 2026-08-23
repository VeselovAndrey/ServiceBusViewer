namespace ServiceBusViewer.Api.Endpoints.Viewer.Send;

using System;
using System.Collections.Generic;

/// <summary>
/// Request to send a message; includes body, system properties and application properties.
/// </summary>
/// <param name="SendMessageBody">Message body to send.</param>
/// <param name="SendMessageProperties">Optional system properties for the message.</param>
/// <param name="SendMessageApplicationProperties">Optional list of application properties.</param>
public sealed record SendRequest(
	string? SendMessageBody,
	SendMessagePropertiesRequest? SendMessageProperties,
	IReadOnlyList<SendMessageApplicationPropertyRequest>? SendMessageApplicationProperties);

/// <summary>Validates a <see cref="SendRequest"/> instance.</summary>
internal static class SendRequestValidator
{
	/// <summary>Validates a <see cref="SendRequest"/> instance.</summary>
	/// <param name="request">The <see cref="SendRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(SendRequest request, out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();
		if (string.IsNullOrWhiteSpace(request.SendMessageBody))
			local[nameof(request.SendMessageBody)] = ["Message body cannot be empty."];

		errors = local.Count > 0 ? local : null;

		return errors is null;
	}
}

/// <summary>Optional system properties for the message to send.</summary>
/// <param name="MessageId">Message id.</param>
/// <param name="SessionId">Session id.</param>
/// <param name="CorrelationId">Correlation id.</param>
/// <param name="ContentType">Content type.</param>
/// <param name="ScheduledEnqueueTime">Scheduled enqueue time (ISO-8601).</param>
/// <param name="TimeToLive">Time-to-live string or null.</param>
/// <param name="To">To property or null.</param>
/// <param name="ReplyTo">Reply To property or null.</param>
/// <param name="Subject">Subject or null.</param>
/// <param name="PartitionKey">Partition key or null.</param>
public sealed record SendMessagePropertiesRequest(
	string? MessageId,
	string? SessionId,
	string? CorrelationId,
	string? ContentType,
	string? ScheduledEnqueueTime,
	string? TimeToLive,
	string? To,
	string? ReplyTo,
	string? Subject,
	string? PartitionKey);

/// <summary>Validates a <see cref="SendMessagePropertiesRequest"/> instance.</summary>
internal static class SendMessagePropertiesRequestValidator
{
	private const int _maxSystemPropertyLength = 128;

	/// <summary>Validates a <see cref="SendMessagePropertiesRequest"/> instance.</summary>
	/// <param name="request">The <see cref="SendMessagePropertiesRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
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

		if (!string.IsNullOrWhiteSpace(request.PartitionKey) && request.PartitionKey.Length > _maxSystemPropertyLength)
			local[nameof(SendMessagePropertiesRequest.PartitionKey)] = [$"The Partition Key must be {_maxSystemPropertyLength} characters or fewer."];

		errors = local.Count > 0 ? local : null;

		return errors is null;
	}
}

/// <summary>Single application property entry for a message to send.</summary>
/// <param name="Key">Property key.</param>
/// <param name="Value">Property value.</param>
/// <param name="Type">Property type name.</param>
public sealed record SendMessageApplicationPropertyRequest(
	string? Key,
	string? Value,
	string? Type);

/// <summary>Validates a <see cref="SendMessageApplicationPropertyRequest"/> instance.	</summary>
internal static class SendMessageApplicationPropertyRequestValidator
{
	/// <summary>Validates a <see cref="SendMessageApplicationPropertyRequest"/> instance.</summary>
	/// <param name="request">The <see cref="SendMessageApplicationPropertyRequest"/> instance to validate.</param>
	/// <param name="fieldName">The field name for error reporting.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(SendMessageApplicationPropertyRequest request, string fieldName, out IReadOnlyDictionary<string, string[]>? errors)
	{
		var local = new Dictionary<string, string[]>();

		if (string.IsNullOrWhiteSpace(request.Type))
			local[fieldName] = ["Application property type is required."];

		// enum parsing and type-specific validation remains in the handler helper where the ApplicationProperty instance is constructed.

		errors = local.Count > 0 ? local : null;

		return errors is null;
	}
}