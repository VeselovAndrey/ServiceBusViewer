using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Business.Contracts;
using ServiceBusViewer.Business.Services;
using ServiceBusViewer.Infrastructure.ClientSession;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy("LocalDevelopment", policy => policy
	.SetIsOriginAllowed(static origin => IsLocalDevelopmentOrigin(origin))
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowCredentials()));
builder.Services.AddSingleton<ClientSessionStateRegistry>();
builder.Services.AddHostedService<ClientSessionStateCleanupService>();
builder.Services.AddSingleton<IViewerBusinessService, ViewerBusinessService>();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseCors("LocalDevelopment");
app.UseClientSessionState();

app.MapApiEndpoints();

await app.RunAsync();

static bool IsLocalDevelopmentOrigin(string origin)
	=> Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
	   && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
