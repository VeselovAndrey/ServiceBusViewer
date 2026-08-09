using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.ExceptionHandling;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Application.Services;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Business.Viewer.Services;
using ServiceBusViewer.Infrastructure.ClientSession;
using ServiceBusViewer.Infrastructure.ServiceBus;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddCors(options => options.AddPolicy("LocalDevelopment", policy => policy
	.SetIsOriginAllowed(static origin => IsLocalDevelopmentOrigin(origin))
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowCredentials()));
builder.Services.AddSingleton<ClientSessionStateRegistry>();
builder.Services.AddHostedService<ClientSessionStateCleanupService>();
builder.Services.AddSingleton<IServiceBusConnectionFactory, ServiceBusConnectionFactory>();
builder.Services.AddSingleton<IApplicationInfoProvider, ApplicationInfoProvider>();
builder.Services.AddSingleton<IViewerConnectionService, ViewerConnectionService>();
builder.Services.AddSingleton<IViewerEntityService, ViewerEntityService>();
builder.Services.AddSingleton<IViewerMessageService, ViewerMessageService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseCors("LocalDevelopment");
app.UseClientSessionState();

app.MapApiEndpoints();

await app.RunAsync();

static bool IsLocalDevelopmentOrigin(string origin)
	=> Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
	   && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
