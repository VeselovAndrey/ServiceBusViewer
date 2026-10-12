namespace ServiceBusViewer.Api.Endpoints.Viewer.RepublishDeadLetter;

/// <summary>Outcome of a dead-letter republish operation.</summary>
/// <param name="Succeeded">Whether every requested message was processed without failure.</param>
/// <param name="AffectedCount">Number of dead-lettered messages republished.</param>
/// <param name="FailedCount">Number of messages that could not be processed and remain in the dead-letter queue.</param>
/// <param name="Detail">Human-readable detail about the outcome or null.</param>
public sealed record RepublishDeadLetterResponse(bool Succeeded, int AffectedCount, int FailedCount, string? Detail);
