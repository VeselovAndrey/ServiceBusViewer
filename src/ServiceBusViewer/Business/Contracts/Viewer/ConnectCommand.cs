namespace ServiceBusViewer.Business.Contracts.Viewer;

internal sealed record ConnectCommand(
	string ConnectionString,
	string? RootConnectionString,
	string? QueueOrTopicName,
	string? SubscriptionName);
