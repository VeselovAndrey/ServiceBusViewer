namespace ServiceBusViewer.Api.Endpoints;

using System.Globalization;
using ServiceBusViewer.Business.Contracts.ServiceBus;

internal static class ResponseMapping
{
	internal static Models.ConnectionSettings ToConnectionSettingsResponse(Business.Contracts.Viewer.ConnectionSettings settings)
	{
		return new Models.ConnectionSettings(
			settings.ConnectionString,
			settings.RootConnectionString,
			settings.QueueOrTopicName,
			settings.SubscriptionName);
	}

	internal static Models.ViewerState ToViewerStateResponse(Business.Contracts.Viewer.ViewerState state)
	{
		return new Models.ViewerState(
			state.ServiceBusHostName,
			state.EntityName,
			state.TopicName,
			state.RequiresSession,
			state.IsManagementApiAvailable,
			[.. state.AvailableEntities.Select(ToEntityIdResponse)],
			[.. state.Messages.Select(ToReceivedMessageResponse)],
			state.HasMoreMessages,
			state.DisplayedMessage is null ? null : ToReceivedMessageResponse(state.DisplayedMessage),
			state.SendResultMessage,
			state.ReceiveSessionId);
	}

	internal static Models.EntityId ToEntityIdResponse(EntityId entity) => entity switch {
		QueueEntityId queue => new Models.EntityId("Queue", queue.Name, null),
		TopicEntityId topic => new Models.EntityId("Topic", topic.Name, null),
		SubscriptionEntityId subscription => new Models.EntityId("Subscription", subscription.Name, subscription.TopicName),
		_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
	};

	private static Models.ReceivedMessage ToReceivedMessageResponse(ReceivedMessage message)
	{
		return new Models.ReceivedMessage(
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
				message.ApplicationProperties.ToDictionary<KeyValuePair<string, object>, string, object>(pair => pair.Key, pair => NormalizeObjectValue(pair.Value)));
	}


	// Subscription rule mapping moved to DetailsRequestHandler (single-use)

	internal static object? NormalizeObjectValue(object? value) => value switch {
		null => null,
		DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
		DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
		TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
		Guid guid => guid.ToString(),
		char character => character.ToString(),
		byte[] bytes => Convert.ToBase64String(bytes),
		_ => value
	};

}
