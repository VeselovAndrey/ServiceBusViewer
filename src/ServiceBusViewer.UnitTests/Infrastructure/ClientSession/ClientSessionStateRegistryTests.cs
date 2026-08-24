namespace ServiceBusViewer.UnitTests.Infrastructure.ClientSession;

using NSubstitute;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;
using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;
using Xunit;

public sealed class ClientSessionStateRegistryTests
{
	[Fact]
	public void ClientSessionStateRegistry_GetOrCreate_SameSessionId_ReturnsSameStateWithConfiguredSettings()
	{
		// Arrange
		const string sessionId = "session-id";

		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");
		settings.EmulatorManagementConnectionString.Returns("Endpoint=sb://management/");
		settings.QueueOrTopicName.Returns("orders");
		settings.SubscriptionName.Returns("processing");

		var registry = new ClientSessionStateRegistry(settings);

		// Act
		ClientSessionState firstState = registry.GetOrCreate(sessionId);
		ClientSessionState secondState = registry.GetOrCreate(sessionId);

		// Assert
		Assert.Same(firstState, secondState);
		Assert.Equal(new ConnectionSettings("Endpoint=sb://test/", "Endpoint=sb://management/", "orders", "processing"), firstState.ConnectionSettings);
	}

	[Fact]
	public void ClientSessionStateRegistry_GetOrCreate_DifferentSessionIds_ReturnsIsolatedStates()
	{
		// Arrange
		const string sessionId = "session-id";

		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");

		var registry = new ClientSessionStateRegistry(settings);

		// Act
		ClientSessionState firstState = registry.GetOrCreate(sessionId);
		ClientSessionState secondState = registry.GetOrCreate($"{sessionId}-different-session");

		// Assert
		Assert.NotSame(firstState, secondState);
	}
}
