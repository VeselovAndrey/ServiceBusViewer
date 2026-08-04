namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Base type for cached Service Bus entity properties.</summary>
/// <param name="Name">The entity name.</param>
internal abstract record EntityProperties(string Name);
