namespace ServiceBusViewer.Api.ExceptionHandling;

using System.Text.Json;
using Azure;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ServiceBusViewer.Business.Viewer.Contracts;

/// <summary>Converts unhandled API exceptions into consistent RFC problem-details responses.</summary>
/// <param name="problemDetailsService">The service used to write problem-details responses.</param>
/// <param name="logger">The logger used to record unexpected exceptions.</param>
internal sealed class ApiExceptionHandler(
	IProblemDetailsService problemDetailsService,
	ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
	private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;
	private readonly ILogger<ApiExceptionHandler> _logger = logger;

	/// <summary>Writes a classified problem-details response for an unhandled API exception.</summary>
	/// <param name="httpContext">The HTTP context for the failed request.</param>
	/// <param name="exception">The unhandled exception to classify.</param>
	/// <param name="cancellationToken">The token that signals request cancellation.</param>
	/// <returns><see langword="true"/> when the exception response was written; otherwise, <see langword="false"/>.</returns>
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
			return false;

		ExceptionResponse response = ClassifyException(exception);

		if (response is UnexpectedExceptionResponse)
			_logger.LogError(exception, "An unexpected exception occurred while processing an API request.");

		ProblemDetails problemDetails = response switch {
			KnownExceptionResponse known => new ProblemDetails {
				Status = known.Status,
				Title = known.Title,
				Detail = known.Detail
			},
			UnexpectedExceptionResponse unexpected => new ProblemDetails {
				Status = unexpected.Status,
				Title = unexpected.Title,
				Detail = unexpected.Detail
			}
		};

		httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

		return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext {
			HttpContext = httpContext,
			ProblemDetails = problemDetails
		});
	}

	private static ExceptionResponse ClassifyException(Exception exception)
	{
		KnownExceptionResponse? response = TryClassifyKnownException(exception);
		if (response is not null)
			return response;

		if (exception is AggregateException aggregateException) {
			foreach (Exception innerException in aggregateException.Flatten().InnerExceptions) {
				response = TryClassifyKnownException(innerException);
				if (response is not null)
					return response;
			}
		}

		return new UnexpectedExceptionResponse(
			StatusCodes.Status500InternalServerError,
			"Request failed",
			"An unexpected error occurred while processing the request.");
	}

	private static KnownExceptionResponse? TryClassifyKnownException(Exception exception)
		=> exception switch {
			ViewerAlreadyConnectedException => new KnownExceptionResponse(StatusCodes.Status409Conflict, "Already connected", exception.Message),
			ViewerNotConnectedException => new KnownExceptionResponse(StatusCodes.Status409Conflict, "Not connected", exception.Message),
			SendMessageValidationException => new KnownExceptionResponse(StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
			OperationCanceledException => new KnownExceptionResponse(StatusCodes.Status409Conflict, "Operation cancelled", "The Service Bus operation was cancelled because the viewer disconnected."),
			ArgumentException or FormatException or JsonException or BadHttpRequestException => new KnownExceptionResponse(StatusCodes.Status400BadRequest, "Invalid request", exception.Message),
			RequestFailedException or ServiceBusException => new KnownExceptionResponse(
				StatusCodes.Status400BadRequest,
				"Service Bus request failed",
				exception.Message),
			_ => null
		};

	private union ExceptionResponse(KnownExceptionResponse, UnexpectedExceptionResponse);

	private sealed record KnownExceptionResponse(int Status, string Title, string Detail);

	private sealed record UnexpectedExceptionResponse(int Status, string Title, string Detail);
}
