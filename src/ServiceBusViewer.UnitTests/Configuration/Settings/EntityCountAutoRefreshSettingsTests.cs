namespace ServiceBusViewer.UnitTests.Configuration.Settings;

using Microsoft.Extensions.Options;
using ServiceBusViewer.Configuration.Settings;
using Xunit;

public sealed class EntityCountAutoRefreshSettingsTests
{
	[Fact]
	public void EntityCountAutoRefreshSettings_NotConfigured_UsesDocumentedDefault()
	{
		// Arrange
		var settings = CreateSettings(new EntityCountAutoRefreshOptions());

		// Act
		bool isEnabled = settings.EntityCountAutoRefreshEnabled;

		// Assert
		Assert.False(isEnabled);
	}

	[Fact]
	public void EntityCountAutoRefreshSettings_EnabledConfigured_EnablesAutoRefresh()
	{
		// Arrange
		var settings = CreateSettings(new EntityCountAutoRefreshOptions { Enabled = true });

		// Act
		bool isEnabled = settings.EntityCountAutoRefreshEnabled;

		// Assert
		Assert.True(isEnabled);
	}

	[Fact]
	public void EntityCountAutoRefreshSettings_EnabledConfiguredFalse_KeepsAutoRefreshDisabled()
	{
		// Arrange
		var settings = CreateSettings(new EntityCountAutoRefreshOptions { Enabled = false });

		// Act
		bool isEnabled = settings.EntityCountAutoRefreshEnabled;

		// Assert
		Assert.False(isEnabled);
	}

	private static EntityCountAutoRefreshSettings CreateSettings(EntityCountAutoRefreshOptions options)
		=> new(Options.Create(options));
}
