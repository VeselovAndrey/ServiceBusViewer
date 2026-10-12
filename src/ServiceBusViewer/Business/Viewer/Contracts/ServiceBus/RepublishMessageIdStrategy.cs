namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Message id strategy for republishing dead-lettered messages; the explicit member values mirror the API-bound copy.</summary>
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
