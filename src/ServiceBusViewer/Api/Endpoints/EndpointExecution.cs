namespace ServiceBusViewer.Api.Endpoints;

using ServiceBusViewer.Business.Viewer.Contracts;

internal static class EndpointExecution
{
	public static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
	{
		try {
			return Results.Ok(await action());
		}
		catch (ViewerAlreadyConnectedException ex) {
			return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Already connected", detail: ex.Message);
		}
		catch (ViewerNotConnectedException ex) {
			return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Not connected", detail: ex.Message);
		}
		catch (Exception ex) {
			return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Request failed", detail: ex.Message);
		}
	}
}
