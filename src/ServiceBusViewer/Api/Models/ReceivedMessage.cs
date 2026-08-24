namespace ServiceBusViewer.Api.Models;

/// <summary>Represents a received message body and its metadata.</summary>
/// <param name="Body">Message body as a string.</param>
/// <param name="Properties">System properties of the received message.</param>
/// <param name="ApplicationProperties">Application properties dictionary.</param>
public sealed record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, ReceivedMessageApplicationProperty> ApplicationProperties);

/// <summary>Represents a received message application property value and its type metadata.</summary>
/// <param name="Value">The normalized application property value.</param>
/// <param name="Type">The original application property type name.</param>
public sealed record ReceivedMessageApplicationProperty(object Value, ApplicationPropertyType Type);
