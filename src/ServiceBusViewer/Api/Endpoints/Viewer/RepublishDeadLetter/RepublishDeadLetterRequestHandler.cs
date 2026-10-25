namespace ServiceBusViewer.Api.Endpoints.Viewer.RepublishDeadLetter;

using System;
using System.Collections.Generic;
using ServiceBusViewer.Business.Viewer.Contracts;
using ServiceBusViewer.Business.Viewer.Contracts.ServiceBus;
using ServiceBusViewer.Infrastructure.ClientSession;

internal static class RepublishDeadLetterRequestHandler
{
	public static async Task<IResult> HandleAsync(HttpContext context, RepublishDeadLetterRequest request, IViewerMessageService service, CancellationToken cancellationToken)
	{
		if (!RepublishDeadLetterRequestValidator.Validate(request, out IReadOnlyDictionary<string, string[]>? validationErrors))
			return Results.ValidationProblem(validationErrors, statusCode: StatusCodes.Status400BadRequest, title: "Validation failed");

		ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.RepublishMessageIdStrategy strategy =
			Enum.Parse<ServiceBusViewer.Business.Viewer.Contracts.ServiceBus.RepublishMessageIdStrategy>(request.Strategy!.Value.ToString(), ignoreCase: true);

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

		DeadLetterOperationResult result = await service.RepublishDeadLetterMessagesAsync(
			context.GetClientSessionState(),
			request.MessageId,
			sequenceNumber,
			strategy,
			request.ManualMessageId,
			request.MaxCount,
			cancellationToken);

		return TypedResults.Ok(new RepublishDeadLetterResponse(result.Succeeded, result.AffectedCount, result.FailedCount, result.Detail));
	}
}
