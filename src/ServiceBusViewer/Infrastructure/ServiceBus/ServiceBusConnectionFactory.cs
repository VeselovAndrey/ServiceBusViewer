namespace ServiceBusViewer.Infrastructure.ServiceBus;

using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Builds browser-session-scoped Service Bus connections.</summary>
internal sealed class ServiceBusConnectionFactory : IServiceBusConnectionFactory
{
	public async Task<IServiceBusConnection> OpenAsync(ConnectionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		ServiceBusConnectionStringProperties connectionProperties = ServiceBusConnectionStringProperties.Parse(settings.ConnectionString);
		ServiceBusConnectionStringProperties? emulatorManagementProperties = ParseEmulatorManagementProperties(settings);
		bool usesDevelopmentEmulator = UsesDevelopmentEmulator(settings.ConnectionString);
		bool managementUsesDevelopmentEmulator = emulatorManagementProperties is not null
			&& UsesDevelopmentEmulator(settings.EmulatorManagementConnectionString!);
		ValidateConnectionCombination(usesDevelopmentEmulator, emulatorManagementProperties, managementUsesDevelopmentEmulator);

		ServiceBusClient client = new(settings.ConnectionString);

		try {
			if (usesDevelopmentEmulator) {
				if (emulatorManagementProperties is null)
					return CreateScopedEntityConnection(client, settings, connectionProperties.FullyQualifiedNamespace);

				ServiceBusAdministrationClient emulatorAdminClient = new(settings.EmulatorManagementConnectionString!);
				return await CreateNamespaceConnectionAsync(client, emulatorAdminClient, connectionProperties.FullyQualifiedNamespace);
			}

			ServiceBusAdministrationClient adminClient = new(settings.ConnectionString);

			try {
				return await CreateNamespaceConnectionAsync(client, adminClient, connectionProperties.FullyQualifiedNamespace);
			}
			catch (Exception exception) when (IsManagementAuthorizationFailure(exception)) {
				return CreateScopedEntityConnection(client, settings, connectionProperties.FullyQualifiedNamespace);
			}
		}
		catch {
			await client.DisposeAsync();
			throw;
		}
	}

	private static async Task<ServiceBusConnection> CreateNamespaceConnectionAsync(
		ServiceBusClient client,
		ServiceBusAdministrationClient adminClient,
		string namespaceHost)
	{
		var connection = new ServiceBusConnection(client, adminClient, namespaceHost);
		await connection.RefreshEntitiesAsync();

		return connection;
	}

	private static ServiceBusConnection CreateScopedEntityConnection(ServiceBusClient client, ConnectionSettings settings, string namespaceHost)
	{
		if (string.IsNullOrWhiteSpace(settings.QueueOrTopicName))
			throw new ArgumentException("Queue or topic name is required because Service Bus management is unavailable. Provide a queue or topic name and try again.");

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
			namespaceHost,
			availableEntities);
	}

	private static ServiceBusConnectionStringProperties? ParseEmulatorManagementProperties(ConnectionSettings settings)
		=> !string.IsNullOrWhiteSpace(settings.EmulatorManagementConnectionString)
			? ServiceBusConnectionStringProperties.Parse(settings.EmulatorManagementConnectionString)
			: null;

	private static void ValidateConnectionCombination(
		bool usesDevelopmentEmulator,
		ServiceBusConnectionStringProperties? emulatorManagementProperties,
		bool managementUsesDevelopmentEmulator)
	{
		if (emulatorManagementProperties is null)
			return;

		if (!usesDevelopmentEmulator)
			throw new ArgumentException("Emulator management connection string can only be used with an emulator primary connection string.");

		if (!managementUsesDevelopmentEmulator)
			throw new ArgumentException("Emulator management connection string must include UseDevelopmentEmulator=true.");
	}

	private static bool UsesDevelopmentEmulator(string connectionString)
	{
		foreach (string component in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			string[] parts = component.Split('=', 2, StringSplitOptions.TrimEntries);
			if (parts.Length == 2
				&& parts[0].Equals("UseDevelopmentEmulator", StringComparison.OrdinalIgnoreCase)
				&& bool.TryParse(parts[1], out bool useDevelopmentEmulator))
				return useDevelopmentEmulator;
		}

		return false;
	}

	private static bool IsManagementAuthorizationFailure(Exception exception)
		=> exception is UnauthorizedAccessException or RequestFailedException { Status: 401 or 403 };
}
