namespace ServiceBusViewer.Business.Application.Contracts;

/// <summary>Provides application-level metadata.</summary>
internal interface IApplicationInfoProvider
{
	/// <summary>Gets the application version string exposed to clients.</summary>
	string ApplicationVersion { get; }
}
