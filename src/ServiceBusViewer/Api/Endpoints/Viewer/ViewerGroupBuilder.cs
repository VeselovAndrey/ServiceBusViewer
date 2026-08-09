namespace ServiceBusViewer.Api.Endpoints.Viewer;

using ServiceBusViewer.Api.Endpoints.Viewer.GetViewer;
using ServiceBusViewer.Api.Endpoints.Viewer.Receive;
using ServiceBusViewer.Api.Endpoints.Viewer.Refresh;
using ServiceBusViewer.Api.Endpoints.Viewer.SelectEntity;
using ServiceBusViewer.Api.Endpoints.Viewer.Send;

internal static class ViewerGroupBuilder
{
	public static IEndpointRouteBuilder MapViewerEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder viewerGroup = endpoints.MapGroup("/viewer");

		viewerGroup.MapGet(string.Empty, GetViewerRequestHandler.HandleAsync);
		viewerGroup.MapPost("/refresh", RefreshRequestHandler.HandleAsync);
		viewerGroup.MapPost("/select-entity", SelectEntityRequestHandler.HandleAsync);
		viewerGroup.MapPost("/receive", ReceiveRequestHandler.HandleAsync);
		viewerGroup.MapPost("/send", SendRequestHandler.HandleAsync);

		return endpoints;
	}
}
