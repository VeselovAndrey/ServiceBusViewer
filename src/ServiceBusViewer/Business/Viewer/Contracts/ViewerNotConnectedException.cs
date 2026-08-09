namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Indicates that a viewer operation requires a Service Bus connection that is not available.</summary>
public sealed class ViewerNotConnectedException() : Exception("Not connected to any Service Bus instance.");
