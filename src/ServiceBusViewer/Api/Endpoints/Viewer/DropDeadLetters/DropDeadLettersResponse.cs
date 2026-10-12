namespace ServiceBusViewer.Api.Endpoints.Viewer.DropDeadLetters;

/// <summary>Outcome of a dead-letter drop operation.</summary>
/// <param name="Succeeded">Whether every requested message was processed without failure.</param>
/// <param name="AffectedCount">Number of dead-lettered messages dropped.</param>
/// <param name="FailedCount">Number of messages that could not be processed and remain in the dead-letter queue.</param>
/// <param name="Detail">Human-readable detail about the outcome or null.</param>
public sealed record DropDeadLettersResponse(bool Succeeded, int AffectedCount, int FailedCount, string? Detail);
