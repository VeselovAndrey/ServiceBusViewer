using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Api.Endpoints;
using ServiceBusViewer.Api.ExceptionHandling;
using ServiceBusViewer.Configuration;
using ServiceBusViewer.Infrastructure.ClientSession;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options
	=> options.SerializerOptions.Converters.Add(new SubscriptionRuleJsonConverter()));
builder.Services.AddCors(options => options.AddPolicy("LocalDevelopment", policy => policy
	.SetIsOriginAllowed(static origin => IsLocalDevelopmentOrigin(origin))
	.AllowAnyHeader()
	.AllowAnyMethod()
	.AllowCredentials()));
builder.Services.AddApplicationServices();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseCors("LocalDevelopment");
app.UseClientSessionState();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapApiEndpoints();
app.MapFallback("/api/{**path}", static context => {
	context.Response.StatusCode = StatusCodes.Status404NotFound;
	return Task.CompletedTask;
});
app.MapFallbackToFile("index.html");

await app.RunAsync();

static bool IsLocalDevelopmentOrigin(string origin)
	=> Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri)
	   && (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));
