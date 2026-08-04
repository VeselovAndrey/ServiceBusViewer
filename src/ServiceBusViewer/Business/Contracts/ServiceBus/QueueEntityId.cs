namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Identifies a Service Bus queue.</summary>
/// <param name="Name">The queue name.</param>
internal sealed record QueueEntityId(string Name) : EntityId(Name);
