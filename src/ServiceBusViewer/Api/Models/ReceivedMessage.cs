namespace ServiceBusViewer.Api.Models;

using System.Globalization;
using ServiceBusViewer.Api.Converters;

/// <summary>Represents a received message body and its metadata.</summary>
/// <param name="Body">Message body as a string.</param>
/// <param name="Properties">System properties of the received message.</param>
/// <param name="ApplicationProperties">Application properties dictionary.</param>
public sealed record ReceivedMessage(
	string Body,
	ReceivedMessageProperties Properties,
	IReadOnlyDictionary<string, ReceivedMessageApplicationProperty> ApplicationProperties);

/// <summary>Provides mappings from business received-message models to API models.</summary>
public static class ReceivedMessageExtensions
{
	/// <summary>Maps a business received message to its API representation.</summary>
	/// <param name="message">The business received message to map.</param>
	/// <returns>The API received-message model.</returns>
	internal static ReceivedMessage ToApiModel(this Business.Viewer.Contracts.ServiceBus.ReceivedMessage message)
		=> new Models.ReceivedMessage(
				message.Body,
				new ReceivedMessageProperties(
					message.Properties.MessageId,
					message.Properties.PartitionKey,
					message.Properties.SessionId,
					message.Properties.CorrelationId,
					message.Properties.ContentType,
					message.Properties.EnqueuedTimeUtc.ToString("O", CultureInfo.InvariantCulture),
					message.Properties.ScheduledEnqueueTime?.ToString("O", CultureInfo.InvariantCulture),
					message.Properties.TimeToLive?.ToString("c", CultureInfo.InvariantCulture),
					message.Properties.To,
					message.Properties.ReplyTo,
					message.Properties.Subject),
				message.ApplicationProperties.ToDictionary<KeyValuePair<string, object>, string, ReceivedMessageApplicationProperty>(
					pair => pair.Key,
					pair => new ReceivedMessageApplicationProperty(
						ResponseMapping.NormalizeObjectValue(pair.Value) ?? string.Empty,
						ResponseMapping.GetApplicationPropertyType(pair.Value))));
}


/// <summary>Represents a received message application property value and its type metadata.</summary>
/// <param name="Value">The normalized application property value.</param>
/// <param name="Type">The original application property type name.</param>
public sealed record ReceivedMessageApplicationProperty(object Value, ApplicationPropertyType Type);