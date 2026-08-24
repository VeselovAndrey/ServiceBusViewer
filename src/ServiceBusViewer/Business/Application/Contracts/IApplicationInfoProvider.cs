namespace ServiceBusViewer.Business.Application.Contracts;

/// <summary>Provides application-level metadata.</summary>
public interface IApplicationInfoProvider
{
	/// <summary>Gets the application version string exposed to clients.</summary>
	string ApplicationVersion { get; }

	/// <summary>Gets whether the application image explicitly identifies the current process as containerized.</summary>
	bool IsRunningInContainer { get; }
}
