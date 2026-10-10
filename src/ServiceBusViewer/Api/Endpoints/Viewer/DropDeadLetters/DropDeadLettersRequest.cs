namespace ServiceBusViewer.Api.Endpoints.Viewer.DropDeadLetters;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Request to permanently delete the selected entity's dead-lettered message (or the whole dead-letter queue) without re-delivering it.
/// </summary>
/// <param name="MessageId">Id of the single dead-lettered message to drop, or null to drop every dead-lettered message.</param>
public sealed record DropDeadLettersRequest(string? MessageId);

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

		errors = local.Count > 0
			? local
			: null;

		return errors is null;
	}
}
