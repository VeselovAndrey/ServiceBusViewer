namespace ServiceBusViewer.Infrastructure.ServiceBus.Models;

/// <summary>Represents information about a Service Bus entity.</summary>
/// <param name="Name">The name of the entity.</param>
public abstract record EntityInfo(string Name);

/// <summary>Represents information about a Service Bus queue.</summary>
/// <param name="Name">The name of the queue.</param>
public record QueueEntityInfo(string Name) : EntityInfo(Name);

/// <summary>Represents information about a Service Bus topic.</summary>
/// <param name="Name">The name of the topic.</param>
public record TopicEntityInfo(string Name) : EntityInfo(Name);

/// <summary>Represents information about a Service Bus topic subscription.</summary>
/// <param name="Name">The name of the subscription.</param>
/// <param name="TopicName">The name of the parent topic.</param>
public record SubscriptionEntityInfo(string Name, string TopicName) : EntityInfo(Name);