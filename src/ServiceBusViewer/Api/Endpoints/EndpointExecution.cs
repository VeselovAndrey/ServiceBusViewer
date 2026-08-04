namespace ServiceBusViewer.Api.Endpoints;

internal static class EndpointExecution
{
	public static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
	{
		try {
			return Results.Ok(await action());
		}
		catch (ApiProblemException ex) when (ex.Errors is not null) {
			return Results.ValidationProblem(ex.Errors, statusCode: ex.StatusCode, title: ex.Title);
		}
		catch (ApiProblemException ex) {
			return Results.Problem(statusCode: ex.StatusCode, title: ex.Title, detail: ex.Message);
		}
		catch (Exception ex) {
			return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Request failed", detail: ex.Message);
		}
	}
}
