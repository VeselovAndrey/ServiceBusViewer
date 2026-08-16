namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Indicates that the current send request is invalid for the selected Service Bus entity.</summary>
public sealed class SendMessageValidationException(string message) : Exception(message);
