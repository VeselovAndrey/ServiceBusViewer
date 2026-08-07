namespace ServiceBusViewer.Business.Application.Contracts;

/// <summary>
/// Provides application-level metadata needed by business services.
/// </summary>
internal interface IApplicationInfoProvider
{
	/// <summary>
	/// Gets the running application version string exposed to viewers.
	/// </summary>
	string ApplicationVersion { get; }
}
