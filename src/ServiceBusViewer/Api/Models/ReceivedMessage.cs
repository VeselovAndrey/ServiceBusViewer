namespace ServiceBusViewer.Api.Models;
/// <summary>
/// Represents a received message body and its metadata.
/// </summary>
/// <param name="Body">Message body as a string.</param>
/// <param name="Properties">System properties of the received message.</param>
/// <param name="ApplicationProperties">Application properties dictionary.</param>
internal sealed record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, object?> ApplicationProperties);
