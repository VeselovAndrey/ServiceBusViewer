namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Identifies a Service Bus topic.</summary>
/// <param name="Name">The topic name.</param>
internal sealed record TopicEntityId(string Name) : EntityId(Name);
