namespace ServiceBusViewer.Api.Endpoints.Viewer.DropDeadLetters;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Request to permanently delete the selected entity's dead-lettered message (or the whole dead-letter queue) without re-delivering it.
/// </summary>
/// <param name="MessageId">Id of the single dead-lettered message to drop, or null to drop every dead-lettered message.</param>
/// <param name="SequenceNumber">Exact broker sequence number of the single dead-lettered message to drop, serialized as a string to preserve 64-bit
/// values; when set it selects exactly one message independently of its message id and takes precedence. Null retains the message-id or bulk behavior.</param>
/// <param name="MaxCount">Maximum number of messages a bulk drop processes in this request, or null for an unbounded bulk drop; must be a positive
/// integer when set. Meaningful only for bulk requests (MessageId null).</param>
public sealed record DropDeadLettersRequest(
	string? MessageId,
	string? SequenceNumber,
	int? MaxCount);

/// <summary>Validates a <see cref="DropDeadLettersRequest"/> instance.</summary>
internal static class DropDeadLettersRequestValidator
{
	/// <summary>Validates a <see cref="DropDeadLettersRequest"/> instance.</summary>
	/// <param name="request">The <see cref="DropDeadLettersRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(DropDeadLettersRequest request, [NotNullWhen(false)] out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();

		// MessageId is either null (bulk drop) or a non-empty message id (single drop); an empty value is rejected.
		if (request.MessageId is not null && string.IsNullOrWhiteSpace(request.MessageId))
			local[nameof(request.MessageId)] = ["MessageId must be a non-empty message id, or null for a bulk drop."];

		// MaxCount is either null (unbounded, current behavior) or a positive integer; zero and negative values are rejected.
		if (request.MaxCount <= 0)
			local[nameof(request.MaxCount)] = ["MaxCount must be a positive integer, or null for an unbounded bulk drop."];

		errors = local.Count > 0
			? local
			: null;

		return errors is null;
	}
}
