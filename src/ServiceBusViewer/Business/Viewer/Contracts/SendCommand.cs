namespace ServiceBusViewer.Business.Viewer.Contracts;

using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;

/// <summary>Represents a request to send a message to the selected entity.</summary>
/// <param name="Body">The message body.</param>
/// <param name="MessageProperties">Standard Service Bus message properties (content type, message id, etc.).</param>
/// <param name="ApplicationProperties">Custom application properties to attach to the message.</param>
public sealed record SendCommand(
	string Body,
	MessageProperties MessageProperties,
	IReadOnlyList<ApplicationProperty> ApplicationProperties);
