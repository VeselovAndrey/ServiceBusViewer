namespace SolidCode.Aspire.Hosting.ApplicationModel;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;

/// <summary>Represents the Service Bus Viewer container resource in the Aspire app model.</summary>
public sealed class ServiceBusViewerResource([ResourceName] string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
	/// <summary>The well-known endpoint name for the Service Bus Viewer HTTP endpoint.</summary>
	public const string HttpEndpointName = "http";

	internal const int TargetHttpPort = 8080;

	private EndpointReference? _httpEndpoint;

	/// <summary>Gets the HTTP endpoint exposed by the Service Bus Viewer container.</summary>
	public EndpointReference HttpEndpoint => _httpEndpoint ??= new EndpointReference(this, HttpEndpointName);
}
