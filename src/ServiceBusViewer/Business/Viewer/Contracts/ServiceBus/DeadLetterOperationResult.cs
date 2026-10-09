namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Reports the outcome of a bulk dead-letter operation on the selected entity's $DeadLetterQueue.</summary>
/// <param name="Succeeded">Whether every requested message was processed without failure.</param>
/// <param name="AffectedCount">Number of dead-lettered messages republished or dropped.</param>
/// <param name="FailedCount">Number of messages that could not be processed and remain in the dead-letter queue.</param>
/// <param name="Detail">Human-readable detail about the outcome, or <c>null</c> when there is nothing to report.</param>
public sealed record DeadLetterOperationResult(bool Succeeded, int AffectedCount, int FailedCount, string? Detail);
