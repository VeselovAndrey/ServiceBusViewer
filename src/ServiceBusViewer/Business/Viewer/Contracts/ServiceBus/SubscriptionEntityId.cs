namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
/// <summary>Identifies a Service Bus subscription.</summary>
/// <param name="Name">The subscription name.</param>
/// <param name="TopicName">The parent topic name.</param>
public sealed record SubscriptionEntityId(string Name, string TopicName) : EntityId(Name);
