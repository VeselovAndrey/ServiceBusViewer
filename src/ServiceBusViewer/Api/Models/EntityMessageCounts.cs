namespace ServiceBusViewer.Api.Models;

/// <summary>Runtime message counts of a Service Bus entity.</summary>
/// <param name="ActiveMessageCount">The number of active messages in the entity.</param>
/// <param name="ScheduledMessageCount">The number of scheduled messages in the entity.</param>
/// <param name="DeadLetterMessageCount">The number of messages in the entity's dead-letter queue.</param>
public readonly record struct EntityMessageCounts(long ActiveMessageCount, long ScheduledMessageCount, long DeadLetterMessageCount);
