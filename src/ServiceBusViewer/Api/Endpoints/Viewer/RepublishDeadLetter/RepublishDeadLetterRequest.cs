namespace ServiceBusViewer.Api.Endpoints.Viewer.RepublishDeadLetter;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Request to republish the selected entity's dead-lettered message (or the whole dead-letter queue) to its original entity.
/// </summary>
/// <param name="MessageId">Id of the single dead-lettered message to republish, or null to republish every dead-lettered message.</param>
/// <param name="Strategy">Message id strategy (KeepOriginal, AutoGenerate, Guid, GuidV7, or Manual), bound case-insensitively from its JSON string value.</param>
/// <param name="ManualMessageId">The manually provided message id when the strategy is Manual.</param>
public sealed record RepublishDeadLetterRequest(
	string? MessageId,
	RepublishMessageIdStrategy? Strategy,
	string? ManualMessageId);

/// <summary>Message id strategy for republishing dead-lettered messages; API-bound copy of the business contract enum with identical members.</summary>
public enum RepublishMessageIdStrategy
{
	/// <summary>Keep the original message id; valid only when the republish target does not use duplicate detection.</summary>
	KeepOriginal = 1,

	/// <summary>Let the Service Bus client auto-generate a new message id for each republished copy at send time.</summary>
	AutoGenerate = 2,

	/// <summary>Assign a newly generated random GUID message id to each republished copy.</summary>
	// CA1720 is suppressed because the member name is fixed by the developer-approved User Story 13 contract; see .agents/specs/PROJECT_DECISIONS.md.
#pragma warning disable CA1720
	Guid = 3,
#pragma warning restore CA1720

	/// <summary>Assign a newly generated GUID version 7 message id to each republished copy.</summary>
	GuidV7 = 4,

	/// <summary>Republish with the manually provided message id; single-message republish only.</summary>
	Manual = 5
}

/// <summary>Validates a <see cref="RepublishDeadLetterRequest"/> instance.</summary>
internal static class RepublishDeadLetterRequestValidator
{
	/// <summary>Validates a <see cref="RepublishDeadLetterRequest"/> instance.</summary>
	/// <param name="request">The <see cref="RepublishDeadLetterRequest"/> instance to validate.</param>
	/// <param name="errors">A dictionary of validation errors, if any.</param>
	/// <returns><c>true</c> if the request is valid; otherwise, <c>false</c>.</returns>
	public static bool Validate(RepublishDeadLetterRequest request, [NotNullWhen(false)] out IReadOnlyDictionary<string, string[]>? errors)
	{
		Dictionary<string, string[]> local = new();

		if (request.Strategy is null)
			local[nameof(request.Strategy)] = ["Strategy is required."];

		if (request.Strategy is RepublishMessageIdStrategy.Manual) {
			if (string.IsNullOrWhiteSpace(request.MessageId))
				local[nameof(request.MessageId)] = ["The manual strategy supports a single message, not a bulk republish."];

			if (string.IsNullOrWhiteSpace(request.ManualMessageId))
				local[nameof(request.ManualMessageId)] = ["A manual message id is required when the strategy is Manual."];
		}

		errors = local.Count > 0
			? local
			: null;

		return errors is null;
	}
}
