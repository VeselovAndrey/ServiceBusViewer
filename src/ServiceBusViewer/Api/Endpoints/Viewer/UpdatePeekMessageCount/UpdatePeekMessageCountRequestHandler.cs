namespace ServiceBusViewer.Api.Endpoints.Viewer.UpdatePeekMessageCount;

using ServiceBusViewer.Api.Converters;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class UpdatePeekMessageCountRequestHandler
{
	public static async Task<IResult> HandleAsync(
		HttpContext context,
		UpdatePeekMessageCountRequest request,
		IViewerMessageService service,
		CancellationToken cancellationToken)
	{
		Dictionary<string, string[]> errors = [];
		if (!UpdatePeekMessageCountRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? validationErrors) && validationErrors is not null) {
			foreach (KeyValuePair<string, string[]> kv in validationErrors)
				errors[kv.Key] = kv.Value;
		}

		if (errors.Count > 0)
			return Results.ValidationProblem(errors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		ServiceBusViewer.Business.Viewer.Contracts.ViewerState result = await service.UpdatePeekMessageCountAsync(
			context.GetClientSessionState(),
			request.PeekMessageCount,
			cancellationToken);

		return TypedResults.Ok(result.ToApiModel());
	}
}
