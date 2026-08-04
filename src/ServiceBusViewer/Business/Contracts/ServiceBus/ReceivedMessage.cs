namespace ServiceBusViewer.Business.Contracts.ServiceBus;

/// <summary>Represents a message received from Service Bus.</summary>
/// <param name="Body">The message body.</param>
/// <param name="Properties">The system properties.</param>
/// <param name="ApplicationProperties">The application properties.</param>
internal sealed record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, object> ApplicationProperties);
