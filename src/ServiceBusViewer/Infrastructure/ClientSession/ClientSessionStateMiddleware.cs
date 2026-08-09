namespace ServiceBusViewer.Infrastructure.ClientSession;

using ServiceBusViewer.Business.Viewer.Dependencies;

/// <summary>Ensures each request is associated with a server-managed browser session state bucket.</summary>
internal sealed class ClientSessionStateMiddleware(RequestDelegate next)
{
	private const string _sessionCookieName = "sbv-session";
	private const string _httpContextItemKey = "__ServiceBusViewerApi_BrowserSessionState";
	private readonly RequestDelegate _next = next;

	public async Task InvokeAsync(HttpContext context, ClientSessionStateRegistry registry)
	{
		string sessionId = GetOrCreateSessionId(context.Request.Cookies[_sessionCookieName]);
		ClientSessionState state = registry.GetOrCreate(sessionId);
		context.Items[_httpContextItemKey] = state;

		context.Response.Cookies.Append(_sessionCookieName, sessionId, new CookieOptions {
			HttpOnly = true,
			IsEssential = true,
			Path = "/",
			SameSite = SameSiteMode.Lax,
			Secure = context.Request.IsHttps
		});

		await _next(context);
	}

	public static IViewerSessionState GetClientSessionState(HttpContext context)
		=> context.Items.TryGetValue(_httpContextItemKey, out object? value) && value is IViewerSessionState state
			? state
			: throw new InvalidOperationException("Browser session state was not initialized for this request.");

	private static string GetOrCreateSessionId(string? sessionId)
		=> Guid.TryParse(sessionId, out _)
			? sessionId
			: Guid.NewGuid().ToString("N");
}

internal static class ClientSessionStateMiddlewareExtensions
{
	public static IApplicationBuilder UseClientSessionState(this IApplicationBuilder app)
		=> app.UseMiddleware<ClientSessionStateMiddleware>();

	public static IViewerSessionState GetClientSessionState(this HttpContext context)
		=> ClientSessionStateMiddleware.GetClientSessionState(context);
}
