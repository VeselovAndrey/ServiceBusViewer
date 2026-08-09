namespace ServiceBusViewer.Business.Application.Services;

using System.Reflection;
using ServiceBusViewer.Business.Application.Contracts;

internal sealed class ApplicationInfoProvider : IApplicationInfoProvider
{
	private const string _containerMarkerEnvironmentVariable = "SERVICEBUSVIEWER_RUNNING_IN_CONTAINER";

	private static readonly string _applicationVersion = GetApplicationVersion();
	private static readonly bool _isRunningInContainer = string.Equals(Environment.GetEnvironmentVariable(_containerMarkerEnvironmentVariable), "true", StringComparison.OrdinalIgnoreCase);

	public string ApplicationVersion => _applicationVersion;

	public bool IsRunningInContainer => _isRunningInContainer;

	private static string GetApplicationVersion()
	{
		Assembly assembly = typeof(ApplicationInfoProvider).Assembly;
		return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "unknown";
	}
}
