namespace ServiceBusViewer.Business.Contracts.Viewer;

using ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>
/// Command to send a message to the currently selected entity.
/// </summary>
/// <param name="Body">Message body content as a string.</param>
/// <param name="MessageProperties">Standard Service Bus message properties (content type, message id, etc.).</param>
/// <param name="ApplicationProperties">Custom application properties to attach to the message.</param>
internal sealed record SendCommand(
	string Body,
	MessageProperties MessageProperties,
	IReadOnlyList<ApplicationProperty> ApplicationProperties);
