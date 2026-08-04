namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record ConnectionSettings(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
