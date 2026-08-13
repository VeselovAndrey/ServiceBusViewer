namespace ServiceBusViewer.Configuration;

using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Application.Dependencies;
using ServiceBusViewer.Business.Application.Services;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Business.Viewer.Services;
using ServiceBusViewer.Configuration.Settings;
using ServiceBusViewer.Infrastructure.ClientSession;
using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;
using ServiceBusViewer.Infrastructure.ServiceBus;

internal static class ApplicationContainerConfiguration
{
	public static IServiceCollection AddApplicationServices(this IServiceCollection services)
	{
		// Application metadata
		services.AddSingleton<IApplicationInfoProviderSettings, ApplicationInfoProviderSettings>();
		services.AddSingleton<IApplicationInfoProvider, ApplicationInfoProvider>();

		// Session state
		services.AddSingleton<IClientSessionSettings, ClientSessionSettings>();
		services.AddSingleton<ClientSessionStateRegistry>();
		services.AddHostedService<ClientSessionStateCleanupService>();

		// Viewer services
		services.AddSingleton<IViewerConnectionService, ViewerConnectionService>();
		services.AddSingleton<IViewerEntityService, ViewerEntityService>();
		services.AddSingleton<IViewerMessageService, ViewerMessageService>();

		// Service Bus infrastructure
		services.AddSingleton<IServiceBusConnectionFactory, ServiceBusConnectionFactory>();

		return services;
	}
}
