namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
/// <summary>Identifies a Service Bus topic.</summary>
/// <param name="Name">The topic name.</param>
public sealed record TopicEntityId(string Name) : EntityId(Name);
