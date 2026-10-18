namespace ServiceBusViewer.Api.Models;

/// <summary>An entity available in the namespace together with its runtime message counts.</summary>
/// <param name="Type">Entity kind: "Queue", "Topic" or "Subscription".</param>
/// <param name="Name">Entity name.</param>
/// <param name="TopicName">Topic name for subscriptions; null for queues/topics.</param>
/// <param name="MessageCounts">Runtime message counts, or null when they could not be retrieved (e.g. the emulator) or the entity is a topic.</param>
public sealed record AvailableEntity(
	string Type,
	string Name,
	string? TopicName,
	EntityMessageCounts? MessageCounts);
