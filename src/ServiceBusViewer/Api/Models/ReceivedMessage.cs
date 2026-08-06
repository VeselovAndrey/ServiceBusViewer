namespace ServiceBusViewer.Api.Models;

using System.Globalization;
using ServiceBusViewer.Api.Endpoints;

/// <summary>
/// Represents a received message body and its metadata.
/// </summary>
/// <param name="Body">Message body as a string.</param>
/// <param name="Properties">System properties of the received message.</param>
/// <param name="ApplicationProperties">Application properties dictionary.</param>
internal sealed record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, object> ApplicationProperties);


internal static class ReceivedMessageExtensions
{
	internal static ReceivedMessage ToApiModel(this Business.Contracts.ServiceBus.ReceivedMessage message)
		=> new Models.ReceivedMessage(
				message.Body,
				new Models.ReceivedMessageProperties(
					message.Properties.MessageId,
					message.Properties.PartitionKey,
					message.Properties.SessionId,
					message.Properties.CorrelationId,
					message.Properties.ContentType,
					message.Properties.EnqueuedTimeUtc.ToString("O", CultureInfo.InvariantCulture),
					message.Properties.ScheduledEnqueueTime?.ToString("O", CultureInfo.InvariantCulture),
					message.Properties.TimeToLive?.ToString("c", CultureInfo.InvariantCulture)),
				message.ApplicationProperties.ToDictionary<KeyValuePair<string, object>, string, object>(pair => pair.Key, pair => ResponseMapping.NormalizeObjectValue(pair.Value) ?? string.Empty));
}