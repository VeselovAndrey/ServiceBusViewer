namespace ServiceBusViewer.Business.Application.Services;

using System.Reflection;
using ServiceBusViewer.Business.Application.Contracts;

internal sealed class ApplicationInfoProvider : IApplicationInfoProvider
{
	private static readonly string _applicationVersion = GetApplicationVersion();

	public string ApplicationVersion => _applicationVersion;

	private static string GetApplicationVersion()
	{
		Assembly assembly = typeof(ApplicationInfoProvider).Assembly;
		return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
			?? assembly.GetName().Version?.ToString()
			?? "unknown";
	}
}
