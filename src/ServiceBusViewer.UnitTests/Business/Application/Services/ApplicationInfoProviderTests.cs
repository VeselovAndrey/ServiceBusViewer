namespace ServiceBusViewer.UnitTests.Business.Application.Services;

using System.Reflection;
using NSubstitute;
using ServiceBusViewer.Business.Application.Dependencies;
using ServiceBusViewer.Business.Application.Services;
using Xunit;

public sealed class ApplicationInfoProviderTests
{
	[Fact]
	public void ApplicationInfoProvider_IsRunningInContainer_SettingsIndicateContainer_ReturnsTrue()
	{
		// Arrange
		IApplicationInfoProviderSettings settings = Substitute.For<IApplicationInfoProviderSettings>();
		settings.IsRunningInContainer.Returns(true);
		var provider = new ApplicationInfoProvider(settings);

		// Act
		bool isRunningInContainer = provider.IsRunningInContainer;

		// Assert
		Assert.True(isRunningInContainer);
	}

	[Fact]
	public void ApplicationInfoProvider_ApplicationVersion_AssemblyMetadataIsRead_ReturnsAssemblyVersion()
	{
		// Arrange
		Assembly assembly = typeof(ApplicationInfoProvider).Assembly;
		string expectedVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "unknown";

		IApplicationInfoProviderSettings settings = Substitute.For<IApplicationInfoProviderSettings>();
		settings.IsRunningInContainer.Returns(false);

		var provider = new ApplicationInfoProvider(settings);

		// Act
		string applicationVersion = provider.ApplicationVersion;

		// Assert
		Assert.Equal(expectedVersion, applicationVersion);
	}
}
