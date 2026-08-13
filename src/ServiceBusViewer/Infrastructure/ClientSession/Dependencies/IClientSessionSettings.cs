namespace ServiceBusViewer.Infrastructure.ClientSession.Dependencies;

/// <summary>Provides the initial connection settings for newly created browser sessions.</summary>
internal interface IClientSessionSettings
{
	/// <summary>Gets the primary Service Bus connection string.</summary>
	string ConnectionString { get; }

	/// <summary>Gets the optional emulator management connection string.</summary>
	string? EmulatorManagementConnectionString { get; }

	/// <summary>Gets the optional initially selected queue or parent topic name.</summary>
	string? QueueOrTopicName { get; }

	/// <summary>Gets the optional initially selected subscription name.</summary>
	string? SubscriptionName { get; }
}
