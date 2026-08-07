namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Base type for Service Bus entity identifiers.</summary>
/// <param name="Name">The entity name.</param>
public abstract record EntityId(string Name);
