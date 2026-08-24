namespace ServiceBusViewer.Api.Converters;

using System.Globalization;
using ServiceBusViewer.Api.Models;
using BusinessConnectionSettings = ServiceBusViewer.Business.Viewer.Contracts.ConnectionSettings;
using BusinessViewerState = ServiceBusViewer.Business.Viewer.Contracts.ViewerState;
using BusinessEntityId = ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.EntityId;
using BusinessReceivedMessage = ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.ReceivedMessage;
using QueueEntityId = ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.QueueEntityId;
using SubscriptionEntityId = ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.SubscriptionEntityId;
using TopicEntityId = ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.TopicEntityId;

/// <summary>Maps business-contract models to API response models.</summary>
internal static class BusinessModelMapping
{
	/// <summary>Maps business-contract connection settings to an API response model.</summary>
	internal static ConnectionSettings ToApiModel(this BusinessConnectionSettings settings)
		=> new(settings.ConnectionString, settings.EmulatorManagementConnectionString, settings.QueueOrTopicName, settings.SubscriptionName);

	/// <summary>Maps a business-contract entity identifier to an API response model.</summary>
	internal static EntityId ToApiModel(this BusinessEntityId entity)
		=> entity switch {
			QueueEntityId queue => new EntityId("Queue", queue.Name, null),
			TopicEntityId topic => new EntityId("Topic", topic.Name, null),
			SubscriptionEntityId subscription => new EntityId("Subscription", subscription.Name, subscription.TopicName),
			_ => throw new ArgumentException($"Unknown entity type: {entity.GetType().FullName}", nameof(entity))
		};

	/// <summary>Maps a business-contract received message to an API response model.</summary>
	internal static ReceivedMessage ToApiModel(this BusinessReceivedMessage message)
		=> new(
			message.Body,
			new ReceivedMessageProperties(
				message.Properties.MessageId, message.Properties.PartitionKey, message.Properties.SessionId, message.Properties.CorrelationId,
				message.Properties.ContentType, message.Properties.DeliveryCount, message.Properties.EnqueuedTimeUtc.ToString("O", CultureInfo.InvariantCulture),
				message.Properties.ScheduledEnqueueTime?.ToString("O", CultureInfo.InvariantCulture), message.Properties.TimeToLive?.ToString("c", CultureInfo.InvariantCulture),
				message.Properties.To, message.Properties.ReplyTo, message.Properties.Subject),
			message.ApplicationProperties.ToDictionary(
				pair => pair.Key,
				pair => new ReceivedMessageApplicationProperty(ResponseMapping.NormalizeObjectValue(pair.Value) ?? string.Empty, ResponseMapping.GetApplicationPropertyType(pair.Value))));

	/// <summary>Maps business-contract viewer state to an API response model.</summary>
	internal static ViewerState ToApiModel(this BusinessViewerState state)
		=> new(
			state.ServiceBusHostName, state.EntityName, state.TopicName, state.RequiresSession, state.IsManagementApiAvailable,
			[.. state.AvailableEntities.Select(entity => entity.ToApiModel())], [.. state.Messages.Select(message => message.ToApiModel())], state.HasMoreMessages,
			state.DisplayedMessage?.ToApiModel(), state.SendResultMessage, state.ReceiveSessionId);
}
