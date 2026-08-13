namespace ServiceBusViewer.Configuration.Settings;

using ServiceBusViewer.Business.Application.Dependencies;

/// <summary>Settings for the <see cref="ServiceBusViewer.Business.Application.Services.ApplicationInfoProvider"/>.</summary>
internal sealed class ApplicationInfoProviderSettings : IApplicationInfoProviderSettings
{
	/// <inheritdoc/>
	public bool IsRunningInContainer
		=> string.Equals(
			Environment.GetEnvironmentVariable("SERVICEBUSVIEWER_RUNNING_IN_CONTAINER"),
			"true",
			StringComparison.OrdinalIgnoreCase);
}
