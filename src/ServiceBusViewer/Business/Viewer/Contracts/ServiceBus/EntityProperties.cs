namespace ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Base type for cached Service Bus entity properties.</summary>
/// <param name="Name">The entity name.</param>
/// <param name="RequiresSession">Indicates whether the entity requires a session (guarantee the FIFO order).</param>
public abstract record EntityProperties(string Name, bool RequiresSession);
