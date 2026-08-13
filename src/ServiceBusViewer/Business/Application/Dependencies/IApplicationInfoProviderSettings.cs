namespace ServiceBusViewer.Business.Application.Dependencies;

/// <summary>Provides configuration required by the application metadata provider.</summary>
internal interface IApplicationInfoProviderSettings
{
	/// <summary>Gets whether the application process is explicitly identified as containerized.</summary>
	bool IsRunningInContainer { get; }
}
