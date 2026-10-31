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

		long? sequenceNumber = null;

		if (request.SequenceNumber is not null) {
			if (!long.TryParse(request.SequenceNumber, out long parsedSequenceNumber)) {
				Dictionary<string, string[]> sequenceValidationErrors = new()
				{
					[nameof(request.SequenceNumber)] = ["SequenceNumber must be a valid 64-bit sequence number, or null."]
				};

				return Results.ValidationProblem(sequenceValidationErrors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");
			}

			sequenceNumber = parsedSequenceNumber;
		}

		DeadLetterOperationResult result = await service.DropDeadLetterMessagesAsync(
			context.GetClientSessionState(),
			request.MessageId,
			sequenceNumber,
			request.MaxCount,
			cancellationToken);

		return TypedResults.Ok(new DropDeadLettersResponse(result.Succeeded, result.AffectedCount, result.FailedCount, result.Detail));
	}
}
