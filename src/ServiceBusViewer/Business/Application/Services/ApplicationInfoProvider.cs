namespace ServiceBusViewer.Business.Application.Services;

using System.Reflection;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Application.Dependencies;

internal sealed class ApplicationInfoProvider(IApplicationInfoProviderSettings settings) : IApplicationInfoProvider
{
	private static readonly string _applicationVersion = GetApplicationVersion();

	public string ApplicationVersion => _applicationVersion;

	public bool IsRunningInContainer { get; } = settings.IsRunningInContainer;

	private static string GetApplicationVersion()
	{
		Assembly assembly = typeof(ApplicationInfoProvider).Assembly;
		return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "unknown";
	}
}
