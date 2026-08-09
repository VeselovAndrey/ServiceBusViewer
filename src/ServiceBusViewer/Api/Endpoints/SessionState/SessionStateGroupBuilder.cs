namespace ServiceBusViewer.Api.Endpoints.SessionState;

using ServiceBusViewer.Api.Endpoints.SessionState.GetSessionState;

internal static class SessionStateGroupBuilder
{
	public static IEndpointRouteBuilder MapSessionStateEndpoints(this IEndpointRouteBuilder endpoints)
	{
		RouteGroupBuilder sessionStateGroup = endpoints.MapGroup("/session-state");

		sessionStateGroup.MapGet(string.Empty, GetSessionStateRequestHandler.HandleAsync);

		return endpoints;
	}
}
