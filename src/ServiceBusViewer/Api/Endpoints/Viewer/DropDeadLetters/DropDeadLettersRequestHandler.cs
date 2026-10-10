namespace ServiceBusViewer.Api.Endpoints.Viewer.DropDeadLetters;

using System;
using System.Collections.Generic;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class DropDeadLettersRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, DropDeadLettersRequest request, IViewerMessageService service, CancellationToken cancellationToken)
	{
		if (!DropDeadLettersRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? validationErrors))
			return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		DeadLetterOperationResult result = await service.DropDeadLetterMessagesAsync(
			context.GetClientSessionState(),
			request.MessageId,
			cancellationToken);

		return TypedResults.Ok(new DropDeadLettersResponse(result.Succeeded, result.AffectedCount, result.FailedCount, result.Detail));
	}
}
