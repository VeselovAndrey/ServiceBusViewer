namespace SolidCode.Aspire.Hosting;

using global::Aspire.Hosting;
using global::Aspire.Hosting.ApplicationModel;
using SolidCode.Aspire.Hosting.ApplicationModel;

/// <summary>Provides Aspire registration and configuration helpers for the Service Bus Viewer container.</summary>
public static class ServiceBusViewerResourceBuilderExtensions
{
	private static class EnvironmentVariables
	{
		public const string ServiceBusViewerConnectionString = "SERVICEBUSVIEWER_CONNECTION_STRING";
		public const string ServiceBusViewerEmulatorManagementConnectionString = "SERVICEBUSVIEWER_EMULATOR_MANAGEMENT_CONNECTION_STRING";
		public const string ServiceBusViewerQueueOrTopicName = "SERVICEBUSVIEWER_QUEUE_OR_TOPIC_NAME";
		public const string ServiceBusViewerSubscriptionName = "SERVICEBUSVIEWER_SUBSCRIPTION_NAME";
	}

	private static class ServiceBusViewerContainerImageTags
	{
		internal const string Registry = "ghcr.io";
		internal const string Image = "veselovandrey/servicebusviewer";
		internal const string Tag = "latest";
	}

	/// <summary>Adds the Service Bus Viewer container resource using the default GHCR image and `latest` tag.</summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <param name="name">The resource name.</param>
	/// <param name="tag">The optional container image tag override. Defaults to <c>latest</c>.</param>
	/// <param name="httpPort">The optional host HTTP port bound to the container's internal port 8080.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> AddServiceBusViewer(
		this IDistributedApplicationBuilder builder,
		[ResourceName] string name,
		string? tag = null,
		int? httpPort = null)
	{
		string resolvedTag = string.IsNullOrWhiteSpace(tag)
			? ServiceBusViewerContainerImageTags.Tag
			: tag;

		var resource = new ServiceBusViewerResource(name);

		IResourceBuilder<ServiceBusViewerResource> resourceBuilder = builder.AddResource(resource)
			.WithImage(ServiceBusViewerContainerImageTags.Image)
			.WithImageRegistry(ServiceBusViewerContainerImageTags.Registry)
			.WithImageTag(resolvedTag);

		return httpPort is null
			? resourceBuilder.WithHttpEndpoint(targetPort: ServiceBusViewerResource.TargetHttpPort, name: ServiceBusViewerResource.HttpEndpointName)
			: resourceBuilder.WithHttpEndpoint(port: httpPort.Value, targetPort: ServiceBusViewerResource.TargetHttpPort, name: ServiceBusViewerResource.HttpEndpointName);
	}

	/// <summary>Sets all supported Service Bus Viewer connection-related environment variables in one call.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="connectionString">The primary Service Bus connection string.</param>
	/// <param name="emulatorManagementConnectionString">The optional emulator-only management connection string.</param>
	/// <param name="queueOrTopicName">The optional queue name or parent topic name.</param>
	/// <param name="subscriptionName">The optional subscription name.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithConnectionSettings(
		this IResourceBuilder<ServiceBusViewerResource> builder,
		string connectionString,
		string? emulatorManagementConnectionString = null,
		string? queueOrTopicName = null,
		string? subscriptionName = null)
	{
		IResourceBuilder<ServiceBusViewerResource> configuredBuilder = builder.WithConnectionString(connectionString);

		if (emulatorManagementConnectionString is not null)
			configuredBuilder = configuredBuilder.WithEmulatorManagementConnectionString(emulatorManagementConnectionString);

		if (queueOrTopicName is not null)
			configuredBuilder = configuredBuilder.WithQueueOrTopicName(queueOrTopicName);

		if (subscriptionName is not null)
			configuredBuilder = configuredBuilder.WithSubscriptionName(subscriptionName);

		return configuredBuilder;
	}

	/// <summary>Configures the viewer to use Aspire-managed Service Bus emulator endpoints for messaging and management.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="serviceBusResourceBuilder">The Aspire resource builder that exposes the emulator's <c>messaging</c> and <c>management</c> endpoints.</param>
	/// <param name="messagingEndpointName">The name of the messaging endpoint. The default value is the same as used in the Aspire-managed Service Bus emulator containers.</param>
	/// <param name="managementEndpointName">The name of the management endpoint. The default value is the same as used in the Aspire-managed Service Bus emulator containers.</param>
	/// <param name="queueOrTopicName">The optional queue name or parent topic name.</param>
	/// <param name="subscriptionName">The optional subscription name.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithServiceBusEmulatorReference(
		this IResourceBuilder<ServiceBusViewerResource> builder,
		IResourceBuilder<IResourceWithEndpoints> serviceBusResourceBuilder,
		string messagingEndpointName = "emulator",
		string managementEndpointName = "emulatorhealth",
		string? queueOrTopicName = null,
		string? subscriptionName = null)
	{
		ReferenceExpression messagingReferenceExpression = GetEndpointReferenceExpression(serviceBusResourceBuilder.GetEndpoint(messagingEndpointName));
		ReferenceExpression managementReferenceExpression = GetEndpointReferenceExpression(serviceBusResourceBuilder.GetEndpoint(managementEndpointName));

		return builder
			.WithConnectionSettings(messagingReferenceExpression, managementReferenceExpression, queueOrTopicName, subscriptionName)
			.WaitFor(serviceBusResourceBuilder, WaitBehavior.WaitOnResourceUnavailable);

		static ReferenceExpression GetEndpointReferenceExpression(EndpointReference endpointReference)
			=> ReferenceExpression.Create($"Endpoint=sb://{endpointReference.Property(EndpointProperty.Host)}:{endpointReference.Property(EndpointProperty.Port)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");
	}

	/// <summary>Sets all supported Service Bus Viewer connection-related environment variables in one call.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="connectionString">The primary Service Bus connection string expression.</param>
	/// <param name="emulatorManagementConnectionString">The optional emulator-only management connection string expression.</param>
	/// <param name="queueOrTopicName">The optional queue name or parent topic name.</param>
	/// <param name="subscriptionName">The optional subscription name.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithConnectionSettings(
		this IResourceBuilder<ServiceBusViewerResource> builder,
		ReferenceExpression connectionString,
		ReferenceExpression? emulatorManagementConnectionString = null,
		string? queueOrTopicName = null,
		string? subscriptionName = null)
	{
		IResourceBuilder<ServiceBusViewerResource> configuredBuilder = builder.WithConnectionString(connectionString);

		if (emulatorManagementConnectionString is not null)
			configuredBuilder = configuredBuilder.WithEmulatorManagementConnectionString(emulatorManagementConnectionString);

		if (queueOrTopicName is not null)
			configuredBuilder = configuredBuilder.WithQueueOrTopicName(queueOrTopicName);

		if (subscriptionName is not null)
			configuredBuilder = configuredBuilder.WithSubscriptionName(subscriptionName);

		return configuredBuilder;
	}

	/// <summary>Sets the primary Service Bus connection string for the Service Bus Viewer container.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="connectionString">The primary Service Bus connection string.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithConnectionString(this IResourceBuilder<ServiceBusViewerResource> builder, string connectionString)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerConnectionString, connectionString);

	/// <summary>Sets the primary Service Bus connection string for the Service Bus Viewer container.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="connectionString">The primary Service Bus connection string expression.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithConnectionString(this IResourceBuilder<ServiceBusViewerResource> builder, ReferenceExpression connectionString)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerConnectionString, connectionString);

	/// <summary>Sets the optional emulator-only management connection string for the Service Bus Viewer container.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="emulatorManagementConnectionString">The emulator management connection string.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithEmulatorManagementConnectionString(this IResourceBuilder<ServiceBusViewerResource> builder, string emulatorManagementConnectionString)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerEmulatorManagementConnectionString, emulatorManagementConnectionString);

	/// <summary>Sets the optional emulator-only management connection string for the Service Bus Viewer container.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="emulatorManagementConnectionString">The emulator management connection string expression.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithEmulatorManagementConnectionString(this IResourceBuilder<ServiceBusViewerResource> builder, ReferenceExpression emulatorManagementConnectionString)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerEmulatorManagementConnectionString, emulatorManagementConnectionString);

	/// <summary>Sets the optional queue name or parent topic name for direct-entity access.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="queueOrTopicName">The queue name or parent topic name.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithQueueOrTopicName(this IResourceBuilder<ServiceBusViewerResource> builder, string queueOrTopicName)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerQueueOrTopicName, queueOrTopicName);

	/// <summary>Sets the optional subscription name for direct subscription access.</summary>
	/// <param name="builder">The Service Bus Viewer resource builder.</param>
	/// <param name="subscriptionName">The subscription name.</param>
	/// <returns>The resource builder for further configuration.</returns>
	public static IResourceBuilder<ServiceBusViewerResource> WithSubscriptionName(this IResourceBuilder<ServiceBusViewerResource> builder, string subscriptionName)
		=> builder.WithEnvironment(EnvironmentVariables.ServiceBusViewerSubscriptionName, subscriptionName);
}
