namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Indicates that a connection was requested for a viewer session that is already connected.</summary>
public sealed class ViewerAlreadyConnectedException() : Exception("Already connected to a Service Bus instance.");
