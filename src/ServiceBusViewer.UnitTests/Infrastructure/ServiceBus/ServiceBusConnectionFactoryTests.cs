namespace ServiceBusViewer.UnitTests.Infrastructure.ServiceBus;

using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ServiceBus;
using Xunit;

public sealed class ServiceBusConnectionFactoryTests
{
	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_NullSettings_ThrowsArgumentNullException()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();

		// Act
		Task action() => factory.OpenAsync(null!, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentNullException>(action);
	}

	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_ManagementConnectionTargetsNonEmulator_ThrowsArgumentException()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();
		var settings = new ConnectionSettings(
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;",
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			"orders",
			null);

		// Act
		Task action() => factory.OpenAsync(settings, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentException>(action);
	}

	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_EmulatorWithoutEntityName_ThrowsArgumentException()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();
		var settings = new ConnectionSettings(
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			null,
			null,
			null);

		// Act
		Task action() => factory.OpenAsync(settings, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentException>(action);
	}

	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_EmulatorManagementConnectionDoesNotUseDevelopmentEmulator_ThrowsArgumentException()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();
		var settings = new ConnectionSettings(
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;",
			"orders",
			null);

		// Act
		Task action() => factory.OpenAsync(settings, CancellationToken.None);

		// Assert
		await Assert.ThrowsAsync<ArgumentException>(action);
	}

	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_EmulatorWithoutManagementAndQueueName_ReturnsScopedQueueConnection()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();
		var settings = new ConnectionSettings(
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			null,
			"orders",
			null);

		// Act
		await using var connection = await factory.OpenAsync(settings, CancellationToken.None);

		// Assert
		Assert.False(connection.IsManagementApiAvailable);
		Assert.Collection(connection.AvailableEntities, entity => Assert.Equal(new QueueEntityProperties("orders", false, TimeSpan.FromMinutes(1), 10, TimeSpan.MaxValue, false, TimeSpan.FromMinutes(10), false, true, false, TimeSpan.MaxValue), entity));
	}

	[Fact]
	public async Task ServiceBusConnectionFactory_OpenAsync_EmulatorWithoutManagementAndSubscription_ReturnsScopedTopicAndSubscriptionConnection()
	{
		// Arrange
		var factory = new ServiceBusConnectionFactory();
		var settings = new ConnectionSettings(
			"Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
			null,
			"orders",
			"processed");

		// Act
		await using var connection = await factory.OpenAsync(settings, CancellationToken.None);

		// Assert
		Assert.False(connection.IsManagementApiAvailable);
		Assert.Collection(
			connection.AvailableEntities,
			entity => Assert.Equal(new TopicEntityProperties("orders", TimeSpan.MaxValue, false, TimeSpan.FromMinutes(10), true, false, TimeSpan.MaxValue), entity),
			entity => Assert.Equal(new SubscriptionEntityProperties("processed", "orders", false, TimeSpan.FromMinutes(1), 10, TimeSpan.MaxValue, false, true, TimeSpan.MaxValue, []), entity));
	}
}
