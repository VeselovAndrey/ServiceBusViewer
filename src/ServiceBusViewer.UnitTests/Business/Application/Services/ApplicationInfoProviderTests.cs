namespace ServiceBusViewer.UnitTests.Business.Application.Services;

using System.Reflection;
using NSubstitute;
using ServiceBusViewer.Business.Application.Dependencies;
using ServiceBusViewer.Business.Application.Services;
using ServiceBusViewer.Configuration.Settings;
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

	[Fact]
	public void ApplicationInfoProviderSettings_ShowEmulatorConnectHint_VariableUnset_ReturnsTrue()
	{
		// Arrange
		string? previousValue = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT");
		try
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", null);
			var settings = new ApplicationInfoProviderSettings();

			// Act
			bool shown = settings.ShowEmulatorConnectHint;

			// Assert
			Assert.True(shown);
		}
		finally
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", previousValue);
		}
	}

	[Fact]
	public void ApplicationInfoProviderSettings_ShowEmulatorConnectHint_VariableFalse_ReturnsFalse()
	{
		// Arrange
		string? previousValue = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT");
		try
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", "false");
			var settings = new ApplicationInfoProviderSettings();

			// Act
			bool shown = settings.ShowEmulatorConnectHint;

			// Assert
			Assert.False(shown);
		}
		finally
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", previousValue);
		}
	}

	[Fact]
	public void ApplicationInfoProviderSettings_ShowEmulatorConnectHint_VariableFalseDifferentCase_ReturnsFalse()
	{
		// Arrange
		string? previousValue = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT");
		try
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", "FALSE");
			var settings = new ApplicationInfoProviderSettings();

			// Act
			bool shown = settings.ShowEmulatorConnectHint;

			// Assert
			Assert.False(shown);
		}
		finally
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", previousValue);
		}
	}

	[Fact]
	public void ApplicationInfoProviderSettings_ShowEmulatorConnectHint_VariableOtherValue_ReturnsTrue()
	{
		// Arrange
		string? previousValue = Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT");
		try
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", "true");
			var settings = new ApplicationInfoProviderSettings();

			// Act
			bool shown = settings.ShowEmulatorConnectHint;

			// Assert
			Assert.True(shown);
		}
		finally
		{
			Environment.SetEnvironmentVariable("SERVICEBUSVIEWER_SHOW_EMULATOR_CONNECT_HINT", previousValue);
		}
	}
}
