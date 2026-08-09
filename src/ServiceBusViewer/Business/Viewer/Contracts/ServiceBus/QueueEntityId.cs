namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
/// <summary>Identifies a Service Bus queue.</summary>
/// <param name="Name">The queue name.</param>
public sealed record QueueEntityId(string Name) : EntityId(Name);
