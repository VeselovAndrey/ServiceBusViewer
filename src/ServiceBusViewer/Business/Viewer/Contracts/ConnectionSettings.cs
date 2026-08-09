namespace ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Represents the settings used to connect to a Service Bus namespace.</summary>
public sealed record ConnectionSettings
{
	/// <summary>Gets the primary Service Bus connection string used for messaging operations.</summary>
	public string ConnectionString { get; }

	/// <summary>Gets the root connection string used for namespace-level management operations, if available.</summary>
	public string? RootConnectionString { get; }

	/// <summary>Gets the queue or topic name selected for entity-scoped connections, if any.</summary>
	public string? QueueOrTopicName { get; }

	/// <summary>Gets the subscription name selected for a topic subscription connection, if any.</summary>
	public string? SubscriptionName { get; }

	/// <summary>
	/// Initializes namespace-scoped connection settings using both messaging and root connection strings.
	/// </summary>
	/// <param name="connectionString">Primary Service Bus connection string used for messaging operations.</param>
	/// <param name="rootConnectionString">Root connection string used for namespace-level management operations.</param>
	/// <param name="queueOrTopicName">Optional default queue or topic name to select after connecting.</param>
	/// <param name="subscriptionName">Optional subscription name when a topic subscription is selected.</param>
	public ConnectionSettings(
		string connectionString,
		string rootConnectionString,
		string? queueOrTopicName,
		string? subscriptionName)
	{
		ConnectionString = connectionString;
		RootConnectionString = rootConnectionString;
		QueueOrTopicName = queueOrTopicName;
		SubscriptionName = subscriptionName;
	}

	/// <summary>Initializes entity-scoped connection settings using only the primary connection string.</summary>
	/// <param name="connectionString">Primary Service Bus connection string used for messaging operations.</param>
	/// <param name="queueOrTopicName">Queue or topic name to connect to.</param>
	/// <param name="subscriptionName">Optional subscription name when a topic subscription is selected.</param>
	public ConnectionSettings(
		string connectionString,
		string queueOrTopicName,
		string? subscriptionName)
	{
		ConnectionString = connectionString;
		RootConnectionString = null;
		QueueOrTopicName = queueOrTopicName;
		SubscriptionName = subscriptionName;
	}
}
