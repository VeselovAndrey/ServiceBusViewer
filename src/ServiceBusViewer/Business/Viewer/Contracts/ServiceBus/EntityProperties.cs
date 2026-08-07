namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Base type for cached Service Bus entity properties.</summary>
/// <param name="Name">The entity name.</param>
public abstract record EntityProperties(string Name);
