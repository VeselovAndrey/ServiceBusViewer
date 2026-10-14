namespace ServiceBusViewer.Business.Application.Dependencies;

/// <summary>Provides configuration required by the application metadata provider.</summary>
internal interface IApplicationInfoProviderSettings
{
	/// <summary>Gets whether the application process is explicitly identified as containerized.</summary>
	bool IsRunningInContainer { get; }

	/// <summary>Gets whether the emulator connect hint message and insert button are shown on the connect screen.</summary>
	bool ShowEmulatorConnectHint { get; }
}
