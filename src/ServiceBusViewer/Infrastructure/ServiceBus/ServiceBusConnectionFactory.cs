namespace ServiceBusViewer.Infrastructure.ServiceBus;

using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.ServiceBus.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Builds browser-session-scoped Service Bus connections.</summary>
internal sealed class ServiceBusConnectionFactory : IServiceBusConnectionFactory
{
	public async Task<IServiceBusConnection> OpenAsync(ConnectionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		ServiceBusClient client = new(settings.ConnectionString);

		try {
			ServiceBusAdministrationClient? adminClient = string.IsNullOrWhiteSpace(settings.RootConnectionString)
				? null
				: new ServiceBusAdministrationClient(settings.RootConnectionString);

			ServiceBusConnection connection = adminClient is null
				? CreateScopedEntityConnection(client, settings)
				: await CreateNamespaceConnectionAsync(client, adminClient, settings.ConnectionString);

			return connection;
		}
		catch {
			await client.DisposeAsync();
			throw;
		}
	}

	private static async Task<ServiceBusConnection> CreateNamespaceConnectionAsync(
		ServiceBusClient client,
		ServiceBusAdministrationClient adminClient,
		string connectionString)
	{
		ServiceBusConnection connection = new(
			client,
			adminClient,
			GetServiceBusHost(connectionString));

		await connection.RefreshEntitiesAsync();
		return connection;
	}

	private static ServiceBusConnection CreateScopedEntityConnection(ServiceBusClient client, ConnectionSettings settings)
	{
		if (string.IsNullOrWhiteSpace(settings.QueueOrTopicName))
			throw new InvalidOperationException("Queue or topic name is required.");

		List<EntityProperties> availableEntities = [];

		if (!string.IsNullOrWhiteSpace(settings.SubscriptionName)) {
			availableEntities.Add(new TopicEntityProperties(
				settings.QueueOrTopicName,
				TimeSpan.MaxValue,
				false,
				TimeSpan.FromMinutes(10),
				true,
				false,
				TimeSpan.MaxValue));

			SubscriptionEntityProperties subscription = new(
				settings.SubscriptionName,
				settings.QueueOrTopicName,
				TimeSpan.FromMinutes(1),
				10,
				TimeSpan.MaxValue,
				false,
				false,
				true,
				TimeSpan.MaxValue,
				[]);
			availableEntities.Add(subscription);
		}
		else {
			QueueEntityProperties queue = new(
				settings.QueueOrTopicName,
				TimeSpan.FromMinutes(1),
				10,
				TimeSpan.MaxValue,
				false,
				TimeSpan.FromMinutes(10),
				false,
				true,
				false,
				false,
				TimeSpan.MaxValue);
			availableEntities.Add(queue);
		}

		return new ServiceBusConnection(
			client,
			null,
			GetServiceBusHost(settings.ConnectionString),
			availableEntities);
	}

	private static string GetServiceBusHost(string connectionString)
	{
		const string connectionStringPrefix = "Endpoint=sb://";

		if (!connectionString.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Invalid Service Bus connection string.", nameof(connectionString));

		int endIndex = connectionString.IndexOf(';', connectionStringPrefix.Length);
		if (endIndex < 0)
			endIndex = connectionString.Length;

		ReadOnlySpan<char> host = connectionString.AsSpan(connectionStringPrefix.Length, endIndex - connectionStringPrefix.Length)
			.TrimEnd('/');

		return host.ToString();
	}
}
