namespace ServiceBusViewer.UnitTests.Api.Endpoints.SessionState;

using Microsoft.AspNetCore.Http;
using NSubstitute;
using ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;
using ServiceBusViewer.Business.Application.Contracts;
using ServiceBusViewer.Business.Application.Dependencies;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Dependencies;
using ServiceBusViewer.Infrastructure.ClientSession;
using ServiceBusViewer.Infrastructure.ClientSession.Dependencies;
using Xunit;

public sealed class GetSessionStateRequestHandlerTests
{
	[Fact]
	public async Task GetSessionStateRequestHandler_HandleAsync_AutoRefreshConfigured_ReturnsEnabledFlag()
	{
		// Arrange
		DefaultHttpContext context = await CreateSessionContextAsync();

		IViewerConnectionService service = Substitute.For<IViewerConnectionService>();
		service.GetSnapshotAsync(Arg.Any<IViewerSessionState>(), Arg.Any<CancellationToken>())
			.Returns(new ViewerConnectionSnapshot(new ConnectionSettings("Endpoint=sb://localhost/;", null, null, null), true, null, false));

		IApplicationInfoProvider applicationInfoProvider = Substitute.For<IApplicationInfoProvider>();
		applicationInfoProvider.ApplicationVersion.Returns("1.2.3");

		IEntityCountAutoRefreshSettings autoRefreshSettings = Substitute.For<IEntityCountAutoRefreshSettings>();
		autoRefreshSettings.EntityCountAutoRefreshEnabled.Returns(true);

		// Act
		IResult result = await GetSessionStateRequestHandler.HandleAsync(context, service, applicationInfoProvider, autoRefreshSettings, CancellationToken.None);

		// Assert
		Microsoft.AspNetCore.Http.HttpResults.Ok<SessionStateResponse> ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<SessionStateResponse>>(result);
		Assert.True(ok.Value!.EntityCountAutoRefreshEnabled);
	}

	[Fact]
	public async Task GetSessionStateRequestHandler_HandleAsync_AutoRefreshDisabled_ReturnsDisabledFlag()
	{
		// Arrange
		DefaultHttpContext context = await CreateSessionContextAsync();

		IViewerConnectionService service = Substitute.For<IViewerConnectionService>();
		service.GetSnapshotAsync(Arg.Any<IViewerSessionState>(), Arg.Any<CancellationToken>())
			.Returns(new ViewerConnectionSnapshot(new ConnectionSettings("Endpoint=sb://localhost/;", null, null, null), true, null, false));

		IApplicationInfoProvider applicationInfoProvider = Substitute.For<IApplicationInfoProvider>();
		applicationInfoProvider.ApplicationVersion.Returns("1.2.3");

		IEntityCountAutoRefreshSettings autoRefreshSettings = Substitute.For<IEntityCountAutoRefreshSettings>();
		autoRefreshSettings.EntityCountAutoRefreshEnabled.Returns(false);

		// Act
		IResult result = await GetSessionStateRequestHandler.HandleAsync(context, service, applicationInfoProvider, autoRefreshSettings, CancellationToken.None);

		// Assert
		Microsoft.AspNetCore.Http.HttpResults.Ok<SessionStateResponse> ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<SessionStateResponse>>(result);
		Assert.False(ok.Value!.EntityCountAutoRefreshEnabled);
	}

	private static async Task<DefaultHttpContext> CreateSessionContextAsync()
	{
		IClientSessionSettings sessionSettings = Substitute.For<IClientSessionSettings>();
		sessionSettings.ConnectionString.Returns("Endpoint=sb://localhost/;");
		var registry = new ClientSessionStateRegistry(sessionSettings);
		var middleware = new ClientSessionStateMiddleware(_ => Task.CompletedTask);
		var context = new DefaultHttpContext();
		await middleware.InvokeAsync(context, registry);

		return context;
	}
}
