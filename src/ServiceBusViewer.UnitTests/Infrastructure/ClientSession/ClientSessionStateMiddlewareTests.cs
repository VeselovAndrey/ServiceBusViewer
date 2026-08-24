namespace ServiceBusViewer.UnitTests.Infrastructure.ClientSession;

using Microsoft.AspNetCore.Http;
using NSubstitute;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Infrastructure.ClientSession;
using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;
using Xunit;

public sealed class ClientSessionStateMiddlewareTests
{
	[Fact]
	public async Task ClientSessionStateMiddleware_InvokeAsync_ValidSessionCookie_ReusesSessionState()
	{
		// Arrange
		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");
		var registry = new ClientSessionStateRegistry(settings);
		var middleware = new ClientSessionStateMiddleware(_ => Task.CompletedTask);
		const string sessionId = "11111111-1111-1111-1111-111111111111";
		DefaultHttpContext firstContext = CreateContext($"sbv-session={sessionId}");
		DefaultHttpContext secondContext = CreateContext($"sbv-session={sessionId}");

		// Act
		await middleware.InvokeAsync(firstContext, registry);
		await middleware.InvokeAsync(secondContext, registry);

		// Assert
		IViewerSessionState firstState = ClientSessionStateMiddleware.GetClientSessionState(firstContext);
		IViewerSessionState secondState = ClientSessionStateMiddleware.GetClientSessionState(secondContext);
		Assert.Same(firstState, secondState);
		Assert.Contains($"sbv-session={sessionId}", firstContext.Response.Headers.SetCookie.ToString());
	}

	[Fact]
	public async Task ClientSessionStateMiddleware_InvokeAsync_MissingSessionCookie_CreatesSessionAndSetsCookie()
	{
		// Arrange
		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");
		var registry = new ClientSessionStateRegistry(settings);
		var middleware = new ClientSessionStateMiddleware(_ => Task.CompletedTask);
		DefaultHttpContext context = CreateContext(null);

		// Act
		await middleware.InvokeAsync(context, registry);

		// Assert
		Assert.IsAssignableFrom<IViewerSessionState>(ClientSessionStateMiddleware.GetClientSessionState(context));
		Assert.Matches("sbv-session=[0-9a-f]{32}", context.Response.Headers.SetCookie.ToString());
	}

	[Fact]
	public void ClientSessionStateMiddleware_GetClientSessionState_StateWasNotInitialized_ThrowsInvalidOperationException()
	{
		// Arrange
		var context = new DefaultHttpContext();

		// Act
		Action action = () => ClientSessionStateMiddleware.GetClientSessionState(context);

		// Assert
		Assert.Throws<InvalidOperationException>(action);
	}

	[Fact]
	public async Task ClientSessionStateMiddleware_InvokeAsync_InvalidSessionCookie_ReplacesCookieWithNewSessionId()
	{
		// Arrange
		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");
		var registry = new ClientSessionStateRegistry(settings);
		var middleware = new ClientSessionStateMiddleware(_ => Task.CompletedTask);
		DefaultHttpContext context = CreateContext("sbv-session=invalid");

		// Act
		await middleware.InvokeAsync(context, registry);

		// Assert
		string setCookie = context.Response.Headers.SetCookie.ToString();
		Assert.Matches("sbv-session=[0-9a-f]{32}", setCookie);
		Assert.DoesNotContain("invalid", setCookie);
	}

	[Fact]
	public async Task ClientSessionStateMiddleware_InvokeAsync_HttpsRequest_SetsSecureCookie()
	{
		// Arrange
		IClientSessionSettings settings = Substitute.For<IClientSessionSettings>();
		settings.ConnectionString.Returns("Endpoint=sb://test/");
		var registry = new ClientSessionStateRegistry(settings);
		var middleware = new ClientSessionStateMiddleware(_ => Task.CompletedTask);
		DefaultHttpContext context = CreateContext(null);
		context.Request.IsHttps = true;

		// Act
		await middleware.InvokeAsync(context, registry);

		// Assert
		Assert.Contains("secure", context.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	private static DefaultHttpContext CreateContext(string? cookie)
	{
		var context = new DefaultHttpContext();
		if (cookie is not null)
			context.Request.Headers.Cookie = cookie;

		return context;
	}
}
