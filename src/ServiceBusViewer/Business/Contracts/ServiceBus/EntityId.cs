namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Base type for Service Bus entity identifiers.</summary>
/// <param name="Name">The entity name.</param>
internal abstract record EntityId(string Name);
